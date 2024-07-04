package de.unibremen.swt.see.manager.controller;

import de.unibremen.swt.see.manager.model.File;
import de.unibremen.swt.see.manager.model.FileType;
import de.unibremen.swt.see.manager.service.FileService;
import de.unibremen.swt.see.manager.service.ServerService;
import jakarta.persistence.EntityNotFoundException;
import java.io.IOException;
import java.io.InputStream;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;
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

    /**
     * Handle file-related operations and business logic.
     */
    private final FileService fileService;

    /**
     * Handle server-related operations and business logic.
     */
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
            fileService.delete(id);
            return ResponseEntity.ok().build();
        } catch (EntityNotFoundException e) {
            return ResponseEntity.badRequest().body("File not found.");
        } catch (IOException e) {
            return ResponseEntity.badRequest().body("File could not be deleted.");
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
        // FIXME Any user can access any files.
        //       While this does not contain the actual file, metadata can be leaked.
        return ResponseEntity.ok().body(fileService.get(id));
    }

    /**
     * Retrieves the {@code GXL} file of the specified server.
     * 
     * @param serverId the ID of the server
     * @param roomPassword the password to access server data
     * @return {@code 200 OK} with the file content as payload if the file
     * exists and can be accessed, or {@code 400 Bad Request} if server or file
     * do not exist or file cannot be accessed.
     */
    @GetMapping("/client/gxl")
    public ResponseEntity<?> getGxl(@RequestParam("serverId") UUID serverId, @RequestParam("roomPassword") String roomPassword) {
        if (!serverService.validateAccess(serverId, roomPassword)) {
            return ResponseEntity.badRequest().body("Authorization failed.");
        }
        try {
            File file = fileService.getByServerAndFileType(serverId, FileType.GXL);
            return buildResponseEntity(file);
        } catch (EntityNotFoundException e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        } catch (IOException e) {
            return ResponseEntity.badRequest().body("Error reading file.");
        }
    }

    /**
     * Retrieves the {@code Solution} file of the specified server.
     * 
     * @param serverId the ID of the server
     * @param roomPassword the password to access server data
     * @return {@code 200 OK} with the file content as payload if the file
     * exists and can be accessed, or {@code 400 Bad Request} if server or file
     * do not exist or file cannot be accessed.
     */
    @GetMapping("/client/solution")
    public ResponseEntity<?> getSolution(@RequestParam("serverId") UUID serverId, @RequestParam("roomPassword") String roomPassword) {
        if (!serverService.validateAccess(serverId, roomPassword)) {
            return ResponseEntity.badRequest().body("Authorization failed.");
        }
        try {
            File file = fileService.getByServerAndFileType(serverId, FileType.SOLUTION);
            return buildResponseEntity(file);
        } catch (EntityNotFoundException e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        } catch (IOException e) {
            return ResponseEntity.badRequest().body("Error reading file.");
        }
    }

    /**
     * Retrieves the source code file of the specified server.
     * <p>
     * The source code of a Code City is usually stored in a {@code ZIP} file
     * and will be extracted on the client machine.
     * 
     * @param serverId the ID of the server
     * @param roomPassword the password to access server data
     * @return {@code 200 OK} with the file content as payload if the file
     * exists and can be accessed, or {@code 400 Bad Request} if server or file
     * do not exist or file cannot be accessed.
     */
    @GetMapping("/client/source")
    public ResponseEntity<?> getSource(@RequestParam("serverId") UUID serverId, @RequestParam("roomPassword") String roomPassword) {
        if (!serverService.validateAccess(serverId, roomPassword)) {
            return ResponseEntity.badRequest().body("Authorization failed.");
        }
        try {
            File file = fileService.getByServerAndFileType(serverId, FileType.SOURCE);
            return buildResponseEntity(file);
        } catch (EntityNotFoundException e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        } catch (IOException e) {
            return ResponseEntity.badRequest().body("Error reading file.");
        }
    }

    /**
     * Retrieves the multiplayer configuration file of the specified server.
     * 
     * @param serverId the ID of the server
     * @param roomPassword the password to access server data
     * @return {@code 200 OK} with the file content as payload if the file
     * exists and can be accessed, or {@code 400 Bad Request} if server or file
     * do not exist or file cannot be accessed.
     */
    @GetMapping("/client/config")
    public ResponseEntity<?> getConfig(@RequestParam("serverId") UUID serverId, @RequestParam("roomPassword") String roomPassword) {
        if (!serverService.validateAccess(serverId, roomPassword)) {
            return ResponseEntity.badRequest().body("Authorization failed.");
        }
        try {
            File file = fileService.getByServerAndFileType(serverId, FileType.CFG);
            return buildResponseEntity(file);
        } catch (EntityNotFoundException e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        } catch (IOException e) {
            return ResponseEntity.badRequest().body("Error reading file.");
        }
    }

    /**
     * Retrieves the {@code CSV} file of the specified server.
     * 
     * @param serverId the ID of the server
     * @param roomPassword the password to access server data
     * @return {@code 200 OK} with the file content as payload if the file
     * exists and can be accessed, or {@code 400 Bad Request} if server or file
     * do not exist or file cannot be accessed.
     */
    @GetMapping("/client/csv")
    public ResponseEntity<?> getCsv(@RequestParam("serverId") UUID serverId, @RequestParam("roomPassword") String roomPassword) {
        if (!serverService.validateAccess(serverId, roomPassword)) {
            return ResponseEntity.badRequest().body("Authorization failed.");
        }
        try {
            File file = fileService.getByServerAndFileType(serverId, FileType.CSV);
            return buildResponseEntity(file);
        } catch (EntityNotFoundException e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        } catch (IOException e) {
            return ResponseEntity.badRequest().body("Error reading file.");
        }
    }

    /**
     * Builds the {@code ResponseEntity} that can be used to stream given file.
     * 
     * @param file the file to be used in the response
     * @return the {@code ResponseEntity} with the given file
     * @throws IOException if the file is missing or cannot be accessed
     */
    private ResponseEntity<StreamingResponseBody> buildResponseEntity(File file) throws IOException {
        InputStream fileInputStream = fileService.getInputStream(file);
        StreamingResponseBody responseBody = outputStream -> {
            try (InputStream inputStream = fileInputStream) {
                inputStream.transferTo(outputStream);
            }
        };
        
        return ResponseEntity
                .ok()
                .contentLength(file.getSize())
                .header("Content-type", file.getContentType())
                .header("Content-disposition", "attachment; filename=\"" + file.getName() + "\"")
                .body(responseBody);
    }

}
