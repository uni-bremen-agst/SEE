package de.unibremen.swt.see.manager.model;

import com.fasterxml.jackson.annotation.JsonIgnore;
import jakarta.persistence.*;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;
import java.util.HashSet;
import java.util.Set;
import java.util.UUID;
import lombok.Getter;
import lombok.Setter;

/**
 * Represents a user of the management system.
 */
@Entity
@Getter
@Setter
@Table(name = "users",
        uniqueConstraints = {
            @UniqueConstraint(columnNames = {"username"})
        })
public class User {
    
    /**
     * ID of the user.
     */
    @Id
    @GeneratedValue(strategy = GenerationType.UUID)
    @Column(name = "id", updatable = false)
    private UUID id;

    /**
     * The username of the user.
     * <p>
     * The username must be unique and up to 20 characters long.
     */
    @NotBlank
    @Size(max = 36)
    @Column(name = "username", unique = true)
    private String username;

    /**
     * The password of the user.
     * <p>
     * The password is usually stored as a salted hash value, e.g., using
     * {@code bcrypt} algorithm (as of 2024).
     */
    @NotBlank
    @JsonIgnore
    @Size(max = 120)
    private String password;

    /**
     * A list of roles associated to the user.
     * <p>
     * One user can assume multiple roles that are checked during API access.
     */
    @ManyToMany(fetch = FetchType.LAZY)
    @JoinTable(name = "user_roles",
            joinColumns = @JoinColumn(name = "user_id"),
            inverseJoinColumns = @JoinColumn(name = "role_id"))
    private Set<Role> roles = new HashSet<>();

    /**
     * A list of servers associated to the user.
     * <p>
     * One user can be granted access to multiple servers.
     */
    @ManyToMany(fetch = FetchType.LAZY)
    @JoinTable(name = "user_servers",
            joinColumns = @JoinColumn(name = "user_id"),
            inverseJoinColumns = @JoinColumn(name = "server_id"))
    @JsonIgnore
    private Set<Server> servers = new HashSet<>();

    /**
     * Constructs an empty {@code User}.
     */
    public User() {}

    /**
     * Constructs a {@code User} with name and password.
     *
     * @param username username of the new user
     * @param password password hash of the new user
     */
    public User(String username, String password) {
        this.username = username;
        this.password = password;
    }
}
