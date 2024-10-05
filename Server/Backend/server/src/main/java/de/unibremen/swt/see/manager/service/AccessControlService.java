package de.unibremen.swt.see.manager.service;

import de.unibremen.swt.see.manager.model.File;
import de.unibremen.swt.see.manager.model.RoleType;
import de.unibremen.swt.see.manager.model.Server;
import de.unibremen.swt.see.manager.model.User;
import java.nio.file.AccessDeniedException;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

/**
 * Service class for managing operations related to access control.
 */
@Service
@Slf4j
@RequiredArgsConstructor
public class AccessControlService {

    /**
     * Used to access files.
     */
    private final FileService fileService;

    /**
     * Used to access servers.
     */
    private final ServerService serverService;

    /**
     * Used to access users.
     */
    private final UserService userService;

    /**
     * Evaluates if given user has access to given server.
     *
     * @param user the user
     * @param server the server
     * @return {@code true} if the user is granted access to the server
     * @throws AccessDeniedException if the access cannot be granted
     */
    public boolean canAccessServer(User user, Server server) throws AccessDeniedException {
        if (userService.hasRole(user, RoleType.ROLE_ADMIN) || userService.hasServer(user, server)) {
            return true;
        }
        throw new AccessDeniedException("User is not allowed to access the server!");
    }

    /**
     * Evaluates if given user has access to given server.
     *
     * @param userId the user ID
     * @param serverId the server ID
     * @return {@code true} if the user is granted access to the server
     * @throws AccessDeniedException if the access cannot be granted
     */
    public boolean canAccessServer(UUID userId, UUID serverId) throws AccessDeniedException {
        final User user = userService.get(userId);
        final Server server = serverService.get(serverId);
        return canAccessServer(user, server);
    }

    /**
     * Evaluates if given user has access to given file.
     *
     * @param user the user
     * @param file the file
     * @return {@code true} if the user is granted access to the file
     * @throws AccessDeniedException if the access cannot be granted
     */
    public boolean canAccessFile(User user, File file) throws AccessDeniedException {
        if (user != null && file != null) {
            final Server server = file.getServer();
            try {
                return canAccessServer(user, server);
            } catch (AccessDeniedException e) {
                // Do nothing
            }
        }
        throw new AccessDeniedException("User is not allowed to access the file!");
    }

    /**
     * Evaluates if given user has access to given file.
     *
     * @param userId the user ID
     * @param fileId the file ID
     * @return {@code true} if the user is granted access to the file
     * @throws AccessDeniedException if the access cannot be granted
     */
    public boolean canAccessFile(UUID userId, UUID fileId) throws AccessDeniedException {
        final User user = userService.get(userId);
        final File file = fileService.get(fileId);
        return canAccessFile(user, file);
    }

}
