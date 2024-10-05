package de.unibremen.swt.see.manager.controller.request;

import de.unibremen.swt.see.manager.model.RoleType;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;
import lombok.Data;

/**
 * Data container for a sign-up request.
 */
@Data
public class SignupRequest {

    /**
     * Name of the new user account.
     */
    @NotBlank
    @Size(min = 3, max = 20)
    private String username;

    /**
     * Role that should be assigned to the new user account.
     */
    private RoleType role;

    /**
     * Password that should be assigned to the new user account.
     */
    @NotBlank
    @Size(min = 6, max = 40)
    private String password;
}
