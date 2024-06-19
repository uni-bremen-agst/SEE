package de.unibremen.swt.see.manager.controller.file;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.core.io.ByteArrayResource;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;
import de.unibremen.swt.see.manager.util.FileType;
import de.unibremen.swt.see.manager.file.payload.PayloadFile;
import de.unibremen.swt.see.manager.model.Server;
import de.unibremen.swt.see.manager.service.FileService;
import de.unibremen.swt.see.manager.service.ServerService;

import java.util.UUID;

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
            PayloadFile payloadFile = fileService.getFileByServerAndFileType(server, FileType.GXL);

            byte[] data = payloadFile.getContent();
            ByteArrayResource resource = new ByteArrayResource(data);
            return ResponseEntity
                    .ok()
                    .contentLength(data.length)
                    .header("Content-type", payloadFile.getContentType())
                    .header("Content-disposition", "attachment; filename=\"" + payloadFile.getOriginalFileName() + "\"")
                    .body(resource);

        } catch (Exception e) {
            return ResponseEntity.badRequest().build();
        }
    }

    @GetMapping("/client/solution")
    public ResponseEntity<?> getSolution(@RequestParam("serverId") UUID serverID, @RequestParam("roomPassword") String roomPassword) {
        Server server = serverService.getServerByID(serverID);
        if (evalNot(server, roomPassword)) return ResponseEntity.badRequest().build();
        try {
            PayloadFile payloadFile = fileService.getFileByServerAndFileType(server, FileType.SOLUTION);

            byte[] data = payloadFile.getContent();
            ByteArrayResource resource = new ByteArrayResource(data);
            return ResponseEntity
                    .ok()
                    .contentLength(data.length)
                    .header("Content-type", payloadFile.getContentType())
                    .header("Content-disposition", "attachment; filename=\"" + payloadFile.getOriginalFileName() + "\"")
                    .body(resource);

        } catch (Exception e) {
            return ResponseEntity.badRequest().build();
        }
    }

    @GetMapping("/client/source")
    public ResponseEntity<?> getSource(@RequestParam("serverId") UUID serverID, @RequestParam("roomPassword") String roomPassword) {
        Server server = serverService.getServerByID(serverID);
        if (evalNot(server, roomPassword)) return ResponseEntity.badRequest().build();
        try {
            PayloadFile payloadFile = fileService.getFileByServerAndFileType(server, FileType.SOURCE);

            byte[] data = payloadFile.getContent();
            ByteArrayResource resource = new ByteArrayResource(data);
            return ResponseEntity
                    .ok()
                    .contentLength(data.length)
                    .header("Content-type", payloadFile.getContentType())
                    .header("Content-disposition", "attachment; filename=\"" + payloadFile.getOriginalFileName() + "\"")
                    .body(resource);

        } catch (Exception e) {
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

            //Laden der Datei aus der Datenbank und Dateispeicher
            PayloadFile payloadFile = fileService.getFileByServerAndFileType(server, FileType.CONFIG);

            // Sendern der Datei an den Client
            byte[] data = payloadFile.getContent();
            ByteArrayResource resource = new ByteArrayResource(data);
            return ResponseEntity
                    .ok()
                    .contentLength(data.length)
                    .header("Content-type", payloadFile.getContentType())
                    .header("Content-disposition", "attachment; filename=\"" + payloadFile.getOriginalFileName() + "\"")
                    .body(resource);

        } catch (Exception e) {
            return ResponseEntity.badRequest().build();
        }
    }

    @GetMapping("/client/csv")
    public ResponseEntity<?> getCsv(@RequestParam("serverId") UUID serverID, @RequestParam("roomPassword") String roomPassword) {
        Server server = serverService.getServerByID(serverID);
        if (evalNot(server, roomPassword)) return ResponseEntity.badRequest().build();
        try {
            PayloadFile payloadFile = fileService.getFileByServerAndFileType(server, FileType.CSV);

            byte[] data = payloadFile.getContent();
            ByteArrayResource resource = new ByteArrayResource(data);
            return ResponseEntity
                    .ok()
                    .contentLength(data.length)
                    .header("Content-type", payloadFile.getContentType())
                    .header("Content-disposition", "attachment; filename=\"" + payloadFile.getOriginalFileName() + "\"")
                    .body(resource);

        } catch (Exception e) {
            return ResponseEntity.badRequest().build();
        }
    }


    private boolean evalNot(Server server, String roomPassword) {
        if (server == null) return true;

        return !(server.getServerPassword() == null || server.getServerPassword().isEmpty() || server.getServerPassword().equals(roomPassword));
    }

}
