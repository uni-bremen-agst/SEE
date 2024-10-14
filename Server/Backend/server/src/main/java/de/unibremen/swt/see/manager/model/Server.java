package de.unibremen.swt.see.manager.model;


import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.persistence.*;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.Set;
import java.util.UUID;
import lombok.Getter;
import lombok.RequiredArgsConstructor;
import lombok.Setter;

/**
 * Represents the data model of a SEE game server configuration.
 */
@Getter
@Entity
@Table(name = "servers",
        uniqueConstraints = {
            @UniqueConstraint(columnNames = {"container_port"})
        })
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
     * The Docker container ID of the game server.
     */
    @Setter
    @JsonIgnore
    @Column(name = "container_id")
    private String containerId;

    /**
     * The external address of the container host to access the game server.
     */
    @Setter
    @Column(name = "container_address")
    private String containerAddress;

    /**
     * The port on the container host that should be bound to the container's.
     */
    @Setter
    @Column(name = "container_port", unique = true)
    private Integer containerPort;

    /**
     * The momentary status of the game server container.
     */
    @Setter
    @Column(name = "status")
    @Enumerated(EnumType.STRING)
    private ServerStatusType status = ServerStatusType.OFFLINE;

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
