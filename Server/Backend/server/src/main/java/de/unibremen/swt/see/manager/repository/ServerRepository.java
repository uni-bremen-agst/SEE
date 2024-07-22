package de.unibremen.swt.see.manager.repository;

import de.unibremen.swt.see.manager.model.Server;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

/**
 * Repository interface for managing {@link Server} entities.
 * <p>
 * This interface extends {@link JpaRepository} to provide CRUD operations for
 * {@link Server} entities.
 *
 * @see Server
 */
@Repository
public interface ServerRepository extends JpaRepository<Server, UUID> {

    /**
     * Retrieves a server by unique container port attribute.
     *
     * @param port the container port of the server to be found
     * @return an {@link Optional} containing the server if existent, or an
     * empty {@code Optional} if no server exists with the given container port
     */
    Optional<Server> findByContainerPort(int port);

}
