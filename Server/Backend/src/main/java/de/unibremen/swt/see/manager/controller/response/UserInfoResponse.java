package de.unibremen.swt.see.manager.controller.response;

import java.util.List;
import java.util.UUID;
import lombok.AllArgsConstructor;
import lombok.Data;

@Data
@AllArgsConstructor
public class UserInfoResponse {
    private UUID id;
    private String username;
    private List<String> roles;
}
