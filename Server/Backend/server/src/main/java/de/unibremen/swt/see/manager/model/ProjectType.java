package de.unibremen.swt.see.manager.model;

/**
 * Represents the different types of projects in a SEE virtual world, usually
 * represented as Code City variants on different tables.
 * <p>
 * This {@code enum} defines the project types that are used by the SEE game
 * server to configure multiplayer scenarios and Code Cities.
 *
 * @see de.unibremen.swt.see.manager.service.FileService#create(Server,
 * ProjectType, MultipartFile)
 */
public enum ProjectType {

    /**
     * Used to display differences between two revisions of a software stored in
     * a version control system (VCS).
     */
    DiffCity,
    /**
     * This is default Code City type.
     */
    SEECity,
    /**
     * Used to display an animated evolution of a {@code SEECity}.
     */
    SEECityEvolution,
    /**
     * Used for the visualization of dynamic data traced at the level of
     * statements.
     */
    SEEJlgCity,
    /**
     * Used to support architectural mappings from implementation nodes onto
     * architecture nodes.
     */
    SEEReflexionCity,

}
