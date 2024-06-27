package de.unibremen.swt.see.manager.repo;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;
import de.unibremen.swt.see.manager.model.RoleType;
import de.unibremen.swt.see.manager.model.Role;

import java.util.Optional;

@Repository
public interface RoleRepo extends JpaRepository<Role, Long> {
    Optional<Role> findByName(RoleType name);
}
