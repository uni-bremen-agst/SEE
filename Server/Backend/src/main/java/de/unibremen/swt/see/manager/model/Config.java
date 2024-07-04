package de.unibremen.swt.see.manager.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.PrePersist;
import jakarta.persistence.Table;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import lombok.Getter;
import lombok.RequiredArgsConstructor;
import lombok.Setter;

/**
 * Represents the configuration of a management backend instance.
 */
@Getter
@Entity
@Table(name = "configs")
@RequiredArgsConstructor
public class Config {

    /**
     * ID of the configuration.
     */
    @Id
    private Integer id = 1;

    /**
     * The lower bound of the port range associated with SEE game server
     * instances.
     */
    @Setter
    @Column(name = "min_container_port")
    private Integer minContainerPort;

    /**
     * The upper bound of the port range associated with SEE game server
     * instances.
     */
    @Setter
    @Column(name = "max_container_port")
    private Integer maxContainerPort;

    /**
     * The IP address or domain and port assigned to the management server
     * backend instance.
     */
    @Setter
    @Column(name = "domain")
    private String domain;

    /**
     * The point in time this configuration was persisted.
     */
    @Column(name = "creation_time", updatable = false)
    private ZonedDateTime creationTime;

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
