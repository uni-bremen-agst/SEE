package de.unibremen.swt.see.manager.service;

import de.unibremen.swt.see.manager.model.File;
import de.unibremen.swt.see.manager.model.FileType;
import de.unibremen.swt.see.manager.model.Server;
import de.unibremen.swt.see.manager.model.ServerStatusType;
import de.unibremen.swt.see.manager.repository.ServerRepository;
import jakarta.persistence.EntityManager;
import jakarta.persistence.EntityNotFoundException;
import jakarta.persistence.LockModeType;
import jakarta.persistence.PersistenceContext;
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
     * Used for entity access with locking to prevent race conditions in server
     * operations.
     */
    @PersistenceContext
    private EntityManager entityManager;

    /**
     * Retrieves a server by its ID (read-only).
     *
     * @param id the ID of the server
     * @return server if found, or {@code null} if not found
     */
    @Transactional(readOnly = true)
    public Server get(UUID id) {
        log.info("Fetching server {}", id);
        return serverRepo.findServerById(id).orElse(null);
    }

    /**
     * Retrieves all servers read-only.
     *
     * @return a list containing all servers
     */
    @Transactional(readOnly = true)
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
     * <p>
     * This method will lock access to the {@link Server} object to prevent race
     * conditions.
     *
     * @param id the ID of the server to be deleted
     * @throws IOException if there is an error during file deletion
     * @throws IllegalStateException if the server is busy
     * @throws RuntimeException if there is an error during server deletion
     */
    public void delete(UUID id) throws IOException {
        log.info("Deleting server {}", id);

        // Get the server entity and lock access to prevent race conditions
        final Server server = entityManager.find(Server.class, id, LockModeType.PESSIMISTIC_WRITE);

        if (server == null || !server.getServerStatusType().equals(ServerStatusType.OFFLINE)) {
            throw new IllegalStateException("Server is running!");
        }

        containerService.deleteContainer(server);
        fileService.deleteFilesByServer(server);
        entityManager.remove(server);
    }

    /**
     * Start a server by its ID.
     * <p>
     * This method will lock access to the {@link Server} object to prevent race
     * conditions.
     *
     * @param id the ID of the server to be started
     * @throws java.io.IOException if there is an error accessing server files
     * @throws IllegalStateException if the server is busy or already online
     */
    public void start(UUID id) throws IOException {
        log.info("Starting server {}", id);
        // Get the server entity and lock access to prevent race conditions
        final Server server = entityManager.find(Server.class, id, LockModeType.PESSIMISTIC_WRITE);
        containerService.startContainer(server);
        server.setStopTime(null);
        server.setStartTime(ZonedDateTime.now(ZoneId.of("UTC")));
    }

    /**
     * Stop a server by its ID.
     * <p>
     * This method will lock access to the {@link Server} object to prevent race
     * conditions.
     *
     * @param id the ID of the server to be stopped
     * @throws IllegalStateException if the server is busy or already stopped
     */
    public void stop(UUID id) {
        log.info("Stopping server {}", id);
        // Get the server entity and lock access to prevent race conditions
        final Server server = entityManager.find(Server.class, id, LockModeType.PESSIMISTIC_WRITE);

        containerService.stopContainer(server);
        server.setStartTime(null);
        server.setStopTime(ZonedDateTime.now(ZoneId.of("UTC")));
    }

    /**
     * Evaluates if given server can be accessed with given password.
     *
     * @param serverId the ID of the server to be accessed
     * @param roomPassword the password to access the server
     * @return {@code true} if the password is correct or no password is set for
     * given server, {@code false} if access cannot be granted.
     * @throws EntityNotFoundException if the server does not exist
     */
    public boolean validateAccess(UUID serverId, String roomPassword) {
        final Server server = serverRepo.findServerById(serverId).orElse(null);
        if (server == null) {
            throw new EntityNotFoundException("No entity found with ID " + serverId);
        }
        return (server.getServerPassword() == null || server.getServerPassword().isEmpty() || server.getServerPassword().equals(roomPassword));
    }

}
