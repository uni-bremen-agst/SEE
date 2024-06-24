package de.unibremen.swt.see.manager.service;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.multipart.MultipartFile;
import de.unibremen.swt.see.manager.util.FileType;
import de.unibremen.swt.see.manager.file.payload.PayloadFile;
import de.unibremen.swt.see.manager.model.File;
import de.unibremen.swt.see.manager.model.Server;
import de.unibremen.swt.see.manager.repo.FileRepo;

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

    public File createFile(MultipartFile multipartFile) {
        if (multipartFile.isEmpty()) {
            return null;
        }
        File file = new File();
        file.setContentType(multipartFile.getContentType());
        file.setOriginalFileName(multipartFile.getOriginalFilename());
        File savedFile = fileRepo.save(file);

        try {
            storeFile(savedFile.getId().toString(), multipartFile);
        } catch (IOException e) {
            fileRepo.delete(savedFile);
            throw new IllegalStateException("Error persisting file.", e);
        }
        return savedFile;
    }

    private void storeFile(String fileName, MultipartFile multipartFile) throws IOException {
        if (fileName == null || fileName.isEmpty()) {
            throw new IOException("File name must not be empty!");
        }

        Path filePath = getUploadPath().resolve(fileName);
        if (Files.exists(filePath)) {
            throw new IOException("File already exists: " + filePath.toString());
        }
        
        try (InputStream inputStream = multipartFile.getInputStream()) {
            Files.copy(inputStream, filePath);
        } catch (IOException e) {
            throw new IOException("Unable to save file: " + fileName, e);
        }
    }

    public PayloadFile getFile(UUID fileId) throws IOException {
        PayloadFile payloadFile = new PayloadFile();
        Optional<File> file = fileRepo.findById(fileId);
        if (file.isEmpty()) {
            log.error("File not found in db: {}", fileId);
            return null;
        }
        
        String fileIdStr = fileId.toString();
        Path filePath = getUploadPath().resolve(fileIdStr);
        if (!Files.exists(filePath)) {
            log.error("File not found on filesystem: {}", fileIdStr);
            return null;
        }
        
        // FIXME Do not copy whole file into memory!
        payloadFile.setContent(Files.readAllBytes(filePath));
        payloadFile.setId(fileIdStr);

        // TODO This overrides the original file name. Should we store the intended file name in db instead?
        switch (file.get().getFileType()) {
            case CSV -> payloadFile.setOriginalFileName("multiplayer.csv");
            case CONFIG -> payloadFile.setOriginalFileName("multiplayer.cfg");
            case GXL -> payloadFile.setOriginalFileName("multiplayer.gxl");
            case SOLUTION ->
                    payloadFile.setOriginalFileName("solution." + file.get().getOriginalFileName().split("\\.")[file.get().getOriginalFileName().split("\\.").length - 1]);
            case SOURCE -> payloadFile.setOriginalFileName("src.zip");
        }
//        payloadFile.setOriginalFileName(file.get().getOriginalFileName());

        payloadFile.setContentType(file.get().getContentType());
        payloadFile.setCreationTime(file.get().getCreationTime());

        log.info("Fetched file {}", fileId);
        return payloadFile;
    }

    public PayloadFile getFileByServerAndFileType(Server server, FileType fileType) throws IOException {
        log.info("Fetching file for server {} and type {}", server, fileType);

        Optional<File> file = fileRepo.findFileByServerAndFileType(server, fileType);
        if (file.isEmpty()) {
            log.error("File not found in db for server {} with type {}", server.getId(), fileType);
            return null;
        }
        
        return getFile(file.get().getId());
    }

    public boolean deleteFile(UUID fileId) throws IOException {
        log.info("Removing file {}", fileId);
        
        Optional<File> file = fileRepo.findById(fileId);
        if (file.isEmpty()) {
            log.error("File not found in db: {}", fileId);
            return false;
        }
        
        Path filePath = getUploadPath().resolve(fileId.toString());
        if (!Files.exists(filePath)) {
            log.warn("File already deleted: {}", fileId.toString());
            return true;
        }
        if (!Files.isRegularFile(filePath)) {
            log.error("Not a regular file: {}", fileId.toString());
            return false;
        }
        Files.delete(filePath);
        fileRepo.delete(file.get());

        return true;
    }


    public List<File> getFilesByServer(Server server) {
        return fileRepo.findFilesByServer(server);
    }

    public void deleteFilesByServer(Server server) {
        List<File> files = getFilesByServer(server);

        for (File file : files) {
            try {
                deleteFile(file.getId());
            } catch (Exception e) {
                log.error("Cant delete file {}", file.getId());
            }
        }
    }
    
    private Path getUploadPath() throws IOException {
        Path uploadPath = Paths.get(fileStorageRoot);
        if (!Files.exists(uploadPath)) {
            try {
                Files.createDirectories(uploadPath);
            } catch (IOException e) {
                throw new IOException("File Storage Path does not exist and could not be created: " + uploadPath.toString(), e);
            }
        }
        if (!Files.isDirectory(uploadPath, NOFOLLOW_LINKS)){
            throw new IOException("File Storage Path is not a directory!");
        }
        return uploadPath;
    }
}
