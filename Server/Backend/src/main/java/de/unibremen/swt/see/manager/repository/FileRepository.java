package de.unibremen.swt.see.manager.repository;

import de.unibremen.swt.see.manager.model.File;
import de.unibremen.swt.see.manager.model.FileType;
import de.unibremen.swt.see.manager.model.Server;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.repository.PagingAndSortingRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface FileRepository extends PagingAndSortingRepository<File, UUID> {

    File save(File file);

    Optional<File> findById(UUID fileID);

    boolean existsById(UUID fileID);

    void delete(File file);

    List<File> findFilesByServer(Server server);

    Optional<File> findFileByServerAndFileType(Server server, FileType fileType);

    void deleteFilesByServer(Server server);
}
