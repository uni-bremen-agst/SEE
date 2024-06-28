package de.unibremen.swt.see.manager.controller.request;

import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class AuthenticateUserRequest {
    private String username;

    private String password;
}