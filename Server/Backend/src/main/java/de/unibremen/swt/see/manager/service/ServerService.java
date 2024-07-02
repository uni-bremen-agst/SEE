package de.unibremen.swt.see.manager.service;

import de.unibremen.swt.see.manager.model.File;
import de.unibremen.swt.see.manager.model.FileType;
import de.unibremen.swt.see.manager.model.Server;
import de.unibremen.swt.see.manager.repository.ServerRepository;
import java.io.IOException;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.*;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.multipart.MultipartFile;

/**
 * Service class for managing server-related operations.
 * <p>
 * This service provides high-level operations for server management, including
 * creating, retrieving, updating, and deleting servers. It encapsulates the
 * business logic and acts as an intermediary between the controller layer and
 * the data access layer.
 * <p>
 * Server instances are executed using the {@link ContainerService}.
 *
 * @see ServerRepository
 * @see de.unibremen.swt.see.manager.controller.ServerController
 * @see ContainerService
 */
@Service
@Transactional
@Slf4j
@RequiredArgsConstructor
public class ServerService {

    /**
     * Enables server data persistence and retrieval for this service.
     */
    private final ServerRepository serverRepo;

    /**
     * Used to access files.
     */
    private final FileService fileService;
    /**
     * Used to execute server instances.
     */
    private final ContainerService containerService;

    /**
     * Retrieves a server by its ID.
     *
     * @param id the ID of the server
     * @return server if found, or {@code null} if not found
     */
    public Server get(UUID id) {
        log.info("Fetching server {}", id);
        return serverRepo.findServerById(id).orElse(null);
    }

    /**
     * Retrieves all servers.
     *
     * @return a list containing all servers
     */
    public List<Server> getAll() {
        log.info("Fetching all servers");
        return serverRepo.findAll();
    }

    /**
     * Saves or updates the given server in the database.
     * <p>
     * If the entity has no ID, it will be inserted as a new record. If the
     * entity has an ID, it will update the existing record.
     *
     * @param server the server to be saved or updated
     * @return the saved server containing an ID
     */
    public Server save(Server server) {
        log.info("Saving server {}", server.getName());
        return serverRepo.save(server);
    }

    /**
     * Adds a new file to a server by its ID.
     * <p>
     * The file will be crated and associated to the given server.
     *
     * @param serverId the ID identifying the server instance
     * @param fileTypeStr the type of the file
     * @param multipartFile the file content
     * @return the created file, or {@code null} if the server was not found or
     * an error occurred while storing the file
     */
    public File addFileToServer(UUID serverId, String fileTypeStr, MultipartFile multipartFile) {
        Optional<Server> optServer = serverRepo.findServerById(serverId);
        if (optServer.isEmpty()) {
            log.error("Server not found with ID: {}", serverId);
            return null;
        }
        Server server = optServer.get();
        
        FileType fileType = FileType.valueOf(fileTypeStr);
        log.info("Adding file {} to server {}", multipartFile.getOriginalFilename(), server.getName());

        try {
            return fileService.create(server, fileType, multipartFile);
        } catch (IOException e) {
            log.error("Unable to add file to server {}: ", serverId, e);
        }
        return null;
    }

    /**
     * Retrieves all files for a specific server identified by its ID.
     *
     * @param id the ID of the server
     * @return a list containing all files of the given server
     */
    public List<File> getFilesForServer(UUID id) {
        Optional<Server> optServer = serverRepo.findServerById(id);
        if (optServer.isEmpty()) {
            return Collections.emptyList();
        }
        Server server = optServer.get();

        log.info("Fetching files for server {}", id);
        return fileService.getByServer(server);
    }

    /**
     * Deletes a server and its files.
     *
     * @param id the ID of the server
     * @return {@code true} if the server and its files are deleted, else
     * {@code false}.
     */
    public boolean deleteServer(UUID id) {
        log.info("Deleting server {}", id);
        Server server = get(id);
        if (server == null) {
            return false;
        }

        if (!stop(server)) {
            return false;
        }

        if (!fileService.deleteFilesByServer(server)) {
            return false;
        }

        serverRepo.deleteServerById(id);
        return true;
    }

    /**
     * Start a server.
     *
     * @param server server to be started
     * @return {@code true} if the server was started or was already running,
     * else {@code false}
     */
    public boolean start(Server server) {
        if (containerService.isRunning(server)) {
            return true;
        }

        boolean success = containerService.startContainer(server);

        server.setStopTime(null);
        server.setStartTime(ZonedDateTime.now(ZoneId.of("UTC")));

        return success;
    }

    /**
     * Start a server by its ID.
     *
     * @param id the ID of the server to be started
     * @return {@code true} if the server was started or was already running,
     * else {@code false}
     */
    public boolean start(UUID id) {
        Server server = get(id);
        if (server == null) {
            return false;
        }

        return start(server);
    }

    /**
     * Stop a server.
     *
     * @param server server to be stopped
     * @return {@code true} if the server was stopped or was not running, else
     * {@code false}
     */
    public boolean stop(Server server) {
        if (!containerService.isRunning(server)) {
            return true;
        }

        boolean success = containerService.stopContainer(server);

        server.setStartTime(null);
        server.setStopTime(ZonedDateTime.now(ZoneId.of("UTC")));

        return success;
    }

    /**
     * Stop a server by its ID.
     *
     * @param id the ID of the server to be stopped
     * @return {@code true} if the server was stopped or was not running, else
     * {@code false}
     */
    public boolean stop(UUID id) {
        Server server = get(id);
        if (server == null) {
            return false;
        }

        return stop(server);
    }
}
