package de.unibremen.swt.see.manager.repository;

import org.springframework.data.repository.PagingAndSortingRepository;
import de.unibremen.swt.see.manager.model.Config;

import java.util.Optional;
import org.springframework.stereotype.Repository;

@Repository
public interface ConfigRepository extends PagingAndSortingRepository<Config, Integer> {
    Optional<Config> findConfigById(Integer id);

    Config save(Config config);
}
