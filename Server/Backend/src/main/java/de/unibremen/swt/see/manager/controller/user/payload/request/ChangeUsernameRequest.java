package de.unibremen.swt.see.manager.controller.user.payload.request;

import lombok.Data;

@Data
public class ChangeUsernameRequest {
    private String newUsername;
    private String password;
}
