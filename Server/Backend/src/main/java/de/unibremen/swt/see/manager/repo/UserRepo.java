package de.unibremen.swt.see.manager.repo;

import org.springframework.data.repository.PagingAndSortingRepository;
import de.unibremen.swt.see.manager.model.User;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

public interface UserRepo extends PagingAndSortingRepository<User, UUID> {
    Optional<User> findUserByUsername(String username);

    User save(User user);

    List<User> findAll();

    void deleteUserByUsername(String username);
}
