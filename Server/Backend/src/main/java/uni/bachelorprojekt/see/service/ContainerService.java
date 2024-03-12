package uni.bachelorprojekt.see.service;

import jakarta.validation.constraints.NotNull;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import uni.bachelorprojekt.see.Util.ServerStatusType;
import uni.bachelorprojekt.see.model.Server;
import uni.bachelorprojekt.see.model.ServerConfig;
import uni.bachelorprojekt.see.repo.ServerConfigRepo;

import java.io.File;
import java.io.IOException;
import java.util.List;
import java.util.Optional;
import java.util.Random;

@Service
@Transactional
@Slf4j
@RequiredArgsConstructor
public class ContainerService {
    private final ServerConfigRepo serverConfigRepo;

    public boolean startContainer(@NotNull Server server, List<File> files) {
        // Server Config laden
        Optional<ServerConfig> serverConfig = serverConfigRepo.findServerConfigById(1);

        // Überprüfen, ob der Server gerade beschäftigt ist
        if (serverConfig.isEmpty()) {
            log.error("Cant start server {}, cant find server config", server.getId());
            return false;
        }
        if (server.getServerStatusType().equals(ServerStatusType.ONLINE)
                || server.getServerStatusType().equals(ServerStatusType.STARTING)
                || server.getServerStatusType().equals(ServerStatusType.STOPPING)) {
            log.error("Cant start stop server {}, server is online or busy", server.getId());
            return false;
        }

        // Aussuchen eines zufälligen Ports
        int port = getRandomNumberUsingNextInt(serverConfig.get().getMinContainerPort(), serverConfig.get().getMaxContainerPort());
        String containerName = server.getName() + "-" + server.getId() + "-" + port;

        // Starten des Gameservers
        server.setServerStatusType(ServerStatusType.STARTING);
        try {
            log.info("Starting server {}", server.getId());
            // Befehl zum Starten eines Docker-Containers anpassen
            String dockerCommand = "docker run -d --name " + containerName + " -p " + port + ":" + port + "/udp -e PASSWORD=\"" + server.getServerPassword() + "\" -e PORT=" + port + " -e SERVERID=" + server.getId() + " -e BACKENDDOMAIN=" + serverConfig.get().getDomain() + " see-gameserver:latest";

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

                String command = "docker cp " + "\"" + file.getAbsolutePath() + "\"" + " " + containerName + ":/app/gameserver_Data/StreamingAssets/Multiplayer/";
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
            server.setContainerAddress(serverConfig.get().getDomain());
            server.setServerStatusType(ServerStatusType.ONLINE);

            return true;

        } catch (IOException | InterruptedException e) {
            server.setServerStatusType(ServerStatusType.ERROR);
            log.error("Cant start server {}", server.getId());
        }
        return false;
    }

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

    public int getRandomNumberUsingNextInt(int min, int max) {
        Random random = new Random();
        return random.nextInt(max - min) + min;
    }
}
