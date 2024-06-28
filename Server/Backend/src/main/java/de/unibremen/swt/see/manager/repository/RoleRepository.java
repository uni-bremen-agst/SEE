package de.unibremen.swt.see.manager.repository;

import de.unibremen.swt.see.manager.model.Role;
import de.unibremen.swt.see.manager.model.RoleType;
import java.util.Optional;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface RoleRepository extends JpaRepository<Role, Long> {
    Optional<Role> findByName(RoleType name);
}
