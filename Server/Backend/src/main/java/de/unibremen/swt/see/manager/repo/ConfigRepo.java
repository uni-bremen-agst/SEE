package de.unibremen.swt.see.manager.repo;

import org.springframework.data.repository.PagingAndSortingRepository;
import de.unibremen.swt.see.manager.model.Config;

import java.util.Optional;

public interface ConfigRepo extends PagingAndSortingRepository<Config, Integer> {
    Optional<Config> findConfigById(Integer id);

    Config save(Config config);
}
