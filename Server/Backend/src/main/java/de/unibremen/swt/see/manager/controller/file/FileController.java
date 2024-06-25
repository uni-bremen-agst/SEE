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

@RestController
@RequestMapping("/file")
@RequiredArgsConstructor
@Slf4j
public class FileController {

    private final FileService fileService;
    private final ServerService serverService;

    @DeleteMapping("/delete")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> deleteFile(@RequestParam("id") UUID id) {
        try {
            return ResponseEntity.ok().body(fileService.deleteFile(id));
        } catch (Exception e) {
            return ResponseEntity.badRequest().build();
        }
    }

    @GetMapping("/get")
    @PreAuthorize("hasRole('USER') or hasRole('MODERATOR') or hasRole('ADMIN')")
    public ResponseEntity<?> getFile(@RequestParam("id") UUID id) {
        try {
            return ResponseEntity.ok().body(fileService.getFile(id));
        } catch (Exception e) {
            return ResponseEntity.badRequest().build();
        }
    }

    @GetMapping("/client/gxl")
    public ResponseEntity<?> getGxl(@RequestParam("serverId") UUID serverID, @RequestParam("roomPassword") String roomPassword) {
        Server server = serverService.getServerByID(serverID);
        if (evalNot(server, roomPassword)) return ResponseEntity.badRequest().build();
        try {
            File file = fileService.getFileByServerAndFileType(server, FileType.GXL);
            return buildResponseEntity(file);
        } catch (IOException e) {
            return ResponseEntity.badRequest().build();
        }
    }

    @GetMapping("/client/solution")
    public ResponseEntity<?> getSolution(@RequestParam("serverId") UUID serverID, @RequestParam("roomPassword") String roomPassword) {
        Server server = serverService.getServerByID(serverID);
        if (evalNot(server, roomPassword)) return ResponseEntity.badRequest().build();
        try {
            File file = fileService.getFileByServerAndFileType(server, FileType.SOLUTION);
            return buildResponseEntity(file);
        } catch (IOException e) {
            return ResponseEntity.badRequest().build();
        }
    }

    @GetMapping("/client/source")
    public ResponseEntity<?> getSource(@RequestParam("serverId") UUID serverID, @RequestParam("roomPassword") String roomPassword) {
        Server server = serverService.getServerByID(serverID);
        if (evalNot(server, roomPassword)) return ResponseEntity.badRequest().build();
        try {
            File file = fileService.getFileByServerAndFileType(server, FileType.SOURCE);
            return buildResponseEntity(file);
        } catch (IOException e) {
            return ResponseEntity.badRequest().build();
        }
    }


    @GetMapping("/client/config")
    public ResponseEntity<?> getConfig(@RequestParam("serverId") UUID serverID, @RequestParam("roomPassword") String roomPassword) {
        //Laden des Servers aus der Datenbank
        Server server = serverService.getServerByID(serverID);

        // Überprüfen, ob der Request mit dem richtigen Passwort gesendet wurde, und ob der server existiert
        if (evalNot(server, roomPassword)) return ResponseEntity.badRequest().build();
        try {
            File file = fileService.getFileByServerAndFileType(server, FileType.CONFIG);
            return buildResponseEntity(file);
        } catch (IOException e) {
            return ResponseEntity.badRequest().build();
        }
    }

    @GetMapping("/client/csv")
    public ResponseEntity<?> getCsv(@RequestParam("serverId") UUID serverID, @RequestParam("roomPassword") String roomPassword) {
        Server server = serverService.getServerByID(serverID);
        if (evalNot(server, roomPassword)) return ResponseEntity.badRequest().build();
        try {
            File file = fileService.getFileByServerAndFileType(server, FileType.CSV);
            return buildResponseEntity(file);
        } catch (IOException e) {
            return ResponseEntity.badRequest().build();
        }
    }


    private boolean evalNot(Server server, String roomPassword) {
        if (server == null) return true;

        return !(server.getServerPassword() == null || server.getServerPassword().isEmpty() || server.getServerPassword().equals(roomPassword));
    }
    
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
