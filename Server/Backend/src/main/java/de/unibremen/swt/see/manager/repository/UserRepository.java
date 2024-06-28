package de.unibremen.swt.see.manager.repository;

import de.unibremen.swt.see.manager.model.User;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.repository.PagingAndSortingRepository;
import org.springframework.stereotype.Repository;

/**
 * Repository interface for managing {@link User} entities.
 * <p>
 * This interface extends {@link PagingAndSortingRepository} to provide CRUD
 * operations as well as pagination and sorting capabilities for User entities.
 * <p>
 * See also:<br>
 * <a href="https://docs.spring.io/spring-data/commons/docs/current/api/org/springframework/data/repository/PagingAndSortingRepository.html">PagingAndSortingRepository</a>
 *
 * @see User
 * @see UUID
 */
@Repository
public interface UserRepository extends PagingAndSortingRepository<User, UUID> {

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
    Optional<User> findUserByUsername(String username);

    /**
     * Saves a given user entity in the database.
     * <p>
     * If the user is new (i.e., has no ID), it will be inserted. If the user
     * already exists (i.e., has an ID), it will be updated.
     *
     * @param user the user entity to be saved
     * @return the saved user entity
     */
    User save(User user);

    /**
     * Retrieves all user entities from the database.
     * <p>
     * This method returns all users currently stored in the database. If there
     * are no users, an empty list is returned.
     * <p>
     * Note that this method may be resource-intensive for large datasets.
     *
     * @return a list containing all users, or an empty list if no users exist
     */
    List<User> findAll();

    /**
     * Deletes a user from the database based on the provided username.
     * <p>
     * This method attempts to find a user with the given username and delete
     * them from the database.
     *
     * @param username the username of the user to be deleted
     */
    void deleteUserByUsername(String username);
}
