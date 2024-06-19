package uni.bachelorprojekt.see;

import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.CommandLineRunner;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import uni.bachelorprojekt.see.model.ERole;
import uni.bachelorprojekt.see.model.Role;
import uni.bachelorprojekt.see.model.ServerConfig;
import uni.bachelorprojekt.see.repo.RoleRepository;
import uni.bachelorprojekt.see.repo.ServerConfigRepo;
import uni.bachelorprojekt.see.repo.ServerRepo;
import uni.bachelorprojekt.see.repo.UserRepo;
import uni.bachelorprojekt.see.service.FileService;
import uni.bachelorprojekt.see.service.ServerService;
import uni.bachelorprojekt.see.service.UserService;

@SpringBootApplication
@Slf4j
public class SeeApplication {

    public static void main(String[] args) {
        SpringApplication.run(SeeApplication.class, args);
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