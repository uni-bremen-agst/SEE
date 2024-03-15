package uni.bachelorprojekt.see.repo;

import org.springframework.data.repository.PagingAndSortingRepository;
import uni.bachelorprojekt.see.model.ServerConfig;

import java.util.Optional;

public interface ServerConfigRepo extends PagingAndSortingRepository<ServerConfig, Integer> {
    Optional<ServerConfig> findServerConfigById(Integer id);

    ServerConfig save(ServerConfig serverConfig);
}
