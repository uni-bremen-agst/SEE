package de.unibremen.swt.see.manager.controller.user.payload;

import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class AuthenticateUser {
    private String username;

    private String password;
}