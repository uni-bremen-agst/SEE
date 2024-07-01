package de.unibremen.swt.see.manager.repository;

import de.unibremen.swt.see.manager.model.Config;
import java.util.Optional;
import org.springframework.data.repository.PagingAndSortingRepository;
import org.springframework.stereotype.Repository;

/**
 * Repository interface for managing {@link Config} entities.
 * <p>
 * This interface extends {@link PagingAndSortingRepository} to provide CRUD
 * operations as well as pagination and sorting capabilities for {@link Config}
 * entities.
 * <p>
 * See also:<br>
 * <a href="https://docs.spring.io/spring-data/commons/docs/current/api/org/springframework/data/repository/PagingAndSortingRepository.html">PagingAndSortingRepository</a>
 *
 * @see Config
 */
@Repository
public interface ConfigRepository extends PagingAndSortingRepository<Config, Integer> {

    /**
     * Retrieves a configuration by their ID.
     * <p>
     * This method retrieves the configuration with the provided ID. If no
     * configuration is found with the given ID, an empty {@code Optional} is
     * returned.
     *
     * @param id the ID of the configuration
     * @return an {@link Optional} containing the configuration if existent, or
     * an empty {@code Optional} if no configuration exists with the given ID
     */
    Optional<Config> findConfigById(Integer id);

    /**
     * Saves a given configuration entity in the database.
     * <p>
     * If the configuration is new (i.e., has no ID), it will be inserted. If
     * the configuration already exists (i.e., has an ID), it will be updated.
     *
     * @param config the configuration entity to be saved
     * @return the saved configuration entity
     */
    Config save(Config config);
}
