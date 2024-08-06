package de.unibremen.swt.see.manager.security;

/**
 *  This {@code enum} represents errors during user validation.
 */
public enum ValidationError {
    /**
     * JWT has expired.
     */
    TOKEN_EXPIRED,
    
    /**
     * JWT is not a signed claims token.
     */
    TOKEN_UNSUPPORTED,
    
    /**
     * Token is not a valid JWS.
     */
    TOKEN_MALFORMED,
    
    /**
     * Token signature validation failed.
     */
    TOKEN_SIGNATURE_INVALID,
    
    /**
     * Token is null or blank.
     */
    TOKEN_EMPTY,
}
