package de.unibremen.swt.see.manager.repository;

import de.unibremen.swt.see.manager.model.Server;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.repository.PagingAndSortingRepository;
import org.springframework.stereotype.Repository;

/**
 * Repository interface for managing {@link Server} entities.
 * <p>
 * This interface extends {@link PagingAndSortingRepository} to provide CRUD
 * operations as well as pagination and sorting capabilities for User entities.
 * <p>
 * See also:<br>
 * <a href="https://docs.spring.io/spring-data/commons/docs/current/api/org/springframework/data/repository/PagingAndSortingRepository.html">PagingAndSortingRepository</a>
 *
 * @see Server
 * @see UUID
 */
@Repository
public interface ServerRepository extends PagingAndSortingRepository<Server, UUID> {

    /**
     * Retrieves a server by their ID.
     * <p>
     * This method retrieves the user with the provided ID. If no server is
     * found with the given ID, an empty {@code Optional} is returned.
     *
     * @param id the ID of the server
     * @return an {@link Optional} containing the server if existent, or an
     * empty {@code Optional} if no server exists with the given ID
     */
    Optional<Server> findServerById(UUID id);

    /**
     * Saves a given server entity in the database.
     * <p>
     * If the server is new (i.e., has no ID), it will be inserted. If the
     * server already exists (i.e., has an ID), it will be updated.
     *
     * @param server the server entity to be saved
     * @return the saved server entity
     */
    Server save(Server server);

    /**
     * Retrieves all server entities from the database.
     * <p>
     * This method returns all servers currently stored in the database. If
     * there are no servers, an empty list is returned.
     * <p>
     * Note that this method may be resource-intensive for large datasets.
     *
     * @return a list containing all servers, or an empty list if no servers
     * exist
     */
    List<Server> findAll();

    /**
     * Deletes a server from the database based on the provided ID.
     *
     * @param id the ID of the server to be deleted
     */
    void deleteServerById(UUID id);
}
