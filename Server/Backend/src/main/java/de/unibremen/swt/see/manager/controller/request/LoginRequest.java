package de.unibremen.swt.see.manager.controller.request;

import jakarta.validation.constraints.NotBlank;
import lombok.Data;

/**
 * Data container for a log-in request.
 */
@Data
public class LoginRequest {

    /**
     * Name of the user to log in.
     */
    @NotBlank
    private String username;

    /**
     * Password of the user to log in.
     */
    @NotBlank
    private String password;
}
