package de.unibremen.swt.see.manager.model;

import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.RequiredArgsConstructor;
import lombok.Setter;
import de.unibremen.swt.see.manager.util.FileType;

import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.UUID;

@Getter
@Entity(name = "file")
@RequiredArgsConstructor
public class File {

    @Id
    @GeneratedValue(strategy = GenerationType.UUID)
    @Column(name = "id", updatable = false)
    private UUID id;

    @Setter
    private String originalFileName;

    @Setter
    private String name;

    @Setter
    private String contentType;

    @Setter
    @Enumerated(EnumType.STRING)
    private FileType fileType;

    @JsonIgnore
    @ManyToOne
    @Setter
    @JoinColumn(name = "server_id")
    private Server server;


    @Column(name = "creationTime", updatable = false)
    private ZonedDateTime creationTime;

    @PrePersist
    void generateTimeStamp() {
        creationTime = ZonedDateTime.now(ZoneId.of("UTC"));
    }

}
