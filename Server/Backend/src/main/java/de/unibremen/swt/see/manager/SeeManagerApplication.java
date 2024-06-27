package de.unibremen.swt.see.manager;

import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.CommandLineRunner;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import de.unibremen.swt.see.manager.model.RoleType;
import de.unibremen.swt.see.manager.model.Role;
import de.unibremen.swt.see.manager.model.Config;
import de.unibremen.swt.see.manager.repo.ServerRepo;
import de.unibremen.swt.see.manager.repo.UserRepo;
import de.unibremen.swt.see.manager.service.FileService;
import de.unibremen.swt.see.manager.service.ServerService;
import de.unibremen.swt.see.manager.service.UserService;
import java.util.Optional;
import org.springframework.beans.factory.annotation.Value;
import de.unibremen.swt.see.manager.repo.ConfigRepo;
import de.unibremen.swt.see.manager.repo.RoleRepo;

/**
 * SEE Manager is part of the Software Engineering Experience (SEE).
 * 
 * This is the back-end application that exposes a REST API for creating and
 * managing SEE game server instances.
 * It also exposes an API to store and retrieve multiplayer configurations
 * and Code City data to be used by multiple SEE clients.
 * 
 * @see <a href="https://see.uni-bremen.de/">SEE Website</a>
 */
@SpringBootApplication
@Slf4j
public class SeeManagerApplication {
    
    @Value("${see.app.backend.domain}")
    private String backendDomain;
    
    @Value("${see.app.admin.add.name}")
    private String newAdminName;
    
    // FIXME This is not a secure way to handle passwords.
    @Value("${see.app.admin.add.password}")
    private String newAdminPassword;

    /**
     * The main method to run the Spring Boot application.
     * @param args command-line arguments
     */
    public static void main(String[] args) {
        SpringApplication.run(SeeManagerApplication.class, args);
    }

    /**
     * Configures and returns a CommandLineRunner bean.
     * 
     * This runner performs necessary initialization tasks or setup operations
     * at application startup.
     * 
     * Required dependencies are listed in the parameters.
     */
    @Bean
    CommandLineRunner cliRunner(
            ServerRepo serverRepo, 
            ServerService serverService, 
            UserRepo userRepo, 
            UserService userService, 
            FileService fileService, 
            RoleRepo roleRepo, 
            ConfigRepo configRepo) {
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
}