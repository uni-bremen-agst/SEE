package de.unibremen.swt.see.manager.controller.request;

import lombok.Data;

/**
 * Data container for a username change request.
 */
@Data
public class ChangeUsernameRequest {

    /**
     * New username that should be set.
     */
    private String newUsername;

    /**
     * User's password.
     */
    private String password;
}
