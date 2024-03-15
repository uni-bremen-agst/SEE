package uni.bachelorprojekt.see.controller.user.payload.request;

import lombok.Data;

@Data
public class ChangeUsernameRequest {
    private String newUsername;
    private String password;
}
