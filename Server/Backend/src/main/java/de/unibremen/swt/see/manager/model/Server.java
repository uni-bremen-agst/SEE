package de.unibremen.swt.see.manager.model;


import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.RequiredArgsConstructor;
import lombok.Setter;
import de.unibremen.swt.see.manager.util.ServerStatusType;

import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.Set;
import java.util.UUID;

@Getter
@Entity(name = "server")
@RequiredArgsConstructor
public class Server {
    @Id
    @GeneratedValue(strategy = GenerationType.UUID)
    @Column(name = "id", updatable = false)
    private UUID id;

    @Setter
    @Getter
    @Column(name = "name")
    private String name;

    @Setter
    @Column(name = "serverPassword")
    private String serverPassword;

    @Setter
    @Column(name = "avatarSeed")
    private String avatarSeed;

    @Setter
    @Column(name = "avatarColor")
    private String avatarColor;

    @Setter
    @Column(name = "gameServerAddress")
    private String containerAddress;

    @Setter
    @JsonIgnore
    @Column(name = "containerName")
    private String containerName;

    @Setter
    @Column(name = "containerPort")
    private Integer containerPort;

    @Setter
    @Column(name = "serverStatus")
    @Enumerated(EnumType.STRING)
    private ServerStatusType serverStatusType = ServerStatusType.OFFLINE;

    @JsonIgnore
    @OneToMany(mappedBy = "server")
    private Set<File> serverFiles;

    @Column(name = "creationTime", updatable = false)
    private ZonedDateTime creationTime;

    @Column(name = "startTime")
    @Setter
    private ZonedDateTime startTime;

    @Column(name = "stopTime")
    @Setter
    private ZonedDateTime stopTime;


    @PrePersist
    void generateTimeStamp() {
        creationTime = ZonedDateTime.now(ZoneId.of("UTC"));
    }
}
