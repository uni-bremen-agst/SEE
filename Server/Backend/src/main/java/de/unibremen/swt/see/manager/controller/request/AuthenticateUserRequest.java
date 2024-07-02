package de.unibremen.swt.see.manager.controller.request;

import lombok.Getter;
import lombok.Setter;

/**
 * Data container for a user authentication request.
 */
@Getter
@Setter
public class AuthenticateUserRequest {

    /**
     * Name of the user to authenticate.
     */
    private String username;

    /**
     * User's password.
     */
    private String password;
}