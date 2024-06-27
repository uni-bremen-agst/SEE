package de.unibremen.swt.see.manager.model;


import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.RequiredArgsConstructor;
import lombok.Setter;

import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.Set;
import java.util.UUID;

/**
 * Represents the data model of a SEE game server configuration.
 */
@Getter
@Entity
@Table(name = "servers")
@RequiredArgsConstructor
public class Server {
    
    /**
     * ID of the server configuration.
     */
    @Id
    @GeneratedValue(strategy = GenerationType.UUID)
    @Column(name = "id", updatable = false)
    private UUID id;

    /**
     * The name of the game server instance.
     */
    @Setter
    @Getter
    @Column(name = "name")
    private String name;

    /**
     * The password of the game server instance.
     * <p>
     * <b>Beware: This is a plaintext password!</b>
     */
    @Setter
    @Column(name = "server_password")
    private String serverPassword;

    /**
     * Seed for the avatar generator.
     * <p>
     * This seed is used to generate the same avatar every time.
     */
    @Setter
    @Column(name = "avatar_seed")
    private String avatarSeed;

    /**
     * The color of the avatar.
     */
    @Setter
    @Column(name = "avatar_color")
    private String avatarColor;

    /**
     * The address or domain of the game server instance.
     * <p>
     * Might be {@code null} if the server is stopped.
     */
    @Setter
    @Column(name = "game_server_address")
    private String containerAddress;

    /**
     * The container name under which the game server is running
     * or should be started.
     */
    @Setter
    @JsonIgnore
    @Column(name = "container_name")
    private String containerName;

    /**
     * The port that should be exposed by the game server container.
     */
    @Setter
    @Column(name = "container_port")
    private Integer containerPort;

    /**
     * The momentary status of the game server container.
     */
    @Setter
    @Column(name = "server_status")
    @Enumerated(EnumType.STRING)
    private ServerStatusType serverStatusType = ServerStatusType.OFFLINE;

    /**
     * The files associated with the game server.
     * <p>
     * These are usually files related to a Code City displayed in SEE.
     */
    @JsonIgnore
    @OneToMany(mappedBy = "server")
    private Set<File> serverFiles;

    /**
     * The point in time this configuration was persisted.
     */
    @Column(name = "creation_time", updatable = false)
    private ZonedDateTime creationTime;

    /**
     * The point in time the game server instance was started.
     * <p>
     * Might be {@code null} if the server is stopped.
     */
    @Column(name = "start_time")
    @Setter
    private ZonedDateTime startTime;

    /**
     * The point in time the game server instance was stopped.
     * <p>
     * Might be {@code null} if the server is running.
     */
    @Column(name = "stop_time")
    @Setter
    private ZonedDateTime stopTime;


    /**
     * Generates the timestamp for the {@link creationTime}.
     * <p>
     * The timestamp is generated right before this metadata object is
     * persisted in the database.
     * It is generated using UTC timezone.
     */
    @PrePersist
    void generateTimeStamp() {
        creationTime = ZonedDateTime.now(ZoneId.of("UTC"));
    }
}
