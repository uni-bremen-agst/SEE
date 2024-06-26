package de.unibremen.swt.see.manager.service;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.multipart.MultipartFile;
import de.unibremen.swt.see.manager.util.FileType;
import de.unibremen.swt.see.manager.model.File;
import de.unibremen.swt.see.manager.model.Server;
import de.unibremen.swt.see.manager.repo.ServerRepo;
import java.io.IOException;

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

    public File addFileToServer(UUID serverId, String fileTypeStr, MultipartFile multipartFile) {
        Optional<Server> optServer = serverRepo.findServerById(serverId);
        if (optServer.isEmpty()) {
            log.error("Cant add file to server {} (server is null)", serverId);
            return null;
        }
        Server server = optServer.get();
        
        FileType fileType = FileType.valueOf(fileTypeStr);
        log.info("Adding file {} to server {}", multipartFile.getOriginalFilename(), server.getName());

        try {
            return fileService.createFile(server, fileType, multipartFile);
        } catch (IOException e) {
            log.error("Unable to add file to server {}: ", serverId, e);
        }
        return null;
    }

    public List<File> getFilesForServer(UUID id) {
        Optional<Server> optServer = serverRepo.findServerById(id);
        if (optServer.isEmpty()) {
            return Collections.emptyList();
        }
        Server server = optServer.get();

        log.info("Fetching files for server {}", id);
        return fileService.getFilesByServer(server);
    }

    public boolean deleteServer(UUID id) {
        log.info("Deleting server {}", id);
        stopServer(id);
        fileService.deleteFilesByServer(serverRepo.findServerById(id).orElse(null));
        serverRepo.deleteServerById(id);

        if (serverRepo.findServerById(id).isPresent()) {
            log.error("Server was not deleted: {}", id);
            return false;
        }
        return true;
    }

    public boolean startServer(UUID id) {
        Optional<Server> optServer = serverRepo.findServerById(id);
        if (optServer.isEmpty()) {
            log.error("Cant find server {}", id);
            return false;
        }
        Server server = optServer.get();

        server.setStopTime(null);
        server.setStartTime(ZonedDateTime.now(ZoneId.of("UTC")));

        boolean success = containerService.startContainer(server);

        return success;
    }

    public boolean stopServer(UUID id) {
        Optional<Server> optServer = serverRepo.findServerById(id);
        if (optServer.isEmpty()) {
            log.error("Cant find server {}", id);
            return false;
        }
        Server server = optServer.get();
        
        server.setStartTime(null);
        server.setStopTime(ZonedDateTime.now(ZoneId.of("UTC")));
        return containerService.stopContainer(server);
    }
}
