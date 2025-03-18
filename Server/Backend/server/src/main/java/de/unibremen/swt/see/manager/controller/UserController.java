package de.unibremen.swt.see.manager.controller;

import de.unibremen.swt.see.manager.controller.request.ChangePasswordRequest;
import de.unibremen.swt.see.manager.controller.request.ChangeUsernameRequest;
import de.unibremen.swt.see.manager.controller.request.LoginRequest;
import de.unibremen.swt.see.manager.controller.request.SignupRequest;
import de.unibremen.swt.see.manager.controller.response.MessageResponse;
import de.unibremen.swt.see.manager.model.RoleType;
import de.unibremen.swt.see.manager.model.User;
import de.unibremen.swt.see.manager.security.JwtUtils;
import de.unibremen.swt.see.manager.security.UserDetailsImpl;
import de.unibremen.swt.see.manager.service.UserService;
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

/**
 * Handles HTTP requests for the /user endpoint.
 * <p>
 * This REST controller exposes various methods to perform CRUD operations on
 * user resources.
 */
@RestController
@RequestMapping("/api/v1/user")
@RequiredArgsConstructor
@Slf4j
public class UserController {

    /**
     * Handle user-related operations and business logic.
     */
    private final UserService userService;

    /**
     * Manages authentication tasks.
     */
    private final AuthenticationManager authenticationManager;

    /**
     * Provides JWT utilities.
     */
    private final JwtUtils jwtUtils;

    /**
     * Retrieves user metadata of the authenticated user.
     *
     * @param userDetails are injected by Spring Security framework
     * @return {@code 200 OK} with the user metadata object as payload,
     *         or {@code 401 Unauthorized} if access cannot be granted.
     */
    @GetMapping("/me")
    @PreAuthorize("hasRole('USER') or hasRole('ADMIN')")
    public ResponseEntity<?> getUser(@AuthenticationPrincipal UserDetails userDetails) {
        return ResponseEntity.ok().body(userService.getByUsername(userDetails.getUsername()));
    }

    /**
     * Retrieves the metadata of all available user resources.
     *
     * @return {@code 200 OK} with the user metadata list as payload,
     *         or {@code 401 Unauthorized} if access cannot be granted.
     */
    @GetMapping("/all")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> getUsers() {
        return ResponseEntity.ok().body(userService.getAll());
    }

    /**
     * Creates a new user.
     *
     * @param signupRequest metadata object to create new user instance
     * @return {@code 200 OK} with the user metadata object as payload, or
     * {@code 400 Bad Request} if a user with given name already exists or role
     * could not be assigned, or {@code 401 Unauthorized} if access cannot be
     * granted.
     * @see de.unibremen.swt.see.manager.controller.request.SignupRequest
     */
    @PostMapping("/create")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> createUser(@RequestBody SignupRequest signupRequest) {
        User user = userService.create(signupRequest.getUsername(), signupRequest.getPassword(), signupRequest.getRole());

        if (user == null) {
            return ResponseEntity.badRequest().build();
        }

        return ResponseEntity.ok().body(user);
    }

    /**
     * Adds a role to an existing user.
     *
     * @param username name of the existing user
     * @param role     new role to be added
     * @return {@code 200 OK} with the updated user metadata object as payload,
     * or {@code 400 Bad Request} if a user with given name could not be found,
     * or {@code 401 Unauthorized} if access cannot be granted.
     */
    @PostMapping("/addRoleToUser")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> addRoleToUser(@RequestParam String username,
            @RequestParam RoleType role) {
        User user = userService.addRole(username, role);

        if (user == null) {
            return ResponseEntity.badRequest().build();
        }

        return ResponseEntity.ok().body(user);
    }

    /**
     * Removes a role from an existing user.
     *
     * @param username name of the existing user
     * @param role     role to be removed
     * @return {@code 200 OK} with the updated user metadata object as payload,
     *         or {@code 401 Unauthorized} if access cannot be granted.
     */
    @DeleteMapping("/removeRoleFromUser")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> removeRoleToUSer(@RequestParam String username,
                                              @RequestParam RoleType role) {
        return ResponseEntity.ok().body(userService.removeRole(username, role));
    }

    /**
     * Deletes a user.
     *
     * @param username name of the user to delete
     * @return {@code 200 OK},
     *         or {@code 401 Unauthorized} if access cannot be granted.
     */
    @DeleteMapping("/delete")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> deleteUser(@RequestParam String username) {
        userService.deleteByUsername(username);
        return ResponseEntity.ok().build();
    }

    /**
     * Changes the name of the authenticated user.
     * <p>
     * Users can only change their own name.
     * <p>
     * Currently, only admins are allowed to change their usernames as users are
     * automatically created along with the server.
     *
     * @param oldUserDetails        are injected by Spring Security framework
     * @param changeUsernameRequest request data containing new username
     * @return {@code 200 OK} with the updated user metadata object as payload
     *         and new cookie with updated authentication token,
     *         or {@code 400 Bad Request} if user name could not be changed,
     *         or {@code 401 Unauthorized} if access cannot be granted.
     * @see de.unibremen.swt.see.manager.controller.request.ChangeUsernameRequest
     */
    @PutMapping("/changeUsername")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> changeUsername(
            @AuthenticationPrincipal UserDetails oldUserDetails,
            @RequestBody ChangeUsernameRequest changeUsernameRequest) {
        User newUser = userService.changeUsername(oldUserDetails.getUsername(), changeUsernameRequest.getNewUsername(), changeUsernameRequest.getPassword());
        
        if (newUser == null) {
            return ResponseEntity.badRequest().build();
        }

        Authentication authentication = authenticationManager.authenticate(new UsernamePasswordAuthenticationToken(changeUsernameRequest.getNewUsername(), changeUsernameRequest.getPassword()));
        SecurityContextHolder.getContext().setAuthentication(authentication);
        UserDetailsImpl userDetails = (UserDetailsImpl) authentication.getPrincipal();
        ResponseCookie jwtCookie = jwtUtils.generateJwtCookie(userDetails);

        return ResponseEntity.ok()
                .header(HttpHeaders.SET_COOKIE, jwtCookie.toString())
                .body(newUser);
    }

    /**
     * Changes the name of the authenticated user.
     * <p>
     * Users can only change their own password.
     * <p>
     * Currently, only admins are allowed to change their passwords as users are
     * automatically created along with the server.
     *
     * @param userDetails           are injected by Spring Security framework
     * @param changePasswordRequest request data containing new password
     * @return {@code 200 OK},
     *         or {@code 400 Bad Request} if password could not be changed,
     *         or {@code 401 Unauthorized} if access cannot be granted.
     * @see de.unibremen.swt.see.manager.controller.request.ChangePasswordRequest
     */
    @PutMapping("/changePassword")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<?> changePassword(
            @AuthenticationPrincipal UserDetails userDetails,
            @RequestBody ChangePasswordRequest changePasswordRequest) {
        if (userService.changePassword(userDetails.getUsername(), changePasswordRequest.getOldPassword(), changePasswordRequest.getNewPassword()))
            return ResponseEntity.ok().build();

        return ResponseEntity.badRequest().build();
    }

    /**
     * Sign in to the app.
     *
     * @param loginRequest login metadata object
     * @return {@code 200 OK} with the logged-in user metadata object as payload
     *         and cookie with an authentication token,
     *         or {@code 401 Unauthorized} if access cannot be granted.
     * @see de.unibremen.swt.see.manager.controller.request.LoginRequest
     */
    @PostMapping("/signin")
    public ResponseEntity<?> authenticateUser(@Valid @RequestBody LoginRequest loginRequest) {
        Authentication authentication = authenticationManager.authenticate(new UsernamePasswordAuthenticationToken(loginRequest.getUsername(), loginRequest.getPassword()));
        SecurityContextHolder.getContext().setAuthentication(authentication);
        UserDetailsImpl userDetails = (UserDetailsImpl) authentication.getPrincipal();
        ResponseCookie jwtCookie = jwtUtils.generateJwtCookie(userDetails);

        return ResponseEntity.ok()
                .header(HttpHeaders.SET_COOKIE, jwtCookie.toString())
                .body(userService.getByUsername(userDetails.getUsername()));
    }

    /**
     * Sign out off the app.
     * <p>
     * Currently, this only clears the cookie (if client complies).
     * <p>
     * <b>This does not invalidate the authentication token on server side!</b>
     *
     * @return {@code 200 OK} with a cookie to clear token on client,
     *         or {@code 401 Unauthorized} if access cannot be granted.
     * @see de.unibremen.swt.see.manager.controller.request.LoginRequest
     */
    @PostMapping("/signout")
    public ResponseEntity<?> logoutUser() {
        // FIXME This does not invalidate the token on server side!
        ResponseCookie cookie = jwtUtils.getCleanJwtCookie();
        return ResponseEntity.ok()
                .header(HttpHeaders.SET_COOKIE, cookie.toString())
                .body(new MessageResponse("You've been signed out!"));
    }
}
