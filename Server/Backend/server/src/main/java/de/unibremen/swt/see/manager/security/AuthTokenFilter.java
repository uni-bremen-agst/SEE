package de.unibremen.swt.see.manager.security;

import io.jsonwebtoken.ExpiredJwtException;
import io.jsonwebtoken.MalformedJwtException;
import io.jsonwebtoken.UnsupportedJwtException;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.security.core.userdetails.UsernameNotFoundException;
import org.springframework.security.web.authentication.WebAuthenticationDetailsSource;
import org.springframework.web.filter.OncePerRequestFilter;

/**
 * JWT Authentication Token Filter for processing JWT tokens in incoming
 * requests.
 * <p>
 * This filter extends {@link OncePerRequestFilter} to ensure it is executed
 * only once per request. It is responsible for validating the JWT token present
 * in the request header, extracting user details, and setting up the
 * authentication context for the current request.
 * <p>
 * The filter performs the following key operations:
 *
 * <ul>
 * <li>Extracts the JWT token from the Authorization header</li>
 * <li>Validates the token using {@link JwtUtils}</li>
 * <li>Loads user details based on the username extracted from the token</li>
 * <li>Sets up the Spring Security context if authentication is successful</li>
 * </ul>
 * <p>
 * This filter is added to the Spring Security filter chain in the
 * {@link de.unibremen.swt.see.manager.config.WebSecurityConfig}.
 */
@Slf4j
public class AuthTokenFilter extends OncePerRequestFilter {

    /**
     * Utility class for JWT operations such as token generation and validation.
     */
    @Autowired
    private JwtUtils jwtUtils;

    /**
     * Service for loading user-specific data during authentication.
     */
    @Autowired
    private UserDetailsServiceImpl userDetailsService;

    /**
     * Performs the core filter processing for each request.
     * <p>
     * This method is called by the filter framework for each incoming HTTP
     * request. It attempts to extract and validate the JWT token from the
     * request header, load the associated user details, and set up the security
     * context.
     * <p>
     * If any step fails, the filter chain continues without setting up
     * authentication.
     *
     * @param request the HTTP servlet request
     * @param response the HTTP servlet response
     * @param filterChain the filter chain for executing the next filter
     * @throws ServletException if an exception occurs that interferes with the
     * filter's normal operation
     * @throws IOException if an I/O error occurs during the processing
     */
    @Override
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain filterChain)
            throws ServletException, IOException {
        final String jwt = jwtUtils.getJwtFromCookies(request);
        boolean authorized = false;
        try {
            jwtUtils.validateJwtToken(jwt);
            authorized = true;
        } catch (ExpiredJwtException e) {
            log.debug("JWT token is expired: {}", e.getMessage());
            request.setAttribute(ValidationError.TOKEN_EXPIRED.toString(), e.getMessage());
        } catch (UnsupportedJwtException e) {
            log.debug("JWT token is invalid: {}", e.getMessage());
            request.setAttribute(ValidationError.TOKEN_UNSUPPORTED.toString(), e.getMessage());
        } catch (MalformedJwtException e) {
            log.debug("JWT token is malformed: {}", e.getMessage());
            request.setAttribute(ValidationError.TOKEN_MALFORMED.toString(), e.getMessage());
        } catch (SecurityException e) {
            log.debug("JWT token signature invalid: {}", e.getMessage());
            request.setAttribute(ValidationError.TOKEN_SIGNATURE_INVALID.toString(), e.getMessage());
        } catch (IllegalArgumentException e) {
            log.debug("JWT token is empty: {}", e.getMessage());
            request.setAttribute(ValidationError.TOKEN_EMPTY.toString(), e.getMessage());
        }

        if (authorized) {
            final String username = jwtUtils.getUserNameFromJwtToken(jwt);

            try {
                UserDetails userDetails = userDetailsService.loadUserByUsername(username);

                UsernamePasswordAuthenticationToken authentication
                        = new UsernamePasswordAuthenticationToken(userDetails,
                                null,
                                userDetails.getAuthorities());

                authentication.setDetails(new WebAuthenticationDetailsSource().buildDetails(request));

                SecurityContextHolder.getContext().setAuthentication(authentication);
            } catch (UsernameNotFoundException e) {
                // User has a valid token but username does not exist in database.
                log.warn("Authenticated user not found in db: {}", username);
            }
        }

        filterChain.doFilter(request, response);
    }

}
