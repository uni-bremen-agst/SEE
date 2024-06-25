package de.unibremen.swt.see.manager.service;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import de.unibremen.swt.see.manager.model.ERole;
import de.unibremen.swt.see.manager.model.Role;
import de.unibremen.swt.see.manager.model.User;
import de.unibremen.swt.see.manager.repo.RoleRepository;
import de.unibremen.swt.see.manager.repo.UserRepo;

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
        Optional<User> optUser = userRepo.findUserByUsername(oldUsername);
        if (optUser.isPresent()) {
            log.error("Username to be changed not found: {}", oldUsername);
            return null;
        }
        User user = optUser.get();
            
        if (passwordEncoder.matches(password, user.getPassword())) {
            log.info("Changing username form {} to {}", oldUsername, newUsername);
            user.setUsername(newUsername);
            return user;
        }
        log.error("Unauthorized username change requested: {} to {}", oldUsername, newUsername);
        return null;
    }

    public User addRoleToUser(String username, ERole eRole) {
        log.info("Adding role {} to {}", eRole, username);
        Optional<User> optUser = userRepo.findUserByUsername(username);
        if (optUser.isEmpty()) return null;
        User user = optUser.get();
        
        Optional<Role> optRole = roleRepository.findByName(eRole);
        if (optRole.isEmpty()) return null;
        Role role = optRole.get();

        user.getRoles().add(role);
        return user;
    }

    public User removeRoleToUser(String username, ERole eRole) {
        log.info("Removing role {} to {}", eRole, username);
        Optional<User> optUser = userRepo.findUserByUsername(username);
        if (optUser.isEmpty()) return null;
        User user = optUser.get();
        
        Optional<Role> optRole = roleRepository.findByName(eRole);
        if (optRole.isEmpty()) return null;
        Role role = optRole.get();
        
        user.getRoles().remove(role);
        return user;
    }

    public boolean changePassword(String username, String oldPassword, String newPassword) {
        Optional<User> optUser = userRepo.findUserByUsername(username);
        if (optUser.isPresent()) {
            log.error("A user tried to change their password with wrong username.");
            return false;
        }
        User user = optUser.get();
        
        if (passwordEncoder.matches(oldPassword, user.getPassword())) {
            user.setPassword(passwordEncoder.encode(newPassword));
            log.info("A user changed their password.");
            return true;
        }
        log.error("A user tried to change their password with wrong password.");
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
