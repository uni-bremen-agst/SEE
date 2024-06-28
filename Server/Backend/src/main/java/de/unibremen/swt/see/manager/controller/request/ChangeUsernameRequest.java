package de.unibremen.swt.see.manager.controller.request;

import lombok.Data;

@Data
public class ChangeUsernameRequest {
    private String newUsername;
    private String password;
}
