package uni.bachelorprojekt.see.service;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import uni.bachelorprojekt.see.model.ERole;
import uni.bachelorprojekt.see.model.Role;
import uni.bachelorprojekt.see.model.User;
import uni.bachelorprojekt.see.repo.RoleRepository;
import uni.bachelorprojekt.see.repo.UserRepo;

import java.util.List;
import java.util.Optional;

@Service
@Transactional
@RequiredArgsConstructor
@Slf4j
public class UserService {
    private final UserRepo userRepo;
    private final PasswordEncoder passwordEncoder;
    private final RoleRepository roleRepository;

    public User create(String username, String password, ERole eRole) {
        log.info("Creating new user {}", username);
        if (userRepo.findUserByUsername(username).isPresent()) {
            log.error("Username {} is already taken", username);
            return null;
        }
        User user = userRepo.save(new User(username, passwordEncoder.encode(password)));

        addRoleToUser(user.getUsername(), eRole);

        return user;
    }

    public User changeUsername(String oldUsername, String newUsername, String password) {
        Optional<User> user = userRepo.findUserByUsername(oldUsername);
        if (user.isPresent() && passwordEncoder.matches(password, user.get().getPassword())) {
            log.info("Changing username form {} to {}", oldUsername, newUsername);
            user.get().setUsername(newUsername);
            return user.get();
        }
        log.error("Cant change username form {} to {}", oldUsername, newUsername);
        return null;
    }

    public User addRoleToUser(String username, ERole eRole) {
        log.info("Adding role {} to {}", eRole, username);
        Optional<User> user = userRepo.findUserByUsername(username);
        if (user.isEmpty()) return null;
        Optional<Role> role = roleRepository.findByName(eRole);
        if (role.isEmpty()) return null;

        user.get().getRoles().add(role.get());
        return user.get();
    }

    public User removeRoleToUser(String username, ERole eRole) {
        log.info("Removing role {} to {}", eRole, username);
        Optional<User> user = userRepo.findUserByUsername(username);
        if (user.isEmpty()) return null;
        Optional<Role> role = roleRepository.findByName(eRole);
        if (role.isEmpty()) return null;
        user.get().getRoles().remove(role.get());

        return user.get();
    }

    public boolean changePassword(String username, String oldPassword, String newPassword) {

        Optional<User> user = userRepo.findUserByUsername(username);
        if (user.isPresent() && passwordEncoder.matches(oldPassword, user.get().getPassword())) {
            user.get().setPassword(passwordEncoder.encode(newPassword));
            log.info("A user changed his/her password");
            return true;
        }
        log.error("A user tried to change his/her password with wrong password");
        return false;
    }

    public List<User> getAllUser() {
        log.info("Fetching all users");
        return userRepo.findAll();
    }

    public User getUserByUsername(String username) {
        log.info("Fetching user {}", username);
        return userRepo.findUserByUsername(username).orElse(null);
    }

    public void deleteUserByUsername(String username) {
        log.info("Deleting user {}", username);
        userRepo.deleteUserByUsername(username);
    }
}
