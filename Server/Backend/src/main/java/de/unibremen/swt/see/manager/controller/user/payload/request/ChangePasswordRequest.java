package de.unibremen.swt.see.manager.controller.user.payload.request;

import lombok.Data;

@Data
public class ChangePasswordRequest {
    private String oldPassword;
    private String newPassword;
}
