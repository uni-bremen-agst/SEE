package de.unibremen.swt.see.manager.model;

/**
 * Represents the different roles a user can assume.
 * <p>
 * This {@code enum} defines the roles that are used in the role-based access 
 * model implemented with the Spring Security framework.
 * 
 * @see Role
 */
public enum RoleType {
    /**
     * This role represents the application administrator.
     */
    ROLE_ADMIN,
    
    /**
     * This role represents a normal user of the application.
     */
    ROLE_USER,
}
