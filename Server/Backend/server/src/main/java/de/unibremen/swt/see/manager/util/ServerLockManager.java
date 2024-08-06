package de.unibremen.swt.see.manager.util;

import de.unibremen.swt.see.manager.model.Server;
import java.util.HashMap;
import java.util.Map;
import java.util.UUID;
import java.util.concurrent.locks.ReentrantLock;

/**
 * This singleton manages the locks to synchronize writes on server entities.
 */
public class ServerLockManager {

    /**
     * Singleton instance.
     */
    private static final ServerLockManager instance = new ServerLockManager();

    /**
     * The stored locks.
     */
    private final Map<UUID, ReentrantLock> locks = new HashMap<>();

    /**
     * Private constructor to prevent instantiation (singleton).
     */
    private ServerLockManager() {
    }

    /**
     * @return the {@code ServerLockManager} instance
     */
    public static ServerLockManager getInstance() {
        return instance;
    }

    /**
     * Retrieves or creates a lock associated with a server entity.
     *
     * @param server the server entity
     * @return the lock associated with the server entity
     */
    public ReentrantLock getLock(Server server) {
        return locks.computeIfAbsent(server.getId(), k -> new ReentrantLock());
    }

    /**
     * Retrieves or creates a lock associated with a server entity.
     *
     * @param serverId the ID of the server entity
     * @return the lock associated with the server entity
     */
    public ReentrantLock getLock(UUID serverId) {
        return locks.computeIfAbsent(serverId, k -> new ReentrantLock());
    }

    /**
     * Removes a lock from the manager for given server entity.
     *
     * @param server the server entity
     */
    public void removeLock(Server server) {
        locks.remove(server.getId());
    }

    /**
     * Removes a lock from the manager for given server entity.
     *
     * @param serverId the ID of the server entity
     */
    public void removeLock(UUID serverId) {
        locks.remove(serverId);
    }
}
