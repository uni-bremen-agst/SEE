package de.unibremen.swt.see.manager.service;

import de.unibremen.swt.see.manager.model.Config;
import de.unibremen.swt.see.manager.model.File;
import de.unibremen.swt.see.manager.model.Server;
import de.unibremen.swt.see.manager.model.ServerStatusType;
import de.unibremen.swt.see.manager.repository.ConfigRepository;
import jakarta.validation.constraints.NotNull;
import java.io.IOException;
import java.util.List;
import java.util.Optional;
import java.util.Random;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

/**
 * Service class for managing container-related operations.
 * <p>
 * This service provides high-level operations for container management,
 * including creating, retrieving, updating, and deleting containers. It
 * encapsulates the business logic and acts as an intermediary between the
 * controller layer and the data access layer.
 * <p>
 * Container instances are managed by the {@link ServerService}.
 *
 * @see ServerService
 */
@Service
@Transactional
@Slf4j
@RequiredArgsConstructor
public class ContainerService {

    /**
     * Used to access the backend configuration.
     */
    private final ConfigRepository configRepo;
    /**
     * Used to access server files.
     */
    private final FileService fileService;

    /**
     * Used for the random port generation.
     */
    private final Random random = new Random();

    /**
     * Starts a container for the given server.
     *
     * @param server the server configuration
     * @return {@code true} if the container was started, else {@code false}
     */
    public boolean startContainer(@NotNull Server server) {
        Optional<Config> optConfig = configRepo.findConfigById(1);
        if (optConfig.isEmpty()) {
            log.error("Cant start server {}, cant find server config", server.getId());
            return false;
        }
        Config config = optConfig.get();
        List<File> files = fileService.getByServer(server);
        
        // FIXME This looks like a race condition
        if (server.getServerStatusType().equals(ServerStatusType.ONLINE)
                || server.getServerStatusType().equals(ServerStatusType.STARTING)
                || server.getServerStatusType().equals(ServerStatusType.STOPPING)) {
            log.error("Cant start stop server {}, server is online or busy", server.getId());
            return false;
        }

        // Aussuchen eines zufälligen Ports
        int port = getRandomPort(config.getMinContainerPort(), config.getMaxContainerPort());
        String containerName = server.getName() + "-" + server.getId() + "-" + port;

        // Starten des Gameservers
        server.setServerStatusType(ServerStatusType.STARTING);
        try {
            log.info("Starting server {}", server.getId());
            // Befehl zum Starten eines Docker-Containers anpassen
            String dockerCommand = "docker run -d --name " + containerName + " -p " + port + ":" + port + "/udp -e PASSWORD=\"" + server.getServerPassword() + "\" -e PORT=" + port + " -e SERVERID=" + server.getId() + " -e BACKENDDOMAIN=" + config.getDomain() + " see-gameserver:latest";

            // Docker-Befehl ausführen
            Process process = new ProcessBuilder()
                    .command("bash", "-c", dockerCommand)
                    .inheritIO()
                    .start();

            // Warte auf den Abschluss des Prozesses
            process.waitFor();

            String mkdirCommand = "docker exec " + containerName + " mkdir -p /app/gameserver_Data/StreamingAssets/Multiplayer/";
            Process createFolderCommand = new ProcessBuilder()
                    .command("bash", "-c", mkdirCommand)
                    .inheritIO()
                    .start();

            // Warte auf den Abschluss des Prozesses
            createFolderCommand.waitFor();

            boolean containsZip = false;

            // Kopieren der Daten in den Docker Container
            log.info("Adding files to server {}", server.getId());
            for (File file : files) {

                String command = "docker cp " + "\"" + fileService.getPath(file) + "\"" + " " + containerName + ":/app/gameserver_Data/StreamingAssets/Multiplayer/";
                Process copyProcess = new ProcessBuilder()
                        .command("bash", "-c", command)
                        .inheritIO()
                        .start();

                // Warte auf den Abschluss des Prozesses
                copyProcess.waitFor();

                if (file.getName().equals("src.zip")){

                    log.error("Filename: {}", file.getName());
                    containsZip = true;
                }
            }

            // Entpacken des Zip Archives
            if (containsZip){
                String unzipCommand = "docker exec " + containerName + " unzip /app/gameserver_Data/StreamingAssets/Multiplayer/src.zip -d /app/gameserver_Data/StreamingAssets/Multiplayer/src/";

                Process unzipProcess = new ProcessBuilder()
                        .command("bash", "-c", unzipCommand)
                        .inheritIO()
                        .start();

                // Warte auf den Abschluss des Prozesses
                unzipProcess.waitFor();
            }

            // Server in der Datenbank aktualisieren
            server.setContainerPort(port);
            server.setContainerName(containerName);
            server.setContainerAddress(config.getDomain());
            server.setServerStatusType(ServerStatusType.ONLINE);

            return true;

        } catch (IOException | InterruptedException e) {
            server.setServerStatusType(ServerStatusType.ERROR);
            log.error("Cant start server {}", server.getId());
        }
        return false;
    }

    /**
     * Stops the container for the given server.
     *
     * @param server the server configuration
     * @return {@code true} if the container was stopped, else {@code false}
     */
    public boolean stopContainer(@NotNull Server server) {
        if (server.getServerStatusType().equals(ServerStatusType.OFFLINE)
                || server.getServerStatusType().equals(ServerStatusType.STARTING)
                || server.getServerStatusType().equals(ServerStatusType.STOPPING)) {
            log.error("Cant start stop server {}, server is offline or busy", server.getId());
            return false;
        }
        server.setServerStatusType(ServerStatusType.STOPPING);
        try {
            log.info("Stopping server {}", server.getId());
            // Befehl zum Starten eines Docker-Containers anpassen
            String dockerStopCommand = "docker stop " + server.getContainerName();

            // Docker-Befehl ausführen
            Process processStop = new ProcessBuilder()
                    .command("bash", "-c", dockerStopCommand)
                    .inheritIO()
                    .start();

            // Warte auf den Abschluss des Prozesses
            processStop.waitFor();

            String dockerRemoveCommand = "docker rm " + server.getContainerName();

            // Docker-Befehl ausführen
            Process processRemove = new ProcessBuilder()
                    .command("bash", "-c", dockerRemoveCommand)
                    .inheritIO()
                    .start();

            // Warte auf den Abschluss des Prozesses
            processRemove.waitFor();

            server.setContainerPort(null);
            server.setContainerName(null);
            server.setContainerAddress(null);
            server.setServerStatusType(ServerStatusType.OFFLINE);
            return true;

        } catch (IOException | InterruptedException e) {
            server.setServerStatusType(ServerStatusType.ERROR);
            log.error("Cant stop server {}", server.getId());
        }
        return false;
    }

    /**
     * Generates a random port in the defined range.
     *
     * @param min lower bound of the port range
     * @param max upper bound of the port range
     * @return random port number
     */
    public int getRandomPort(int min, int max) {
        return random.nextInt(max - min) + min;
    }

    /**
     * Checks if a container is running for given server.
     *
     * @param server the server configuration
     * @return {@code true} if a container is running for the server, else
     * {@code false}.
     */
    public boolean isRunning(Server server) {
        // FIXME implement me
        throw new RuntimeException("Not implemented.");
    }
}
