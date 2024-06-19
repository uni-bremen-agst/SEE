package de.unibremen.swt.see.manager.repo;

import org.springframework.data.repository.PagingAndSortingRepository;
import de.unibremen.swt.see.manager.model.ServerConfig;

import java.util.Optional;

public interface ServerConfigRepo extends PagingAndSortingRepository<ServerConfig, Integer> {
    Optional<ServerConfig> findServerConfigById(Integer id);

    ServerConfig save(ServerConfig serverConfig);
}
