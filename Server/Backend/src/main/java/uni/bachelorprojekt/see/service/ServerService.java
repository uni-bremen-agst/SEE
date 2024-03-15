package uni.bachelorprojekt.see.service;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.multipart.MultipartFile;
import uni.bachelorprojekt.see.Util.FileType;
import uni.bachelorprojekt.see.model.File;
import uni.bachelorprojekt.see.model.Server;
import uni.bachelorprojekt.see.repo.ServerRepo;

import java.io.FileOutputStream;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.*;

@Service
@Transactional
@Slf4j
@RequiredArgsConstructor
public class ServerService {
    private final ServerRepo serverRepo;
    private final FileService fileService;
    private final ContainerService containerService;

    public Server getServerByID(UUID id) {
        log.info("Fetching server {}", id);
        return serverRepo.findServerById(id).orElse(null);
    }

    public List<Server> getAllServer() {
        log.info("Fetching all servers");
        return serverRepo.findAll();
    }

    public Server saveServer(Server server) {
        log.info("Saving server {}", server.getName());
        return serverRepo.save(server);
    }

    public File addFileToServer(UUID id, String fileType, MultipartFile multipartFile) {
        Optional<Server> server = serverRepo.findServerById(id);
        if (server.isEmpty()) {
            log.error("Cant add file to server {} (server is null)", id);
            return null;
        }
        File file = fileService.createFile(multipartFile);
        if (file == null) {
            log.error("Cant add file to server {} (file is null)", id);
            return null;
        }
        log.info("Adding file {} to server {}", file.getOriginalFileName(), server.get().getName());
        file.setFileType(FileType.valueOf(fileType));
        file.setServer(server.get());
        return file;
    }

    public List<File> getFilesForServer(UUID id) {
        Optional<Server> server = serverRepo.findServerById(id);
        if (server.isEmpty()) {
            return Collections.emptyList();
        }
        log.info("Fetching files for server {}", id);
        return fileService.getFilesByServer(server.get());
    }

    public boolean deleteServer(UUID id) {
        stopServer(id);
        fileService.deleteFilesByServer(serverRepo.findServerById(id).orElse(null));
        serverRepo.deleteServerById(id);

        if (serverRepo.findServerById(id).isPresent()) {
            log.error("Cant delete server {}. Server is null", id);
            return false;
        }
        log.info("Deleting server {}", id);
        return true;
    }

    public boolean startServer(UUID id) {
        Optional<Server> server = serverRepo.findServerById(id);
        if (server.isEmpty()) {
            log.error("Cant find server {}", id);
            return false;
        }

        server.get().setStopTime(null);
        server.get().setStartTime(ZonedDateTime.now(ZoneId.of("UTC")));

        List<File> files = getFilesForServer(server.get().getId());
        List<java.io.File> realFiles = new ArrayList<>();

        try {
            for (File file : files) {
                java.io.File outputFile = null;

                switch (file.getFileType()){
                    case CSV ->  outputFile = new java.io.File("multiplayer.csv");
                    case CONFIG -> outputFile = new java.io.File("multiplayer.cfg");
                    case GXL -> outputFile = new java.io.File("multiplayer.gxl");
                    case SOLUTION -> outputFile = new java.io.File("solution." + file.getOriginalFileName().split("\\.")[file.getOriginalFileName().split("\\.").length-1]);
                    case SOURCE -> outputFile = new java.io.File("src.zip");
                }

                try (FileOutputStream outputStream = new FileOutputStream(outputFile)) {
                    outputStream.write(fileService.getFile(file.getId()).getContent());
                }
                realFiles.add(outputFile);
            }
        } catch (Exception e) {
            log.error("Cant fetch files for server {}", server.get().getId());
        }

        boolean success = containerService.startContainer(server.get(), realFiles);

        for (java.io.File file : realFiles) {
            file.delete();
        }

        return success;
    }


    public boolean stopServer(UUID id) {
        Optional<Server> server = serverRepo.findServerById(id);
        if (server.isEmpty()) {
            log.error("Cant find server {}", id);
            return false;
        }
        server.get().setStartTime(null);
        server.get().setStopTime(ZonedDateTime.now(ZoneId.of("UTC")));
        return server.filter(containerService::stopContainer).isPresent();
    }
}
