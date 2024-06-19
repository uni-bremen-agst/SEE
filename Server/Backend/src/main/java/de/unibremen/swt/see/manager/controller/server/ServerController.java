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

@RestController
@RequestMapping("/server")
@RequiredArgsConstructor
@Slf4j
public class ServerController {

    private final ServerService serverService;

    @GetMapping("/")
    @PreAuthorize("hasRole('USER') or hasRole('ADMIN')")
    public ResponseEntity<?> getServer(@RequestParam("id") UUID id) {
        return ResponseEntity.ok().body(serverService.getServerByID(id));
    }

    @GetMapping("/all")
    @PreAuthorize("hasRole('USER') or hasRole('ADMIN')")
    public ResponseEntity<?> getServers() {
        return ResponseEntity.ok().body(serverService.getAllServer());
    }

    @PostMapping("/create")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> createServers(@RequestBody Server server) {
        return ResponseEntity.ok().body(serverService.saveServer(server));
    }

    @PostMapping("/addFile")
    @PreAuthorize("hasRole('USER') or hasRole('ADMIN')")
    public ResponseEntity<?> addFileToServer(@RequestParam("id") UUID serverId,
                                             @RequestParam("fileType") String fileType,
                                             @RequestParam("file") MultipartFile file) {
        File responseFile = serverService.addFileToServer(serverId, fileType, file);
        if (file == null)
            return ResponseEntity.internalServerError().build();
        return ResponseEntity.ok().body(responseFile);
    }

    @DeleteMapping("/delete")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> deleteServer(@RequestParam("id") UUID id) {
        return ResponseEntity.ok().body(serverService.deleteServer(id));
    }

    @PutMapping("/startServer")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> startGameServer(@RequestParam("id") UUID id) {
        return ResponseEntity.ok().body(serverService.startServer(id));
    }

    @PutMapping("/stopServer")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> stopGameServer(@RequestParam("id") UUID id) {
        return ResponseEntity.ok().body(serverService.stopServer(id));
    }

    @GetMapping("/files")
    @PreAuthorize("hasRole('USER') or hasRole('ADMIN')")
    public ResponseEntity<?> getFiles(@RequestParam("id") UUID id) {
        return ResponseEntity.ok().body(serverService.getFilesForServer(id));
    }

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
