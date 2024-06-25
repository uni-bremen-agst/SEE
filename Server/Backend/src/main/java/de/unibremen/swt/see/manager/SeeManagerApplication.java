package de.unibremen.swt.see.manager;

import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.CommandLineRunner;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import de.unibremen.swt.see.manager.model.ERole;
import de.unibremen.swt.see.manager.model.Role;
import de.unibremen.swt.see.manager.model.ServerConfig;
import de.unibremen.swt.see.manager.repo.RoleRepository;
import de.unibremen.swt.see.manager.repo.ServerConfigRepo;
import de.unibremen.swt.see.manager.repo.ServerRepo;
import de.unibremen.swt.see.manager.repo.UserRepo;
import de.unibremen.swt.see.manager.service.FileService;
import de.unibremen.swt.see.manager.service.ServerService;
import de.unibremen.swt.see.manager.service.UserService;

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
            RoleRepository roleRepository, 
            ServerConfigRepo serverConfigRepo) {
        return args -> {
            ServerConfig serverConfig = new ServerConfig();
            serverConfig.setDomain("localhost:8080");
            serverConfig.setMinContainerPort(9100);
            serverConfig.setMaxContainerPort(9300);
            serverConfigRepo.save(serverConfig);
            roleRepository.save(new Role(ERole.ROLE_ADMIN));
            roleRepository.save(new Role(ERole.ROLE_USER));
            userService.create("thorsten", "password", ERole.ROLE_ADMIN);
        };
    }
}