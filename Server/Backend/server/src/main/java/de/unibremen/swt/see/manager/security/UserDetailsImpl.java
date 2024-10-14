package de.unibremen.swt.see.manager.security;

import com.fasterxml.jackson.annotation.JsonIgnore;
import de.unibremen.swt.see.manager.model.User;
import java.util.Collection;
import java.util.List;
import java.util.Objects;
import java.util.UUID;
import java.util.stream.Collectors;
import lombok.Getter;
import org.springframework.security.core.GrantedAuthority;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.core.userdetails.UserDetails;

/**
 * Implementation of UserDetails for user authentication.
 * <p>
 * This class represents the authenticated user's details and implements the
 * {@link UserDetails} interface required by Spring Security.
 *
 * @see UserDetails
 */
public class UserDetailsImpl implements UserDetails {

    /**
     * Unique identifier for serialization.
     */
    private static final long serialVersionUID = 1L;

    /**
     * Unique identifier for the user.
     */
    @Getter
    private final UUID id;

    /**
     * Username of the user.
     */
    private final String username;

    /**
     * Password of the user (not serialized).
     */
    @JsonIgnore
    private final String password;

    /**
     * Authorities granted to the user.
     */
    private final Collection<? extends GrantedAuthority> authorities;

    /**
     * Constructs a new UserDetailsImpl instance.
     *
     * @param id the user's ID
     * @param username the user's username
     * @param password the user's password
     * @param authorities the user's granted authorities
     */
    public UserDetailsImpl(UUID id, String username, String password,
                           Collection<? extends GrantedAuthority> authorities) {
        this.id = id;
        this.username = username;
        this.password = password;
        this.authorities = authorities;
    }

    /**
     * Builds a {@code UserDetailsImpl} instance from a {@link User} entity.
     *
     * @param user the {@link User} entity to build from
     * @return the built {@code UserDetailsImpl} instance
     */
    public static UserDetailsImpl build(User user) {
        List<GrantedAuthority> authorities = user.getRoles().stream()
                .map(role -> new SimpleGrantedAuthority(role.getName().name()))
                .collect(Collectors.toList());

        return new UserDetailsImpl(
                user.getId(),
                user.getUsername(),
                user.getPassword(),
                authorities);
    }

    /**
     * Returns the authorities granted to the user.
     *
     * @return the user's granted authorities
     */
    @Override
    public Collection<? extends GrantedAuthority> getAuthorities() {
        return authorities;
    }

    /**
     * Returns the user's password.
     *
     * @return the user's password
     */
    @Override
    public String getPassword() {
        return password;
    }

    /**
     * Returns the user's username.
     *
     * @return the user's username
     */
    @Override
    public String getUsername() {
        return username;
    }

    /**
     * Dummy implementation.
     * <p>
     * Always returns {@code true}.
     *
     * @return {@code true}
     */
    @Override
    public boolean isAccountNonExpired() {
        return true;
    }

    /**
     * Dummy implementation.
     * <p>
     * Always returns {@code true}.
     *
     * @return {@code true}
     */
    @Override
    public boolean isAccountNonLocked() {
        return true;
    }

    /**
     * Dummy implementation.
     * <p>
     * Always returns {@code true}.
     *
     * @return {@code true}
     */
    @Override
    public boolean isCredentialsNonExpired() {
        return true;
    }

    /**
     * Dummy implementation.
     * <p>
     * Always returns {@code true}.
     *
     * @return {@code true}
     */
    @Override
    public boolean isEnabled() {
        return true;
    }

    /**
     * Compares the instance with the given object.
     * <p>
     * Performs only simple class type and ID comparisons.
     *
     * @param o the other object
     * @return {@code true} if given object is of the same type and has the same
     * ID, else {@code false}.
     */
    @Override
    public boolean equals(Object o) {
        if (this == o)
            return true;
        if (o == null || getClass() != o.getClass())
            return false;
        UserDetailsImpl user = (UserDetailsImpl) o;
        return Objects.equals(id, user.id);
    }

}
