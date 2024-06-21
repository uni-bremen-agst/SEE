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

@SpringBootApplication
@Slf4j
public class SeeManagerApplication {

    public static void main(String[] args) {
        SpringApplication.run(SeeManagerApplication.class, args);
    }

    @Bean
    CommandLineRunner cliRunner(ServerRepo serverRepo, ServerService serverService, UserRepo userRepo, UserService userService, FileService fileService, RoleRepository roleRepository, ServerConfigRepo serverConfigRepo) {
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