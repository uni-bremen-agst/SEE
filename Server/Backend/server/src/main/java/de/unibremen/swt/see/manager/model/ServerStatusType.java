package de.unibremen.swt.see.manager.model;

/**
 * Represents the different status a SEE game server can assume.
 * 
 * @see Server
 */
public enum ServerStatusType {

    /**
     * The server is running and accepting connections.
     */
    ONLINE,

    /**
     * The server is offline.
     */
    OFFLINE,

    /**
     * An error occurred during server management.
     * <p>
     * The server is in an undefined state and should not be accessed.
     * This can, e.g., mean that starting or stopping has failed,
     * or the server is not responding.
     */
    ERROR,
}
