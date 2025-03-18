package de.unibremen.swt.see.manager.controller;

import de.unibremen.swt.see.manager.model.File;
import de.unibremen.swt.see.manager.service.FileService;
import de.unibremen.swt.see.manager.service.ServerService;
import jakarta.persistence.EntityNotFoundException;
import java.io.IOException;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.core.io.FileSystemResource;
import org.springframework.core.io.Resource;
import org.springframework.http.ContentDisposition;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

/**
 * Handles HTTP requests for the /file endpoint.
 * <p>
 * This REST controller exposes various methods to perform CRUD operations on
 * file resources, specifically SEE Code City data.
 */
@RestController
@RequestMapping("/api/v1/file")
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
            return ResponseEntity.badRequest().body(ControllerUtils.wrapMessage("File not found."));
        } catch (IOException e) {
            return ResponseEntity.badRequest().body(ControllerUtils.wrapMessage("File could not be deleted."));
        }
    }

    /**
     * Retrieves the file with the specified ID.
     * 
     * @param id the ID of the file to retrieve
     * @return {@code 200 OK} with the file metadata as payload if the file
     * exists, or {@code 400 Bad Request} if the file does not exist, or
     * {@code 401 Unauthorized} if access cannot be granted.
     */
    @GetMapping("/get")
    @PreAuthorize("hasRole('ADMIN') or hasRole('USER') and @accessControlService.canAccessFile(principal.id, #id)")
    public ResponseEntity<?> getFile(@RequestParam("id") UUID id) {
        File file = fileService.get(id);
        if (file == null) {
            return ResponseEntity.badRequest().body(ControllerUtils.wrapMessage("File with specified ID does not exist!"));
        }
        return ResponseEntity.ok().body(file);
    }

    /**
     * Downloads the actual file content by the specified file ID.
     *
     * @param id the ID of the file to retrieve
     * @return {@code 200 OK} with the file content as payload if the file
     * exists and can be accessed, or {@code 400 Bad Request} if the file does
     * not exist, or {@code 500 Internal Server Error} if there is an I/O error
     * while accessing the file, or {@code 401 Unauthorized} if access cannot be
     * granted.
     */
    @GetMapping("/download")
    @PreAuthorize("hasRole('ADMIN') or hasRole('USER') and @accessControlService.canAccessFile(principal.id, #id)")
    public ResponseEntity<?> downloadFile(@RequestParam("id") UUID id) {
        File file = fileService.get(id);
        if (file == null) {
            return ResponseEntity.badRequest().body(ControllerUtils.wrapMessage("File with specified ID does not exist!"));
        }

        try {
            return buildResponseEntity(file, true);
        } catch (IOException e) {
            return ResponseEntity.internalServerError().body(ControllerUtils.wrapMessage("Error reading file."));
        }
    }

    /**
     * Builds the {@code ResponseEntity} that can be used to stream given file.
     * <p>
     * If {@code attachment} is {@code true}, the {@code content-disposition}
     * HTTP header field is set to {@code attachment}, so that the client will
     * usually display a "save as…" dialog.
     *
     * @param file the file to be used in the response
     * @param attachment if a "save as…" dialog should be triggered
     * @return the {@code ResponseEntity} with the given file
     * @throws IOException if the file is missing or cannot be accessed
     */
    private ResponseEntity<Resource> buildResponseEntity(File file, boolean attachment) throws IOException {
        Resource resource = new FileSystemResource(fileService.getPath(file));

        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_OCTET_STREAM);
        headers.setContentLength(file.getSize());
        if (attachment) {
            headers.setContentDisposition(ContentDisposition.attachment().filename(file.getName()).build());
        }

        return ResponseEntity.ok().headers(headers).<Resource>body(resource);
    }

}
