package de.unibremen.swt.see.manager.controller.user.payload.request;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;
import lombok.Data;
import de.unibremen.swt.see.manager.model.ERole;

@Data
public class SignupRequest {
    @NotBlank
    @Size(min = 3, max = 20)
    private String username;

    private ERole role;

    @NotBlank
    @Size(min = 6, max = 40)
    private String password;
}
