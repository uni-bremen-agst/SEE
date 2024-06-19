package de.unibremen.swt.see.manager.repo;

import org.springframework.data.repository.PagingAndSortingRepository;
import de.unibremen.swt.see.manager.model.Server;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

public interface ServerRepo extends PagingAndSortingRepository<Server, UUID> {
    Optional<Server> findServerById(UUID id);

    Server save(Server server);

    List<Server> findAll();

    void deleteServerById(UUID id);
}
