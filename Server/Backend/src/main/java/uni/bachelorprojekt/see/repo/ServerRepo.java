package uni.bachelorprojekt.see.repo;

import org.springframework.data.repository.PagingAndSortingRepository;
import uni.bachelorprojekt.see.model.Server;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

public interface ServerRepo extends PagingAndSortingRepository<Server, UUID> {
    Optional<Server> findServerById(UUID id);

    Server save(Server server);

    List<Server> findAll();

    void deleteServerById(UUID id);
}
