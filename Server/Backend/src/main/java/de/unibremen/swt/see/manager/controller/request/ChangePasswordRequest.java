package de.unibremen.swt.see.manager.controller.request;

import lombok.Data;

/**
 * Data container for a password change request.
 */
@Data
public class ChangePasswordRequest {

    /**
     * User's old password used for validation.
     */
    private String oldPassword;

    /**
     * New password that should be set.
     */
    private String newPassword;
}
