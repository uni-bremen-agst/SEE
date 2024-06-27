package de.unibremen.swt.see.manager.controller.server;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.multipart.MultipartFile;
import de.unibremen.swt.see.manager.model.File;
import de.unibremen.swt.see.manager.model.Server;
import de.unibremen.swt.see.manager.service.ServerService;

import java.util.UUID;

/**
 * Handles HTTP requests for the /server endpoint.
 * <p>
 * This REST controller exposes various methods to control SEE game server
 * instances.
 */
@RestController
@RequestMapping("/server")
@RequiredArgsConstructor
@Slf4j
public class ServerController {

    private final ServerService serverService;

    /**
     * Retrieves metadata of the server identified by the specified ID.
     *
     * @param id the ID of the server to retrieve
     * @return {@code 200 OK} with the server metadata as payload,
     *         or {@code 401 Unauthorized} if access cannot be granted.
     */
    @GetMapping("/")
    @PreAuthorize("hasRole('USER') or hasRole('ADMIN')")
    public ResponseEntity<?> getServer(@RequestParam("id") UUID id) {
        return ResponseEntity.ok().body(serverService.getServerByID(id));
    }

    /**
     * Retrieves the metadata of all available server resources.
     *
     * @return {@code 200 OK} with the server metadata as payload,
     *         or {@code 401 Unauthorized} if access cannot be granted.
     */
    @GetMapping("/all")
    @PreAuthorize("hasRole('USER') or hasRole('ADMIN')")
    public ResponseEntity<?> getServers() {
        return ResponseEntity.ok().body(serverService.getAllServer());
    }

    /**
     * Creates a new server.
     *
     * @param server metadata object to create new server instance
     * @return {@code 200 OK} with the server metadata object as payload,
     *         or {@code 401 Unauthorized} if access cannot be granted.
     */
    @PostMapping("/create")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> createServers(@RequestBody Server server) {
        return ResponseEntity.ok().body(serverService.saveServer(server));
    }

    /**
     * Adds a file to an existing server.
     *
     * @param serverId the ID of the server
     * @param fileType {@code String} representation of a {@code FileType} value
     * @param file     file content
     * @return {@code 200 OK} with the file metadata object as payload,
     *         or {@code 500 Internal Server Error} if the file could not be
     *         persisted,
     *         or {@code 401 Unauthorized} if access cannot be granted.
     * @see de.unibremen.swt.see.manager.model.FileType
     */
    @PostMapping("/addFile")
    @PreAuthorize("hasRole('USER') or hasRole('ADMIN')")
    public ResponseEntity<?> addFileToServer(@RequestParam("id") UUID serverId,
                                             @RequestParam("fileType") String fileType,
                                             @RequestParam("file") MultipartFile file) {
        File responseFile = serverService.addFileToServer(serverId, fileType, file);
        if (responseFile == null)
            return ResponseEntity.internalServerError().build();
        return ResponseEntity.ok().body(responseFile);
    }

    /**
     * Deletes the server with the specified ID.
     * <p>
     * Deletes the server along with its files.
     *
     * @param id the ID of the server to delete
     * @return {@code 200 OK},
     *         or {@code 401 Unauthorized} if access cannot be granted.
     */
    @DeleteMapping("/delete")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> deleteServer(@RequestParam("id") UUID id) {
        return ResponseEntity.ok().body(serverService.deleteServer(id));
    }

    /**
     * Start the server with the specified ID.
     * 
     * @param id the ID of the server to start
     * @return {@code 200 OK},
     *         or {@code 401 Unauthorized} if access cannot be granted.
     */
    @PutMapping("/startServer")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> startGameServer(@RequestParam("id") UUID id) {
        return ResponseEntity.ok().body(serverService.startServer(id));
    }

    /**
     * Stop the server with the specified ID.
     * 
     * @param id the ID of the server to stop
     * @return {@code 200 OK},
     *         or {@code 401 Unauthorized} if access cannot be granted.
     */
    @PutMapping("/stopServer")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> stopGameServer(@RequestParam("id") UUID id) {
        return ResponseEntity.ok().body(serverService.stopServer(id));
    }

    /**
     * Retrieves the file list of the server with the specified ID.
     *
     * @param id the ID of the server
     * @return {@code 200 OK} with the file metadata as payload,
     *         or {@code 401 Unauthorized} if access cannot be granted.
     */
    @GetMapping("/files")
    @PreAuthorize("hasRole('USER') or hasRole('ADMIN')")
    public ResponseEntity<?> getFiles(@RequestParam("id") UUID id) {
        return ResponseEntity.ok().body(serverService.getFilesForServer(id));
    }

    /**
     * Retrieves the file list of the server with the specified ID.
     * <p>
     * This endpoint uses a simple authentication and is intended to be used by
     * SEE clients.
     *
     * @param id       the ID of the server
     * @param password the password to access server data
     * @return {@code 200 OK} with the file metadata as payload,
     *         or {@code 400 Bad Request} if the server does not exist,
     *         or if the password is not correct.
     */
    @GetMapping("/getFilesForClient")
    public ResponseEntity<?> getFiles(@RequestParam("id") UUID id, @RequestParam("roomPassword") String password) {
        Server server = serverService.getServerByID(id);
        if (server == null){
            return ResponseEntity.badRequest().build();
        }
        if (server.getServerPassword() == null  || server.getServerPassword().isEmpty() || server.getServerPassword().equals(password)) {
            return ResponseEntity.ok().body(serverService.getFilesForServer(id));
        }
        return ResponseEntity.badRequest().build();
    }

}
