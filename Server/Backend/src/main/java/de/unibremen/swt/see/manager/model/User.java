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
                @UniqueConstraint(columnNames = "name")
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
     * The name of the user.
     * <p>
     * The name must be unique and less than 20 characters long.
     */
    @NotBlank
    @Size(max = 20)
    @Column(name = "name", unique = true)
    private String name;

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
     * Constructs an empty {@code User}.
     */
    public User() {}

    /**
     * Constructs a {@code User} with {@code name} and {@code password}.
     * 
     * @param name     name of the new user
     * @param password password hash of the new user
     */
    public User(String name, String password) {
        this.name = name;
        this.password = password;
    }
}
