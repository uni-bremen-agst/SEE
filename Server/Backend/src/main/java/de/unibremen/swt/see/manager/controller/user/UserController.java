package de.unibremen.swt.see.manager.controller.user;

import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpHeaders;
import org.springframework.http.ResponseCookie;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.web.bind.annotation.*;
import de.unibremen.swt.see.manager.controller.user.payload.request.ChangePasswordRequest;
import de.unibremen.swt.see.manager.controller.user.payload.request.ChangeUsernameRequest;
import de.unibremen.swt.see.manager.controller.user.payload.request.LoginRequest;
import de.unibremen.swt.see.manager.controller.user.payload.request.SignupRequest;
import de.unibremen.swt.see.manager.controller.user.payload.response.MessageResponse;
import de.unibremen.swt.see.manager.model.ERole;
import de.unibremen.swt.see.manager.security.jwt.JwtUtils;
import de.unibremen.swt.see.manager.security.services.UserDetailsImpl;
import de.unibremen.swt.see.manager.service.UserService;

@RestController
@RequestMapping("/user")
@RequiredArgsConstructor
@Slf4j
public class UserController {

    private final UserService userService;
    private final AuthenticationManager authenticationManager;
    private final JwtUtils jwtUtils;

    @GetMapping("/me")
    @PreAuthorize("hasRole('USER') or hasRole('ADMIN')")
    public ResponseEntity<?> getUser(@AuthenticationPrincipal UserDetails userDetails) {
        return ResponseEntity.ok().body(userService.getUserByUsername(userDetails.getUsername()));
    }

    @GetMapping("/all")
    @PreAuthorize("hasRole('USER') or hasRole('ADMIN')")
    public ResponseEntity<?> getUsers() {
        return ResponseEntity.ok().body(userService.getAllUser());
    }

    @PostMapping("/create")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> createUser(@RequestBody SignupRequest signupRequest) {
        return ResponseEntity.ok().body(userService.create(signupRequest.getUsername(), signupRequest.getPassword(), signupRequest.getRole()));
    }

    @PostMapping("/addRoleToUser")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> addRoleToUser(@RequestParam String username,
                                           @RequestParam ERole role) {
        return ResponseEntity.ok().body(userService.addRoleToUser(username, role));
    }

    @DeleteMapping("/removeRoleFromUser")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> removeRoleToUSer(@RequestParam String username,
                                              @RequestParam ERole role) {
        return ResponseEntity.ok().body(userService.removeRoleToUser(username, role));
    }

    @DeleteMapping("/delete")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> deleteUser(@RequestParam String username) {
        userService.deleteUserByUsername(username);
        return ResponseEntity.ok().build();
    }

    @PutMapping("/changeUsername")
    @PreAuthorize("hasRole('USER') or hasRole('ADMIN')")
    public ResponseEntity<?> changeUsername(@AuthenticationPrincipal UserDetails oldUserDetails,
                                            @RequestBody ChangeUsernameRequest changeUsernameRequest) {
        userService.changeUsername(oldUserDetails.getUsername(), changeUsernameRequest.getNewUsername(), changeUsernameRequest.getPassword());


        Authentication authentication = authenticationManager.authenticate(new UsernamePasswordAuthenticationToken(changeUsernameRequest.getNewUsername(), changeUsernameRequest.getPassword()));
        SecurityContextHolder.getContext().setAuthentication(authentication);
        UserDetailsImpl userDetails = (UserDetailsImpl) authentication.getPrincipal();
        ResponseCookie jwtCookie = jwtUtils.generateJwtCookie(userDetails);

        return ResponseEntity.ok().header(HttpHeaders.SET_COOKIE, jwtCookie.toString())
                .body(userService.getUserByUsername(userDetails.getUsername()));
    }

    @PutMapping("/changePassword")
    @PreAuthorize("hasRole('USER') or hasRole('ADMIN')")
    public ResponseEntity<?> changePassword(@AuthenticationPrincipal UserDetails userDetails,
                                            @RequestBody ChangePasswordRequest changePasswordRequest) {

        if (userService.changePassword(userDetails.getUsername(), changePasswordRequest.getOldPassword(), changePasswordRequest.getNewPassword()))
            return ResponseEntity.ok().build();

        return ResponseEntity.badRequest().build();
    }

    @PostMapping("/signin")
    public ResponseEntity<?> authenticateUser(@Valid @RequestBody LoginRequest loginRequest) {
        Authentication authentication = authenticationManager.authenticate(new UsernamePasswordAuthenticationToken(loginRequest.getUsername(), loginRequest.getPassword()));
        SecurityContextHolder.getContext().setAuthentication(authentication);
        UserDetailsImpl userDetails = (UserDetailsImpl) authentication.getPrincipal();
        ResponseCookie jwtCookie = jwtUtils.generateJwtCookie(userDetails);

        return ResponseEntity.ok().header(HttpHeaders.SET_COOKIE, jwtCookie.toString())
                .body(userService.getUserByUsername(userDetails.getUsername()));
    }

    @PostMapping("/signout")
    public ResponseEntity<?> logoutUser() {
        ResponseCookie cookie = jwtUtils.getCleanJwtCookie();
        return ResponseEntity.ok().header(HttpHeaders.SET_COOKIE, cookie.toString())
                .body(new MessageResponse("You've been signed out!"));
    }
}
