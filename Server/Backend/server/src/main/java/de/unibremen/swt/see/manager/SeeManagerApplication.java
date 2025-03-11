package de.unibremen.swt.see.manager;

import de.unibremen.swt.see.manager.model.Config;
import de.unibremen.swt.see.manager.model.Role;
import de.unibremen.swt.see.manager.model.RoleType;
import de.unibremen.swt.see.manager.repository.ConfigRepository;
import de.unibremen.swt.see.manager.repository.RoleRepository;
import de.unibremen.swt.see.manager.repository.ServerRepository;
import de.unibremen.swt.see.manager.repository.UserRepository;
import de.unibremen.swt.see.manager.service.FileService;
import de.unibremen.swt.see.manager.service.ServerService;
import de.unibremen.swt.see.manager.service.UserService;
import java.util.Optional;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.CommandLineRunner;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import org.springframework.scheduling.annotation.EnableScheduling;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.session.data.redis.config.annotation.web.http.EnableRedisHttpSession;

/**
 * SEE Manager is part of the Software Engineering Experience (SEE).
 * <p>
 * This is the back-end application that exposes a REST API for creating and
 * managing SEE game server instances.
 * It also exposes an API to store and retrieve multiplayer configurations
 * and Code City data to be used by multiple SEE clients.
 * <p>
 * See also:<br>
 * <a href="https://see.uni-bremen.de/">SEE website</a>
 */
@SpringBootApplication
@RequiredArgsConstructor
@EnableRedisHttpSession
@EnableScheduling
@Slf4j
public class SeeManagerApplication {

    /**
     * Contains the domain name, or IP address, and port of this back-end
     * application server.
     * <p>
     * The value is configured in the application properties and gets injected
     * during class initialization.
     */
    @Value("${see.app.backend.domain}")
    private String backendDomain;

    /**
     * Handle server-related operations and business logic.
     */
    private final ServerService serverService;

    /**
     * The main method to run the Spring Boot application.
     * @param args command-line arguments
     */
    public static void main(String[] args) {
        SpringApplication.run(SeeManagerApplication.class, args);
    }

    /**
     * Configures and returns a CommandLineRunner bean.
     * <p>
     * This runner performs necessary initialization tasks or setup operations
     * at application startup.
     * <p>
     * Required dependencies are listed in the parameters.
     *
     * @param newAdminName username of an admin user that should be created
     * during startup
     * @param newAdminPassword plain-text password of an admin user that should
     * @param serverRepo server repository dependency
     * @param userRepo user repository dependency
     * @param userService user service dependency
     * @param fileService file service dependency
     * @param roleRepo role repository dependency
     * @param configRepo configuration repository dependency
     * @return the configured {@link CommandLineRunner}
     */
    @Bean
    CommandLineRunner cliRunner(
            @Value("${see.app.admin.add.name}") String newAdminName,
            @Value("${see.app.admin.add.password}") String newAdminPassword,
            ServerRepository serverRepo,
            UserRepository userRepo, 
            UserService userService, 
            FileService fileService, 
            RoleRepository roleRepo, 
            ConfigRepository configRepo) {
        return args -> {
            Config config = new Config();
            config.setDomain(backendDomain);
            config.setMinContainerPort(9100);
            config.setMaxContainerPort(9300);
            configRepo.save(config);

            Optional<Role> optAdminRole = roleRepo.findByName(RoleType.ROLE_ADMIN);
            if (optAdminRole.isEmpty())
                roleRepo.save(new Role(RoleType.ROLE_ADMIN));
            Optional<Role> optUserRole = roleRepo.findByName(RoleType.ROLE_USER);
            if (optUserRole.isEmpty())
                roleRepo.save(new Role(RoleType.ROLE_USER));

            if (newAdminName != null && !newAdminName.isBlank() && newAdminPassword != null && !newAdminPassword.isBlank()) {
                log.warn("ADDING ADMIN USER PASSED VIA ENVIRONMENT: {}", newAdminName);
                userService.create(newAdminName, newAdminPassword, RoleType.ROLE_ADMIN);
            }
        };
    }

    /**
     * Updates all server status on a fixed interval.
     */
    @Scheduled(fixedRate = 60000)
    public void scheduledServerStatusUpdate() {
        if (serverService == null) {
            return;
        }
        log.info("Updating server status...");
        serverService.updateStatus();
    }
}