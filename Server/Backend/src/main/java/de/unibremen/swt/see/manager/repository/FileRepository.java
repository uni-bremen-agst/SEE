package de.unibremen.swt.see.manager.repository;

import de.unibremen.swt.see.manager.model.File;
import de.unibremen.swt.see.manager.model.FileType;
import de.unibremen.swt.see.manager.model.Server;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.repository.PagingAndSortingRepository;
import org.springframework.stereotype.Repository;

/**
 * Repository interface for managing {@link File} entities.
 * <p>
 * This interface extends {@link PagingAndSortingRepository} to provide CRUD
 * operations as well as pagination and sorting capabilities for {@link File}
 * entities.
 * <p>
 * See also:<br>
 * <a href="https://docs.spring.io/spring-data/commons/docs/current/api/org/springframework/data/repository/PagingAndSortingRepository.html">PagingAndSortingRepository</a>
 *
 * @see File
 * @see UUID
 */
@Repository
public interface FileRepository extends PagingAndSortingRepository<File, UUID> {

    /**
     * Saves a given file entity in the database.
     * <p>
     * If the file is new (i.e., has no ID), it will be inserted. If the file
     * already exists (i.e., has an ID), it will be updated.
     *
     * @param file the file entity to be saved
     * @return the saved file entity
     */
    File save(File file);

    /**
     * Retrieves a file by their ID.
     * <p>
     * This method retrieves the file with the provided ID. If no file is found
     * with the given ID, an empty {@code Optional} is returned.
     *
     * @param fileId the ID of the file
     * @return an {@link Optional} containing the file if existent, or an empty
     * {@code Optional} if no file exists with the given ID
     */
    Optional<File> findById(UUID fileId);

    /**
     * Checks if a file with the given ID exists in the database.
     *
     * @param fileId the ID of the file
     * @return {@code true} if the file is existent, or else {@code false}
     */
    boolean existsById(UUID fileId);

    /**
     * Deletes a file from the database.
     *
     * @param file the file to be deleted
     */
    void delete(File file);

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
    List<File> findFilesByServer(Server server);

    /**
     * Retrieves a file by its type and server association.
     * <p>
     * This method retrieves the file with the provided type that is linked to
     * the provided server. If no such file is found, an empty {@code Optional}
     * is returned.
     *
     * @param server the server that files should be retrieved for
     * @param fileType the type of the file to be retrieved
     * @return an {@link Optional} containing the file if existent, or an empty
     * {@code Optional} if no such file exists
     */
    Optional<File> findFileByServerAndFileType(Server server, FileType fileType);

    /**
     * Deletes all file entities from the database that are associated with the
     * given server.
     *
     * @param server the server that files should be deleted for
     */
    void deleteFilesByServer(Server server);
}
