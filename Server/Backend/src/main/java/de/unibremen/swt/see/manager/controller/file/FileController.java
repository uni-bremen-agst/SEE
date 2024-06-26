package de.unibremen.swt.see.manager.controller.file;

import de.unibremen.swt.see.manager.model.File;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;
import de.unibremen.swt.see.manager.util.FileType;
import de.unibremen.swt.see.manager.model.Server;
import de.unibremen.swt.see.manager.service.FileService;
import de.unibremen.swt.see.manager.service.ServerService;
import java.io.IOException;
import java.io.InputStream;
import java.nio.file.Files;
import java.nio.file.Path;

import java.util.UUID;
import org.springframework.web.servlet.mvc.method.annotation.StreamingResponseBody;

/**
 * Handles HTTP requests for the /file endpoint.
 * <p>
 * This REST controller exposes various methods to perform CRUD operations on
 * file resources, specifically SEE Code City data.
 */
@RestController
@RequestMapping("/file")
@RequiredArgsConstructor
@Slf4j
public class FileController {

    private final FileService fileService;
    private final ServerService serverService;

    /**
     * Deletes the file with the specified ID.
     * 
     * @param id the ID of the file to delete
     * @return {@code 200 OK} if the file is successfully deleted,
     *         or {@code 400 Bad Request} if the file does not exist or cannot
     *         be deleted,
     *         or {@code 401 Unauthorized} if access cannot be granted.
     */
    @DeleteMapping("/delete")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> deleteFile(@RequestParam("id") UUID id) {
        try {
            return ResponseEntity.ok().body(fileService.deleteFile(id));
        } catch (IOException e) {
            return ResponseEntity.badRequest().build();
        }
    }

    /**
     * Retrieves the file with the specified ID.
     * 
     * @param id the ID of the file to retrieve
     * @return {@code 200 OK} with the file content as payload if the file
     *         exists and can be accessed,
     *         or {@code 400 Bad Request} if the file does not exist or cannot
     *         be accessed,
     *         or {@code 401 Unauthorized} if access cannot be granted.
     */
    @GetMapping("/get")
    @PreAuthorize("hasRole('USER') or hasRole('ADMIN')")
    public ResponseEntity<?> getFile(@RequestParam("id") UUID id) {
        try {
            return ResponseEntity.ok().body(fileService.getFile(id));
        } catch (IOException e) {
            return ResponseEntity.badRequest().build();
        }
    }

    /**
     * Retrieves the {@code GXL} file of the specified server.
     * 
     * @param serverID     the ID of the server
     * @param roomPassword the password to access server data
     * @return {@code 200 OK} with the file content as payload if the file
     *         exists and can be accessed,
     *         or {@code 400 Bad Request} if the file does not exist or cannot
     *         be accessed.
     */
    @GetMapping("/client/gxl")
    public ResponseEntity<?> getGxl(@RequestParam("serverId") UUID serverID, @RequestParam("roomPassword") String roomPassword) {
        Server server = serverService.getServerByID(serverID);
        if (!hasRoomAccess(server, roomPassword)) return ResponseEntity.badRequest().build();
        try {
            File file = fileService.getFileByServerAndFileType(server, FileType.GXL);
            return buildResponseEntity(file);
        } catch (IOException e) {
            return ResponseEntity.badRequest().build();
        }
    }

    /**
     * Retrieves the {@code Solution} file of the specified server.
     * 
     * @param serverID     the ID of the server
     * @param roomPassword the password to access server data
     * @return {@code 200 OK} with the file content as payload if the file
     *         exists and can be accessed,
     *         or {@code 400 Bad Request} if the file does not exist or cannot
     *         be accessed.
     */
    @GetMapping("/client/solution")
    public ResponseEntity<?> getSolution(@RequestParam("serverId") UUID serverID, @RequestParam("roomPassword") String roomPassword) {
        Server server = serverService.getServerByID(serverID);
        if (!hasRoomAccess(server, roomPassword)) return ResponseEntity.badRequest().build();
        try {
            File file = fileService.getFileByServerAndFileType(server, FileType.SOLUTION);
            return buildResponseEntity(file);
        } catch (IOException e) {
            return ResponseEntity.badRequest().build();
        }
    }

    /**
     * Retrieves the source code file of the specified server.
     * <p>
     * The source code of a Code City is usually stored in a {@code ZIP} file
     * and will be extracted on the client machine.
     * 
     * @param serverID     the ID of the server
     * @param roomPassword the password to access server data
     * @return {@code 200 OK} with the file content as payload if the file
     *         exists and can be accessed,
     *         or {@code 400 Bad Request} if the file does not exist or cannot
     *         be accessed.
     */
    @GetMapping("/client/source")
    public ResponseEntity<?> getSource(@RequestParam("serverId") UUID serverID, @RequestParam("roomPassword") String roomPassword) {
        Server server = serverService.getServerByID(serverID);
        if (!hasRoomAccess(server, roomPassword)) return ResponseEntity.badRequest().build();
        try {
            File file = fileService.getFileByServerAndFileType(server, FileType.SOURCE);
            return buildResponseEntity(file);
        } catch (IOException e) {
            return ResponseEntity.badRequest().build();
        }
    }


    /**
     * Retrieves the multiplayer configuration file of the specified server.
     * 
     * @param serverID     the ID of the server
     * @param roomPassword the password to access server data
     * @return {@code 200 OK} with the file content as payload if the file
     *         exists and can be accessed,
     *         or {@code 400 Bad Request} if the file does not exist or cannot
     *         be accessed.
     */
    @GetMapping("/client/config")
    public ResponseEntity<?> getConfig(@RequestParam("serverId") UUID serverID, @RequestParam("roomPassword") String roomPassword) {
        //Laden des Servers aus der Datenbank
        Server server = serverService.getServerByID(serverID);

        // Überprüfen, ob der Request mit dem richtigen Passwort gesendet wurde, und ob der server existiert
        if (!hasRoomAccess(server, roomPassword)) return ResponseEntity.badRequest().build();
        try {
            File file = fileService.getFileByServerAndFileType(server, FileType.CFG);
            return buildResponseEntity(file);
        } catch (IOException e) {
            return ResponseEntity.badRequest().build();
        }
    }

    /**
     * Retrieves the {@code CSV} file of the specified server.
     * 
     * @param serverID     the ID of the server
     * @param roomPassword the password to access server data
     * @return {@code 200 OK} with the file content as payload if the file
     *         exists and can be accessed,
     *         or {@code 400 Bad Request} if the file does not exist or cannot
     *         be accessed.
     */
    @GetMapping("/client/csv")
    public ResponseEntity<?> getCsv(@RequestParam("serverId") UUID serverID, @RequestParam("roomPassword") String roomPassword) {
        Server server = serverService.getServerByID(serverID);
        if (!hasRoomAccess(server, roomPassword)) return ResponseEntity.badRequest().build();
        try {
            File file = fileService.getFileByServerAndFileType(server, FileType.CSV);
            return buildResponseEntity(file);
        } catch (IOException e) {
            return ResponseEntity.badRequest().build();
        }
    }


    /**
     * Evaluates if given server can be accessed with given room password.
     * 
     * @param server       the server to be accessed
     * @param roomPassword the password to access the server
     * @return {@code true}, if the password is correct or no password is set
     *         for given server, else {@code false}.
     */
    private boolean hasRoomAccess(Server server, String roomPassword) {
        if (server == null) return false;

        return (server.getServerPassword() == null || server.getServerPassword().isEmpty() || server.getServerPassword().equals(roomPassword));
    }
    
    /**
     * Builds the {@code ResponseEntity} that can be used to stream given file.
     * 
     * @param file the file to be used in the response
     * @return the {@code ResponseEntity} with the given file
     * @throws IOException if the file is missing or cannot be accessed
     */
    private ResponseEntity<StreamingResponseBody> buildResponseEntity(File file) throws IOException {
        Path path = fileService.getFilePath(file);
        long fileSize = Files.size(path);
        StreamingResponseBody responseBody = outputStream -> {
            try (InputStream inputStream = Files.newInputStream(path)) {
                inputStream.transferTo(outputStream);
            }
        };
        
        return ResponseEntity
                    .ok()
                    .contentLength(fileSize)
                    .header("Content-type", file.getContentType())
                    .header("Content-disposition", "attachment; filename=\"" + file.getOriginalFileName() + "\"")
                    .body(responseBody);
    }

}
