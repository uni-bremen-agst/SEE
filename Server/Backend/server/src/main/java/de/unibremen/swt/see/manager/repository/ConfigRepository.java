package de.unibremen.swt.see.manager.repository;

import de.unibremen.swt.see.manager.model.Config;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

/**
 * Repository interface for managing {@link Config} entities.
 * <p>
 * This interface extends {@link JpaRepository} to provide CRUD operations for
 * {@link Config} entities.
 *
 * @see Config
 */
@Repository
public interface ConfigRepository extends JpaRepository<Config, Integer> {

}
