package de.unibremen.swt.see.manager.repository;

import de.unibremen.swt.see.manager.model.File;
import de.unibremen.swt.see.manager.model.FileType;
import de.unibremen.swt.see.manager.model.Server;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

/**
 * Repository interface for managing {@link File} entities.
 * <p>
 * This interface extends {@link JpaRepository} to provide CRUD operations for
 * {@link File} entities.
 *
 * @see File
 */
@Repository
public interface FileRepository extends JpaRepository<File, UUID> {

    /**
     * Retrieves all file entities from the database that are associated with
     * the given server.
     * <p>
     * This method returns all files that are linked to the provided server. If
     * there are no such files, an empty list is returned.
     *
     * @param server the server that files should be retrieved for
     * @return a list containing files associated to given server, or an empty
     * list if no such files exist
     */
    List<File> findByServer(Server server);

    /**
     * Retrieves a file by its type and server association.
     * <p>
     * This method retrieves the file with the provided type that is linked to
     * the provided server. If no such file is found, an empty {@code Optional}
     * is returned.
     *
     * @param serverId the ID of the server that files should be retrieved for
     * @param fileType the type of the file to be retrieved
     * @return an {@link Optional} containing the file if existent, or an empty
     * {@code Optional} if no such file exists
     */
    Optional<File> findByServerIdAndFileType(UUID serverId, FileType fileType);

    /**
     * Deletes all file entities from the database that are associated with the
     * given server.
     *
     * @param server the server that files should be deleted for
     */
    void deleteByServer(Server server);

}
