package de.unibremen.swt.see.manager.service;

import de.unibremen.swt.see.manager.model.Role;
import de.unibremen.swt.see.manager.model.RoleType;
import de.unibremen.swt.see.manager.model.Server;
import de.unibremen.swt.see.manager.model.User;
import de.unibremen.swt.see.manager.repository.RoleRepository;
import de.unibremen.swt.see.manager.repository.UserRepository;
import java.util.List;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

/**
 * Service class for managing user-related operations.
 * <p>
 * This service provides high-level operations for user management, including
 * creating, retrieving, updating, and deleting users. It encapsulates the
 * business logic and acts as an intermediary between the controller layer and
 * the data access layer.
 *
 * @see UserRepository
 * @see de.unibremen.swt.see.manager.controller.UserController
 */
@Service
@Transactional
@RequiredArgsConstructor
@Slf4j
public class UserService {

    /**
     * Provides access to role data for user management operations.
     */
    private final RoleRepository roleRepo;
    /**
     * Enables user data persistence and retrieval for this service.
     */
    private final UserRepository userRepo;

    /**
     * Handles password encryption for user authentication.
     */
    private final PasswordEncoder passwordEncoder;

    /**
     * Creates a new user from the provided attributes.
     *
     * @param username username of the new user
     * @param password password of the new user
     * @param roleType type of the role assigned to the new user
     * @return the created user, or {@code null} if the username is already
     * taken or the role could not be assigned.
     */
    public User create(String username, String password, RoleType roleType) {
        log.info("Creating new user {}", username);
        if (userRepo.findByUsername(username).isPresent()) {
            log.error("Username {} is already taken", username);
            return null;
        }
        User user = userRepo.save(new User(username, passwordEncoder.encode(password)));

        return addRole(user, roleType);
    }

    /**
     * Retrieves a user by its ID.
     *
     * @param id the ID of the user
     * @return the user if found, or {@code null} if not found
     */
    @Transactional(readOnly = true)
    public User get(UUID id) {
        return userRepo.findById(id).orElse(null);
    }

    /**
     * Updates the username of the given user.
     * <p>
     * The user is authenticated using the password before the username is
     * changed. Data model should handle uniqueness of the new username.
     *
     * @param oldUsername the old username that should be updated
     * @param newUsername the new username that should be set
     * @param password password to authenticate the user
     * @return updated user, or {@code null} if the user does not exist or the
     * password is not correct or new username cannot be used
     */
    public User changeUsername(final String oldUsername, final String newUsername, final String password) {
        User user = getByUsername(oldUsername);

        if (user != null && passwordEncoder.matches(password, user.getPassword())) {
            log.info("Changing username form {} to {}", oldUsername, newUsername);
            user.setUsername(newUsername);
            return user;
        }

        log.error("Unauthorized username change requested: {} to {}", oldUsername, newUsername);
        return null;
    }

    /**
     * Adds a role to given user.
     *
     * @param user the user
     * @param roleType type of the role to be assigned to the user
     * @return updated user, or {@code null} if user or role is not found in
     * database
     */
    public User addRole(User user, RoleType roleType) {
        if (user == null) {
            return null;
        }
        log.info("Adding role {} to {}", roleType, user.getUsername());
        
        Optional<Role> optRole = roleRepo.findByName(roleType);
        if (optRole.isEmpty()) {
            log.error("Role not found in database: {}", roleType.toString());
            return null;
        }
        Role role = optRole.get();

        user.getRoles().add(role);
        return user;
    }

    /**
     * Adds a role to given user by its {@code username}.
     *
     * @param username username to identify the user
     * @param roleType type of the role to be assigned to the user
     * @return updated user, or {@code null} if user or role is not found in
     * database
     */
    public User addRole(String username, RoleType roleType) {
        final User user = getByUsername(username);
        if (user == null) {
            log.warn("User for role update not found: {}", username);
            return null;
        }
        return addRole(user, roleType);
    }

    /**
     * Adds a server to given user.
     *
     * @param user the user
     * @param server the server
     * @return the user, or {@code null} if either the user or the server is not
     * set
     */
    public User addServer(User user, Server server) {
        if (user == null || server == null) {
            return null;
        }
        log.info("Adding server {} to {}", server.getId(), user.getUsername());
        user.getServers().add(server);
        return user;
    }

    /**
     * Removes a role from given user.
     *
     * @param username username to identify the user
     * @param roleType type of the role to be removed from the user
     * @return updated user, or {@code null} if user or role is not found in
     * database
     */
    public User removeRole(String username, RoleType roleType) {
        log.info("Removing role {} from {}", roleType, username);

        User user = getByUsername(username);
        if (user == null) {
            log.warn("User for role update not found: {}", username);
            return null;
        }
        
        Optional<Role> optRole = roleRepo.findByName(roleType);
        if (optRole.isEmpty()) {
            log.error("Role not found in database: {}", roleType.toString());
            return null;
        }
        Role role = optRole.get();

        user.getRoles().remove(role);
        return user;
    }

    /**
     * Updates the password of the given user.
     * <p>
     * The user is authenticated using the old password before the password is
     * changed.
     *
     * @param username username to identify the user
     * @param oldPassword old password to authenticate the user
     * @param newPassword new password to be set
     * @return updated user, or {@code null} if the user does not exist or the
     * old password is not correct
     */
    public boolean changePassword(String username, String oldPassword, String newPassword) {
        Optional<User> optUser = userRepo.findByUsername(username);
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

    /**
     * Retrieves all users.
     *
     * @return a list containing all users
     */
    public List<User> getAll() {
        log.info("Fetching all users");
        return userRepo.findAll();
    }

    /**
     * Retrieves a user by their username.
     *
     * @param username the username of the user to retrieve
     * @return the user if found, or {@code null} if not found
     */
    public User getByUsername(String username) {
        log.info("Fetching user {}", username);
        return userRepo.findByUsername(username).orElse(null);
    }

    /**
     * Deletes a user by their ID.
     *
     * @param id the user ID
     */
    public void delete(UUID id) {
        log.info("Deleting user by ID: {}", id);
        userRepo.deleteById(id);
    }

    /**
     * Deletes a user by their username.
     *
     * @param username the username of the user to delete
     */
    public void deleteByUsername(String username) {
        log.info("Deleting user by username: {}", username);
        userRepo.deleteByUsername(username);
    }

    /**
     * Checks if a user has given role.
     *
     * @param user the user
     * @param expectedType the type of the role
     * @return {@code true} if given user has a role with given type
     */
    public boolean hasRole(User user, RoleType expectedType) {
        if (user == null) {
            return false;
        }

        final Set<Role> roles = user.getRoles();
        for (Role role : roles) {
            if (role.getName() == expectedType) {
                return true;
            }
        }

        return false;
    }

    /**
     * Checks if a user has access to given server.
     *
     * @param user the user
     * @param server the server
     * @return {@code true} if given user has access to given server
     */
    public boolean hasServer(User user, Server server) {
        return user != null && server != null && user.getServers().contains(server);
    }

}
