package de.unibremen.swt.see.manager.service;

import com.github.dockerjava.api.exception.InternalServerErrorException;
import com.github.dockerjava.api.exception.NotFoundException;
import com.github.dockerjava.api.exception.NotModifiedException;
import de.unibremen.swt.see.manager.model.Config;
import de.unibremen.swt.see.manager.model.File;
import de.unibremen.swt.see.manager.model.ProjectType;
import de.unibremen.swt.see.manager.model.RoleType;
import de.unibremen.swt.see.manager.model.Server;
import de.unibremen.swt.see.manager.model.ServerStatusType;
import de.unibremen.swt.see.manager.model.User;
import de.unibremen.swt.see.manager.repository.ConfigRepository;
import de.unibremen.swt.see.manager.repository.ServerRepository;
import de.unibremen.swt.see.manager.util.ServerLockManager;
import jakarta.persistence.EntityNotFoundException;
import java.io.IOException;
import java.security.SecureRandom;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.*;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.locks.Lock;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
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
     * Timeout in seconds that is waited to acquire a lock on write operations.
     */
    private final static int LOCK_TIMEOUT = 15;

    /**
     * Used to access the back-end configuration.
     */
    private final ConfigRepository configRepo;

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
     * Used to create/delete a user for a server.
     */
    private final UserService userService;

    /**
     * The external address of the Docker server.
     * <p>
     * This is the address that any game server instance running in a container
     * is accessible by.
     */
    @Value("${see.app.docker.host.external}")
    private String externalDockerHost;

    /**
     * The lock manager for concurrent writes.
     */
    private final ServerLockManager lockManager = ServerLockManager.getInstance();

    /**
     * Used for the random port generation.
     */
    private final Random random = new Random();

    /**
     * Used for room password generation.
     */
    private final SecureRandom secureRandom = new SecureRandom();

    /**
     * Characters used for room password generation.
     */
    private final static String PASSWORD_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+";

    /**
     * Length of generated room passwords.
     */
    private final static int PASSWORD_LENGTH = 24;

    /**
     * Retrieves a server by its ID.
     *
     * @param id the ID of the server
     * @return the server if found, or {@code null} if not found
     */
    @Transactional(readOnly = true)
    public Server get(UUID id) {
        log.info("Fetching server {}", id);
        return serverRepo.findById(id).orElse(null);
    }

    /**
     * Retrieves all servers.
     * <p>
     * This function retrieves the data read-only so that locked entries can be
     * retrieved as well.
     *
     * @return a list containing all servers
     */
    @Transactional(readOnly = true)
    public List<Server> getAll() {
        log.info("Fetching all servers");
        return serverRepo.findAll();
    }

    /**
     * Creates a new server with the given data.
     * <p>
     * Attributes {@code containerAddress} and {@code containerPort} will be set
     * automatically. A unique random port number will be generated. If port
     * assignment fails after several tries, the server cannot be created. If
     * that happens regularly, it might be a good idea to configure a larger
     * port range.
     * <p>
     * A user will be created with a random password to access data associated
     * with the server.
     *
     * @param server the server to be created
     * @return the newly created server, or {@code null}
     */
    public Server create(Server server) {
        log.info("Saving server {}", server.getName());
        server.setContainerAddress(externalDockerHost);

        UUID serverId = server.getId();
        if (serverId != null && serverRepo.findById(serverId).isPresent()) {
            throw new RuntimeException("The server is already present in the database!");
        }

        Integer port = generatePort();
        if (port == null) {
            log.error("Not able to assign unique port after several tries!");
            return null;
        }
        server.setContainerPort(port);

        final String password = generatePassword(PASSWORD_LENGTH);
        server.setServerPassword(password);
        server = serverRepo.save(server);
        serverId = server.getId();

        final User user = userService.create(serverId.toString(), password, RoleType.ROLE_USER);
        userService.addServer(user, server);

        return server;
    }

    /**
     * Adds a new file to a server by its ID.
     * <p>
     * The file will be crated and associated to the given server.
     *
     * @param serverId the ID identifying the server instance
     * @param projectTypeStr the project type of the file
     * @param multipartFile the file content
     * @return the created file, or {@code null} if the server was not found or
     * an error occurred while storing the file
     */
    public File addFile(UUID serverId, String projectTypeStr, MultipartFile multipartFile) {
        Optional<Server> optServer = serverRepo.findById(serverId);
        if (optServer.isEmpty()) {
            log.error("Server not found with ID: {}", serverId);
            return null;
        }
        Server server = optServer.get();

        ProjectType projectType = ProjectType.valueOf(projectTypeStr);
        log.info("Adding file {} to server {}", multipartFile.getOriginalFilename(), server.getName());

        try {
            return fileService.create(server, projectType, multipartFile);
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
        Optional<Server> optServer = serverRepo.findById(id);
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
     * This method acquires a write lock on the server entity to synchronize
     * write operations.
     * <p>
     * The user that was created along with the server will be deleted as well.
     *
     * @param id the ID of the server to be deleted
     * @throws EntityNotFoundException if the server does not exist
     * @throws IOException if there is an error during file deletion
     * @throws IllegalStateException if the server is busy
     */
    public void delete(UUID id) throws EntityNotFoundException, IOException, IllegalStateException {
        final Server server = serverRepo.findById(id).orElse(null);
        if (server == null) {
            throw new EntityNotFoundException("No server found with ID " + id);
        }

        final Lock lock = lockManager.getLock(id);
        try {
            if (lock.tryLock(LOCK_TIMEOUT, TimeUnit.SECONDS)) {
                log.debug("Lock acquired: {}", server.getId());
            } else {
                log.debug("Timeout while waiting for lock: {}", id);
                throw new IllegalStateException("Try again later.");
            }
        } catch (InterruptedException ex) {
            log.debug("Interrupted while waiting for lock: {}", id);
            throw new IllegalStateException("The process was interrupted.");
        }

        try {
            log.info("Deleting server {}", id);
            try {
                containerService.deleteContainer(server);
            } catch (NotFoundException e) {
                // Ignore missing container
            }

            fileService.deleteFilesByServer(server);
            serverRepo.deleteById(id);
            userService.deleteByUsername(id.toString());
            lockManager.removeLock(id);
        } finally {
            lock.unlock();
            log.debug("Lock released: {}", id);
        }
    }

    /**
     * Start a server by its ID.
     * <p>
     * This method acquires a write lock on the server entity to synchronize
     * write operations.
     *
     * @param id the ID of the server to be started
     * @throws EntityNotFoundException if the server does not exist
     * @throws IOException if there is an error accessing server files
     * @throws IllegalStateException if the server is busy or already online
     */
    public void start(UUID id) throws EntityNotFoundException, IOException, IllegalStateException {
        final Server server = serverRepo.findById(id).orElse(null);
        if (server == null) {
            throw new EntityNotFoundException("No server found with ID " + id);
        }

        final Lock lock = lockManager.getLock(id);
        try {
            if (lock.tryLock(LOCK_TIMEOUT, TimeUnit.SECONDS)) {
                log.debug("Lock acquired: {}", server.getId());
            } else {
                log.debug("Timeout while waiting for lock: {}", id);
                throw new IllegalStateException("Try again later.");
            }
        } catch (InterruptedException ex) {
            log.debug("Interrupted while waiting for lock: {}", id);
            throw new IllegalStateException("The process was interrupted.");
        }

        try {
            log.info("Starting server {}", id);

            try {
                containerService.startContainer(server);
            } catch (NotModifiedException e) {
                throw new IllegalStateException("The container is already running!", e);
            } catch (NotFoundException e) {
                // This should not happen except due to external influence or
                // concurrent requests, as the container is created above if missing.
                throw new IllegalStateException("The container vanished!", e);
            } catch (InternalServerErrorException e) {
                throw new IllegalStateException("Internal server error!", e);
            }
            server.setStopTime(null);
            server.setStartTime(ZonedDateTime.now(ZoneId.of("UTC")));
        } finally {
            lock.unlock();
            log.debug("Lock released: {}", id);
        }
    }

    /**
     * Stop a server by its ID.
     * <p>
     * This method acquires a write lock on the server entity to synchronize
     * write operations.
     *
     * @param id the ID of the server to be stopped
     * @throws EntityNotFoundException if the server does not exist
     * @throws IllegalStateException if the server is busy or already stopped
     */
    public void stop(UUID id) throws EntityNotFoundException, IllegalStateException {
        final Server server = serverRepo.findById(id).orElse(null);
        if (server == null) {
            throw new EntityNotFoundException("No server found with ID " + id);
        }

        final Lock lock = lockManager.getLock(server);
        try {
            if (lock.tryLock(LOCK_TIMEOUT, TimeUnit.SECONDS)) {
                log.debug("Lock acquired: {}", server.getId());
            } else {
                log.debug("Timeout while waiting for lock: {}", id);
                throw new IllegalStateException("Try again later.");
            }
        } catch (InterruptedException ex) {
            log.debug("Interrupted while waiting for lock: {}", server.getId());
            throw new IllegalStateException("The process was interrupted.");
        }

        try {
            log.info("Stopping server {}", id);
            containerService.stopContainer(server);
        } catch (NotFoundException e) {
            throw new IllegalStateException("The container to be stopped does not exist!", e);
        } catch (NotModifiedException e) {
            server.setStatus(ServerStatusType.OFFLINE);
            throw new IllegalStateException("The container is already stopped!", e);
        } finally {
            lock.unlock();
            log.debug("Lock released: {}", id);
        }

        server.setStatus(ServerStatusType.OFFLINE);
        server.setStartTime(null);
        server.setStopTime(ZonedDateTime.now(ZoneId.of("UTC")));
    }

    /**
     * Update the server status based on its container state.
     * <p>
     * This method acquires a write lock on the server entity to synchronize
     * write operations if the state has changed. The acquisition timeout is set
     * to 0 to prevent outdated status updates.
     *
     * @param server the server to update the status for
     */
    public void updateStatus(Server server) {
        ServerStatusType newStatus = containerService.isRunning(server)
                ? ServerStatusType.ONLINE
                : ServerStatusType.OFFLINE;
        if (server.getStatus() == newStatus) {
            return;
        }

        final Lock lock = lockManager.getLock(server);
        try {
            if (lock.tryLock(0, TimeUnit.SECONDS)) {
                log.debug("Lock acquired: {}", server.getId());
            } else {
                log.debug("Timeout while waiting for lock: {}", server.getId());
                return;
            }
        } catch (InterruptedException ex) {
            log.debug("Interrupted while waiting for lock: {}", server.getId());
            return;
        }

        try {
            server.setStatus(newStatus);

        } finally {
            lock.unlock();
            log.debug("Lock released: {}", server.getId());
        }
    }

    /**
     * Convenience function to update all server status.
     */
    public void updateStatus() {
        for (Server server : getAll()) {
            updateStatus(server);
        }
    }

    /**
     * Generates a pseudo-random port in the range defined in the server
     * settings.
     * <p>
     * Checks if the port is already used and tries several times to assign a
     * new random port.
     * <p>
     * This might still clash in some edge cases if the database is not locked
     * during creation (race condition).
     *
     * @return random port number
     */
    private synchronized Integer generatePort() {
        final Config config = resolveConfig();
        final int min = config.getMinContainerPort();
        final int max = config.getMaxContainerPort();

        Integer port = null;
        for (int tries = 10; tries > 0; tries--) {
            final int newPort = random.nextInt(max - min) + min;
            if (serverRepo.findByContainerPort(newPort).isEmpty()) {
                port = newPort;
                break;
            }
        }

        return port;
    }

    /**
     * Generates a pseudo-random string for use as server/room password.
     * <p>
     * This is a simple password generator that is not optimized to match high
     * security standards.
     *
     * @param length password length
     * @return generated password
     */
    private synchronized String generatePassword(final int length) {
        return secureRandom.ints(length, 0, PASSWORD_CHARS.length())
                .mapToObj(PASSWORD_CHARS::charAt)
                .collect(StringBuilder::new, StringBuilder::append, StringBuilder::append)
                .toString();
    }

    /**
     * Resolves the configuration.
     *
     * @return the configuration
     * @throws RuntimeException if the configuration could not be found
     */
    private Config resolveConfig() {
        List<Config> configs = configRepo.findAll();
        if (configs.isEmpty()) {
            throw new RuntimeException("Server configuration could not be found!");
        }
        return configs.get(0);
    }

}
