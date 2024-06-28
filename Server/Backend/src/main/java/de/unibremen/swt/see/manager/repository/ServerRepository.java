package de.unibremen.swt.see.manager.repository;

import de.unibremen.swt.see.manager.model.Server;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.repository.PagingAndSortingRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface ServerRepository extends PagingAndSortingRepository<Server, UUID> {
    Optional<Server> findServerById(UUID id);

    Server save(Server server);

    List<Server> findAll();

    void deleteServerById(UUID id);
}
