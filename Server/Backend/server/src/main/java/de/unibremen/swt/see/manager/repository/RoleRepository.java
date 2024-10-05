package de.unibremen.swt.see.manager.repository;

import de.unibremen.swt.see.manager.model.Role;
import de.unibremen.swt.see.manager.model.RoleType;
import java.util.Optional;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

/**
 * Repository interface for managing {@link Role} entities.
 * <p>
 * This interface extends {@link JpaRepository} to provide CRUD operations for
 * {@link Role} entities.
 *
 * @see Role
 */
@Repository
public interface RoleRepository extends JpaRepository<Role, Long> {

    /**
     * Retrieves a role by its username.
     * <p>
     * This method performs a case-sensitive search for a role with the exact
     * name provided. If no role is found with the given name, an empty
     * {@code Optional} is returned.
     *
     * @param name the name of the role to find
     * @return an {@link Optional} containing the role if found, or an empty
     * {@code Optional} if no role exists with the given name
     */
    Optional<Role> findByName(RoleType name);
}
