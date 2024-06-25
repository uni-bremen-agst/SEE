package de.unibremen.swt.see.manager.service;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.multipart.MultipartFile;
import de.unibremen.swt.see.manager.util.FileType;
import de.unibremen.swt.see.manager.model.File;
import de.unibremen.swt.see.manager.model.Server;
import de.unibremen.swt.see.manager.repo.FileRepo;
import static de.unibremen.swt.see.manager.util.FileType.*;

import java.io.IOException;
import java.io.InputStream;
import java.nio.file.Files;
import static java.nio.file.LinkOption.NOFOLLOW_LINKS;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

@Service
@Transactional
@RequiredArgsConstructor
@Slf4j
public class FileService {

    private final FileRepo fileRepo;
    
    @Value("${see.app.filestorage.dir}")
    private String fileStorageRoot;

    public File createFile(Server server, FileType type, MultipartFile multipartFile) {
        if (multipartFile.isEmpty()) {
            return null;
        }
        
        String originalFileName = multipartFile.getOriginalFilename();
        String intendedFileName;
        switch (type) {
            case CSV      -> intendedFileName = "multiplayer.csv";
            case CONFIG   -> intendedFileName = "multiplayer.cfg";
            case GXL      -> intendedFileName = "multiplayer.gxl";
            case SOLUTION -> intendedFileName = "solution." + getFileExtension(originalFileName);
            case SOURCE   -> intendedFileName = "src.zip";
            default       -> throw new RuntimeException("File name could not be derived from file type!");
        }

        File file = new File();
        file.setContentType(multipartFile.getContentType());
        file.setOriginalFileName(originalFileName);
        file.setName(intendedFileName);
        file.setServer(server);

        try {
            storeFile(file, multipartFile);
        } catch (IOException e) {
            throw new IllegalStateException("Error persisting file.", e);
        }
        return fileRepo.save(file);
    }

    private void storeFile(File file, MultipartFile multipartFile) throws IOException {
        Path filePath = getFilePath(file);
        if (Files.exists(filePath)) {
            throw new IOException("File already exists: " + filePath.toString());
        }
        
        try (InputStream inputStream = multipartFile.getInputStream()) {
            Files.copy(inputStream, filePath);
        } catch (IOException e) {
            throw new IOException("Unable to save file: " + file.getName(), e);
        }
    }

    public File getFile(UUID fileId) throws IOException {
        log.info("Fetching file by id {}", fileId);
        Optional<File> optFile = fileRepo.findById(fileId);
        if (optFile.isEmpty()) {
            log.error("File not found in db: {}", fileId);
            return null;
        }
        return optFile.get();
    }

    public File getFileByServerAndFileType(Server server, FileType fileType) throws IOException {
        log.info("Fetching file for server {} and type {}", server, fileType);

        Optional<File> optFile = fileRepo.findFileByServerAndFileType(server, fileType);
        if (optFile.isEmpty()) {
            log.error("File not found in db for server {} with type {}", server.getId(), fileType);
            return null;
        }
        return optFile.get();
    }

    public boolean deleteFile(File file) throws IOException {
        Path filePath = getFilePath(file);
        log.info("Removing file {}", filePath);

        if (!Files.exists(filePath)) {
            log.warn("File already deleted: {}", filePath);
            return true;
        }
        if (!Files.isRegularFile(filePath)) {
            log.error("Not a regular file: {}", filePath);
            return false;
        }
        Files.delete(filePath);
        fileRepo.delete(file);

        return true;
    }
    
    public boolean deleteFile(UUID fileId) throws IOException {
        return deleteFile(getFile(fileId));
    }


    public List<File> getFilesByServer(Server server) {
        return fileRepo.findFilesByServer(server);
    }

    public void deleteFilesByServer(Server server) {
        List<File> files = getFilesByServer(server);

        for (File file : files) {
            try {
                deleteFile(file);
            } catch (IOException e) {
                log.error("Cant delete file {}", file.getId());
            }
        }
    }

    private Path getUploadRootPath(Server server) throws IOException {
        Path basePath = Paths.get(fileStorageRoot);
        Path uploadPath = basePath.resolve(server.getId().toString());
        if (!Files.exists(uploadPath)) {
            try {
                return Files.createDirectories(uploadPath);
            } catch (IOException e) {
                throw new IOException("File Storage Path does not exist and could not be created: " + uploadPath.toString(), e);
            }
        }
        if (!Files.isDirectory(uploadPath, NOFOLLOW_LINKS)){
            throw new IOException("File Storage Path is not a directory!");
        }
        return uploadPath;
    }

    public Path getFilePath(File file) throws IOException {
        String fileName = file.getName();
        if (fileName == null || fileName.isEmpty()) {
            throw new RuntimeException("File name must not be empty!");
        }
        return getUploadRootPath(file.getServer()).resolve(fileName);
    }
    
    public static String getFileExtension(String fileName) {
        int idx = fileName.lastIndexOf('.');
        return (idx != -1) ? fileName.substring(idx + 1) : "";
    }
}
