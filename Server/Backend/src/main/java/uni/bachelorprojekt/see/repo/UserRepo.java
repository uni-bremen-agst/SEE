package uni.bachelorprojekt.see.repo;

import org.springframework.data.repository.PagingAndSortingRepository;
import uni.bachelorprojekt.see.model.User;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

public interface UserRepo extends PagingAndSortingRepository<User, UUID> {
    Optional<User> findUserByUsername(String username);

    User save(User user);

    List<User> findAll();

    void deleteUserByUsername(String username);
}
