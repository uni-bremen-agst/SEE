package de.unibremen.swt.see.manager.repository;

import de.unibremen.swt.see.manager.model.User;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

/**
 * Repository interface for managing {@link User} entities.
 * <p>
 * This interface extends {@link JpaRepository} to provide CRUD operations for
 * {@link User} entities.
 *
 * @see User
 */
@Repository
public interface UserRepository extends JpaRepository<User, UUID> {

    /**
     * Retrieves a user by their username.
     * <p>
     * This method performs a case-sensitive search for a user with the exact
     * username provided. If no user is found with the given username, an empty
     * {@code Optional} is returned.
     *
     * @param username the username of the user to find
     * @return an {@link Optional} containing the user if found, or an empty
     * {@code Optional} if no user exists with the given username
     */
    Optional<User> findByUsername(String username);

    /**
     * Deletes a user from the database based on the provided username.
     * <p>
     * This method attempts to find a user with the given username and delete
     * them from the database.
     *
     * @param username the username of the user to be deleted
     */
    void deleteByUsername(String username);

}
