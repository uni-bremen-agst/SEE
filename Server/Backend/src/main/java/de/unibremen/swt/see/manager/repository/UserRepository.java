package de.unibremen.swt.see.manager.repository;

import de.unibremen.swt.see.manager.model.User;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.repository.PagingAndSortingRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface UserRepository extends PagingAndSortingRepository<User, UUID> {
    Optional<User> findUserByUsername(String username);

    User save(User user);

    List<User> findAll();

    void deleteUserByUsername(String username);
}
