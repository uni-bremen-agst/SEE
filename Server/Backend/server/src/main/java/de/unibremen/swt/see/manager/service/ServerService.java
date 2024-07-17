package de.unibremen.swt.see.manager.service;

import com.github.dockerjava.api.exception.InternalServerErrorException;
import com.github.dockerjava.api.exception.NotFoundException;
import com.github.dockerjava.api.exception.NotModifiedException;
import de.unibremen.swt.see.manager.model.Config;
import de.unibremen.swt.see.manager.model.File;
import de.unibremen.swt.see.manager.model.FileType;
import de.unibremen.swt.see.manager.model.Server;
import de.unibremen.swt.see.manager.model.ServerStatusType;
import de.unibremen.swt.see.manager.repository.ConfigRepository;
import de.unibremen.swt.see.manager.repository.ServerRepository;
import jakarta.persistence.EntityManager;
import jakarta.persistence.EntityNotFoundException;
import jakarta.persistence.LockModeType;
import jakarta.persistence.PersistenceContext;
import java.io.IOException;
import java.security.SecureRandom;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.*;
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
     * Used for entity access with locking to prevent race conditions in server
     * operations.
     */
    @PersistenceContext
    private EntityManager entityManager;

    /**
     * The external address of the Docker server.
     * <p>
     * This is the address that any game server instance running in a container
     * is accessible by.
     */
    @Value("${see.app.docker.host.external}")
    private String externalDockerHost;

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
     * <p>
     * This function retrieves the data read-only so that locked entries can be
     * retrieved as well.
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
     *
     * @param server the server to be created
     * @return the newly created server, or {@code null}
     */
    public Server create(Server server) {
        log.info("Saving server {}", server.getName());
        server.setContainerAddress(externalDockerHost);

        UUID serverId = server.getId();
        if (serverId != null && serverRepo.findServerById(serverId).isPresent()) {
            throw new RuntimeException("The server is already present in the database!");
        }

        Integer port = generatePort();
        if (port == null) {
            log.error("Not able to assign unique port after several tries!");
            return null;
        }
        server.setContainerPort(port);

        server.setServerPassword(generatePassword(PASSWORD_LENGTH));
        server = serverRepo.save(server);
        return server;
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
    public File addFile(UUID serverId, String fileTypeStr, MultipartFile multipartFile) {
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
     */
    public void delete(UUID id) throws IOException {
        log.info("Deleting server {}", id);
        // Get the server entity and lock access to prevent race conditions
        final Server server = entityManager.find(Server.class, id, LockModeType.PESSIMISTIC_WRITE);

        try {
            containerService.deleteContainer(server);
        } catch (NotFoundException e) {
            // Ignore
        } catch (Exception e) {
            // TODO A broken pipe can occur during this process.
            throw new IllegalStateException("Try again later.", e);
        }

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
    public void start(UUID id) throws IOException, IllegalStateException {
        log.info("Starting server {}", id);
        // Get the server entity and lock access to prevent race conditions
        final Server server = entityManager.find(Server.class, id, LockModeType.PESSIMISTIC_WRITE);
        try {
            containerService.startContainer(server);
        } catch (NotModifiedException e) {
            throw new IllegalStateException("The container is already running!", e);
        } catch (InternalServerErrorException e) {
            throw new IllegalStateException("Internal server error!", e);
        }
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
    public void stop(UUID id) throws IllegalStateException {
        log.info("Stopping server {}", id);
        // Get the server entity and lock access to prevent race conditions
        final Server server = entityManager.find(Server.class, id, LockModeType.PESSIMISTIC_WRITE);

        try {
            containerService.stopContainer(server);
        } catch (NotFoundException e) {
            throw new IllegalStateException("The container to be stopped does not exist!", e);
        } catch (NotModifiedException e) {
            server.setServerStatusType(ServerStatusType.OFFLINE);
            throw new IllegalStateException("The container is already stopped!", e);
        } catch (Exception e) {
            // TODO A broken pipe can occur during this process.
            throw new IllegalStateException("Try again later.", e);
        }
        server.setServerStatusType(ServerStatusType.OFFLINE);
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
    private Integer generatePort() {
        final Config config = resolveConfig();
        final int min = config.getMinContainerPort();
        final int max = config.getMaxContainerPort();

        Integer port = null;
        for (int tries = 10; tries > 0; tries--) {
            final int newPort = random.nextInt(max - min) + min;
            if (serverRepo.findServerByContainerPort(newPort).isEmpty()) {
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
    private String generatePassword(final int length) {
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
        final Optional<Config> optConfig = configRepo.findConfigById(1);
        if (optConfig.isEmpty()) {
            throw new RuntimeException("Server configuration could not be found!");
        }
        return optConfig.get();
    }

}
