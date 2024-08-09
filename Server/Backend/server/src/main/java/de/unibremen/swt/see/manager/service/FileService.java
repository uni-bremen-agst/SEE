package de.unibremen.swt.see.manager.service;

import de.unibremen.swt.see.manager.model.File;
import de.unibremen.swt.see.manager.model.ProjectType;
import de.unibremen.swt.see.manager.model.Server;
import de.unibremen.swt.see.manager.repository.FileRepository;
import jakarta.persistence.EntityNotFoundException;
import java.io.IOException;
import java.io.InputStream;
import java.nio.file.Files;
import static java.nio.file.LinkOption.NOFOLLOW_LINKS;
import java.nio.file.NoSuchFileException;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.multipart.MultipartFile;

/**
 * Service class for managing file-related operations.
 * <p>
 * This service provides high-level operations for file management, including
 * creating, retrieving, updating, and deleting files. It encapsulates the
 * business logic and acts as an intermediary between the controller layer and
 * the data access layer.
 *
 * @see FileRepository
 * @see de.unibremen.swt.see.manager.controller.FileController
 */
@Service
@Transactional
@RequiredArgsConstructor
@Slf4j
public class FileService {

    /**
     * Enables file data persistence and retrieval for this service.
     */
    private final FileRepository fileRepo;

    /**
     * Contains the file storage path on the local file system.
     * <p>
     * The value is configured in the application properties and gets injected
     * during class initialization.
     */
    @Value("${see.app.filestorage.dir}")
    private String fileStorageRoot;

    /**
     * Creates a new file from the provided attributes.
     * <p>
     * The file metadata is stored in the database and the content is stored on
     * the local file system. A reference to the associated server and file type
     * is stored in the metadata.
     *
     * @param server the server instance this file belongs to
     * @param projectType the type of the project
     * @param multipartFile the file content from the API request
     * @return the created file, or {@code null} if the file content is empty.
     * @throws java.io.IOException if there was an I/O error while storing the
     * file
     */
    public File create(Server server, ProjectType projectType, MultipartFile multipartFile) throws IOException {
        if (multipartFile.isEmpty()) {
            return null;
        }

        File file = new File();
        file.setName(multipartFile.getOriginalFilename());
        file.setContentType(multipartFile.getContentType());
        file.setServer(server);
        file.setProjectType(projectType);

        Path path;
        try {
            path = storeFile(file, multipartFile);
        } catch (IOException e) {
            throw new IOException("Error persisting file.", e);
        }
        file.setSize(Files.size(path));

        return fileRepo.save(file);
    }

    /**
     * Retrieves a file by its ID.
     *
     * @param fileId the ID of the file to retrieve
     * @return the file if found, or {@code null} if not found
     */
    public File get(UUID fileId) {
        log.info("Fetching file by id {}", fileId);
        Optional<File> optFile = fileRepo.findById(fileId);
        if (optFile.isEmpty()) {
            log.error("File not found in db: {}", fileId);
            return null;
        }
        return optFile.get();
    }

    /**
     * Retrieves a file by its associated server and project type.
     *
     * @param serverId the ID of the server the file belongs to
     * @param projectType the project type of the file
     * @return file if found, or {@code null} if not found
     * @throws EntityNotFoundException if server or file could not be found
     */
    public File getByServerAndProjectType(UUID serverId, ProjectType projectType) {
        log.info("Fetching file for server and project type: {}; {}", serverId, projectType);

        Optional<File> optFile = fileRepo.findByServerIdAndProjectType(serverId, projectType);
        if (optFile.isEmpty()) {
            throw new EntityNotFoundException("File not found by project type " + projectType);
        }
        return optFile.get();
    }

    /**
     * Deletes a file.
     * <p>
     * Deletes the file from local file system and the file metadata object from
     * database.
     * <p>
     * Does not throw I/O exception if the file to delete was not found.
     *
     * @param file the file to be deleted
     * @throws java.io.IOException if there was an I/O error while deleting the
     * file
     */
    public void delete(File file) throws IOException {
        Path filePath = getPath(file);
        log.info("Removing file {}", filePath);

        if (Files.exists(filePath) && !Files.isRegularFile(filePath)) {
            throw new IOException("File not deleted. Not a regular file: " + filePath);
        }

        try {
            Files.delete(filePath);
        } catch (NoSuchFileException e) {
            log.warn("File to delete does not exist: {}", filePath);
        }
        fileRepo.delete(file);
    }

    /**
     * Convenience function to delete a file by its ID.
     * <p>
     * The file is retrieved by its ID and then deleted.
     * <p>
     * Does not throw I/O exception if the file to delete was not found.
     *
     * @param fileId ID of the file to be deleted
     * @throws java.io.IOException if {@link #delete(File)} throws one
     * @throws EntityNotFoundException if no file exists with given ID
     * @see #get(UUID)
     * @see #delete(File)
     */
    public void delete(UUID fileId) throws IOException {
        File file = get(fileId);
        if (file == null) {
            throw new EntityNotFoundException("No entity found with ID " + fileId);
        }
        delete(file);
    }

    /**
     * Retrieves all files of a server.
     *
     * @param server the server that the files belong to
     * @return a list containing all files of the given server
     */
    public List<File> getByServer(Server server) {
        return fileRepo.findByServer(server);
    }

    /**
     * Deletes all files of a server.
     *
     * @param server the server to delete files for
     * @throws IOException if a file cannot be deleted
     */
    public void deleteFilesByServer(Server server) throws IOException {
        List<File> files = getByServer(server);

        for (File file : files) {
            delete(file);
        }

        Files.delete(getServerUploadPath(server));
    }


    /**
     * Stores given file on local file system.
     *
     * @param file the prepared file metadata
     * @param multipartFile the file content
     * @return the path to where the file was stored
     * @throws IOException if there was an I/O error while storing the file
     */
    private Path storeFile(File file, MultipartFile multipartFile) throws IOException {
        Path filePath = getPath(file);
        if (Files.exists(filePath)) {
            throw new IOException("File already exists: " + filePath.toString());
        }

        try (InputStream inputStream = multipartFile.getInputStream()) {
            Files.copy(inputStream, filePath);
        } catch (IOException e) {
            throw new IOException("Unable to save file: " + file.getName(), e);
        }
        return filePath;
    }

    /**
     * Generates the file system path of the directory where all files of a
     * specific server are stored.
     * <p>
     * Tries to create the directory if it does not yet exist. Server ID must
     * not be {@code null}.
     *
     * @param server the server that the upload path belongs to
     * @return the path where all files of given server are stored
     * @throws IOException if there is a problem accessing or creating the
     * directory, or if the path is not a directory
     */
    public Path getServerUploadPath(Server server) throws IOException {
        Path basePath = Paths.get(fileStorageRoot).toAbsolutePath();
        Path uploadPath = basePath.resolve(server.getId().toString());
        if (!Files.exists(uploadPath)) {
            try {
                return Files.createDirectories(uploadPath);
            } catch (IOException e) {
                throw new IOException("File Storage Path does not exist and could not be created: " + uploadPath.toString(), e);
            }
        }
        if (!Files.isDirectory(uploadPath, NOFOLLOW_LINKS)) {
            throw new IOException("File Storage Path is not a directory!");
        }
        return uploadPath;
    }

    /**
     * Generates the file system path for the given file.
     * <p>
     * Gets the server path and appends the file name. File name and server must
     * not be {@code null}.
     *
     * @param file the file to which the path should be assembled
     * @return file system path for the given file
     * @throws IOException if one is thrown by
     * {@link #getServerUploadPath(Server)}
     * @see #getServerUploadPath(Server)
     */
    public Path getPath(File file) throws IOException {
        String fileName = file.getName();
        if (fileName == null || fileName.isEmpty()) {
            throw new RuntimeException("File name must not be empty!");
        }
        return getServerUploadPath(file.getServer()).resolve(fileName);
    }

    /**
     * Extracts the file extension from given file name.
     *
     * @param fileName file name to extract the extension from
     * @return extension of the given file if existent, or else empty
     * {@code String}
     */
    private static String getFileExtension(String fileName) {
        int idx = fileName.lastIndexOf('.');
        return (idx != -1) ? fileName.substring(idx + 1) : "";
    }

}
