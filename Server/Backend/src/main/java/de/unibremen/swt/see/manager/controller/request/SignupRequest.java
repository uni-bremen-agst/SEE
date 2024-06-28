package de.unibremen.swt.see.manager.controller.request;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;
import lombok.Data;
import de.unibremen.swt.see.manager.model.RoleType;

@Data
public class SignupRequest {
    @NotBlank
    @Size(min = 3, max = 20)
    private String username;

    private RoleType role;

    @NotBlank
    @Size(min = 6, max = 40)
    private String password;
}
