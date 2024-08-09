package de.unibremen.swt.see.manager.security;

import io.jsonwebtoken.*;
import io.jsonwebtoken.io.Decoders;
import io.jsonwebtoken.security.Keys;
import io.jsonwebtoken.security.WeakKeyException;
import jakarta.servlet.http.Cookie;
import jakarta.servlet.http.HttpServletRequest;
import java.security.Key;
import java.util.Date;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.web.server.Cookie.SameSite;
import org.springframework.http.ResponseCookie;
import org.springframework.stereotype.Component;
import org.springframework.web.util.WebUtils;

/**
 * Utility class for generating, parsing, and validating JSON Web Tokens (JWT).
 */
@Component
@Slf4j
public class JwtUtils {

    /**
     * Expiration time of JWT in milliseconds.
     */
    @Value("${see.app.jwtExpirationMs}")
    private int jwtExpirationMs;

    /**
     * Name of the cookie used for the JWT.
     */
    @Value("${see.app.jwtCookieName}")
    private String jwtCookie;

    /**
     * Contains the domain name, or IP address, and port of this back-end
     * application server.
     */
    @Value("${see.app.backend.domain}")
    private String backendDomain;

    /**
     * Context path of the application's servlet container.
     */
    @Value("${server.servlet.context-path}")
    private String contextPath;

    /**
     * The secret key used for signing and verifying JWT.
     */
    private final Key key;

    /**
     * The parser used for JWT related operations.
     */
    private final JwtParser jwtParser;

    /**
     * Constructs a new {@code JwtUtils} instance and generates the secret key.
     * <p>
     * The secret key is constructed from the configured JWT secret using
     * HMAC-SHA algorithms.
     *
     * @param jwtSecret secret used for constructing the key
     * @throws WeakKeyException if the key is too short
     * @see io.jsonwebtoken.security.Keys#hmacShaKeyFor(byte[])
     */
    public JwtUtils(@Value("${see.app.jwtSecret}") String jwtSecret) throws WeakKeyException {
        byte[] keyBytes = Decoders.BASE64.decode(jwtSecret);
        this.key = Keys.hmacShaKeyFor(keyBytes);
        this.jwtParser = Jwts.parserBuilder().setSigningKey(key).build();
    }

    /**
     * Extracts the JWT from the cookies of the given request.
     *
     * @param request the HTTP servlet request
     * @return the extracted JWT
     */
    public String getJwtFromCookies(HttpServletRequest request) {
        Cookie cookie = WebUtils.getCookie(request, jwtCookie);
        if (cookie != null) {
            return cookie.getValue();
        } else {
            return null;
        }
    }

    /**
     * Generates a JWT for the given user principal.
     * <p>
     * This method creates a new JWT and encodes it into a
     * {@link ResponseCookie} object. The cookie is configured with the JWT
     * value, an expiration time based on the configured JWT expiration
     * milliseconds, and the HttpOnly and Secure flags.
     *
     * @param userPrincipal the user details to include in the JWT
     * @return the generated JWT cookie
     */
    public ResponseCookie generateJwtCookie(UserDetailsImpl userPrincipal) {
        String jwt = generateToken(userPrincipal.getUsername());
        ResponseCookie cookie = ResponseCookie
                .from(jwtCookie, jwt)
                .domain(backendDomain.split(":")[0])
                .path(contextPath)
                .maxAge(24 * 60 * 60)
                .httpOnly(true)
                .sameSite(SameSite.STRICT.attributeValue())
                .build();
        return cookie;
    }

    /**
     * Generates a clean JWT cookie for logout.
     * <p>
     * This method creates a new {@link ResponseCookie} with an empty value, an
     * expiration time of 0 seconds, and the HttpOnly and Secure flags. This can
     * be used to clear the JWT cookie when the user logs out.
     *
     * @return the clean JWT cookie
     */
    public ResponseCookie getCleanJwtCookie() {
        ResponseCookie cookie = ResponseCookie
                .from(jwtCookie, null)
                .path(contextPath)
                .build();
        return cookie;
    }

    /**
     * Parses a JWT and extracts the username from it.
     * <p>
     * This method takes a JWT as input, parses it to extract the claims, and
     * returns the username (subject) from the claims.
     *
     * @param token the JWT to parse
     * @return the username extracted from the JWT
     */
    public String getUserNameFromJwtToken(String token) {
        return jwtParser.parseClaimsJws(token).getBody().getSubject();
    }

    /**
     * Validates a JWT.
     * <p>
     * This method checks if the provided JWT is valid by parsing it and
     * verifying the signature using the configured secret key. It also checks
     * if the JWT has not expired.
     *
     * @param authToken the JWT to validate
     * @throws ExpiredJwtException if the token is expired
     * @throws UnsupportedJwtException if given token is not a signed claims
     * token
     * @throws MalformedJwtException if the token is malformed
     * @throws SecurityException if signature validation fails
     * @throws IllegalArgumentException if the token is null or blank
     */
    public void validateJwtToken(String authToken)
            throws ExpiredJwtException,
            UnsupportedJwtException,
            MalformedJwtException,
            SecurityException,
            IllegalArgumentException {
        jwtParser.parseClaimsJws(authToken);
    }

    /**
     * Generates a JWT for the given username.
     * <p>
     * This method creates a new JWT with the provided username as the subject.
     * The token is signed using the configured secret key and has an expiration
     * time based on the configured JWT expiration milliseconds.
     *
     * @param username The username to include in the JWT
     * @return The generated JWT as a string
     */
    public String generateToken(String username) {
        return Jwts.builder()
                .setSubject(username)
                .setIssuedAt(new Date())
                .setExpiration(new Date((new Date()).getTime() + jwtExpirationMs))
                .signWith(key, SignatureAlgorithm.HS256)
                .compact();
    }
}
