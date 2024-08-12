package de.unibremen.swt.see.manager.security;

import com.fasterxml.jackson.databind.ObjectMapper;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.util.HashMap;
import java.util.Map;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.MediaType;
import org.springframework.security.core.AuthenticationException;
import org.springframework.security.web.AuthenticationEntryPoint;
import org.springframework.stereotype.Component;

/**
 * Custom Authentication Entry Point for JWT-based authentication.
 * <p>
 * This class implements {@link AuthenticationEntryPoint} to handle
 * authentication exceptions that occur during the JWT authentication process.
 * It is responsible for commencing the authentication scheme and sending the
 * appropriate error response when an unauthenticated user attempts to access a
 * protected resource.
 * <p>
 * Key features:
 * <ul>
 * <li>Handles unauthorized access attempts in a JWT-secured application</li>
 * <li>Sends a 401 Unauthorized response to the client</li>
 * <li>Logs the unauthorized error for debugging and monitoring purposes</li>
 * </ul>
 * <p>
 * This entry point is used in conjunction with other JWT-related components
 * such as {@code JwtTokenFilter} and {@code JwtUtils} in the Spring Security
 * configuration.
 *
 * @see org.springframework.security.web.AuthenticationEntryPoint
 * @see org.springframework.security.core.AuthenticationException
 */
@Component
@Slf4j
public class AuthEntryPointJwt implements AuthenticationEntryPoint {

    /**
     * Commences the authentication scheme.
     * <p>
     * This method is called when an authentication exception occurs, such as
     * when an unauthenticated client attempts to access a protected resource.
     * It sends a {@code 401 Unauthorized} response to the client with a custom
     * error message.
     * <p>
     * The method logs the unauthorized error for debugging and monitoring
     * purposes.
     *
     * @param request the HTTP servlet request
     * @param response the HTTP servlet response
     * @param authException the authentication exception that occurred
     * @throws IOException if an I/O error occurs during the response writing
     * @throws ServletException if an exception occurs that interferes with the
     * filter's normal operation
     */
    @Override
    public void commence(HttpServletRequest request, HttpServletResponse response, AuthenticationException authException)
            throws IOException, ServletException {

        response.setContentType(MediaType.APPLICATION_JSON_VALUE);
        response.setStatus(HttpServletResponse.SC_UNAUTHORIZED);

        String message = authException.getMessage();
        if (request.getAttribute(ValidationError.TOKEN_EXPIRED.toString()) != null) {
            message = "Token has expired!";
        } else if (request.getAttribute(ValidationError.TOKEN_UNSUPPORTED.toString()) != null) {
            message = "Token is unsupported!";
        } else if (request.getAttribute(ValidationError.TOKEN_MALFORMED.toString()) != null) {
            message = "Token is malformed!";
        } else if (request.getAttribute(ValidationError.TOKEN_SIGNATURE_INVALID.toString()) != null) {
            message = "Token signature is invalid!";
        } else if (request.getAttribute(ValidationError.TOKEN_EMPTY.toString()) != null) {
            message = "Token is empty!";
        }

        log.warn("Unauthorized error: {}", message);

        final Map<String, Object> body = new HashMap<>();
        body.put("status", HttpServletResponse.SC_UNAUTHORIZED);
        body.put("error", "Unauthorized");
        body.put("message", message);
        body.put("path", request.getServletPath());

        final ObjectMapper mapper = new ObjectMapper();
        mapper.writeValue(response.getOutputStream(), body);
    }
}
