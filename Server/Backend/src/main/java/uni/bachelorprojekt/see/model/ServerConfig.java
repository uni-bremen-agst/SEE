package uni.bachelorprojekt.see.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.PrePersist;
import lombok.Getter;
import lombok.RequiredArgsConstructor;
import lombok.Setter;

import java.time.ZoneId;
import java.time.ZonedDateTime;

@Getter
@Entity(name = "serverConfig")
@RequiredArgsConstructor
public class ServerConfig {

    @Id
    private Integer id = 1;

    @Setter
    @Column(name = "minContainerPort")
    private Integer minContainerPort;

    @Setter
    @Column(name = "maxContainerPort")
    private Integer maxContainerPort;

    @Setter
    @Column(name = "domain")
    private String domain;

    @Column(name = "creationTime", updatable = false)
    private ZonedDateTime creationTime;

    @PrePersist
    void generateTimeStamp() {
        creationTime = ZonedDateTime.now(ZoneId.of("UTC"));
    }
}
