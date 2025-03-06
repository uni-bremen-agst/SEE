package de.unibremen.swt.see.manager.config;

import de.unibremen.swt.see.manager.security.AuthEntryPointJwt;
import de.unibremen.swt.see.manager.security.AuthTokenFilter;
import de.unibremen.swt.see.manager.security.UserDetailsServiceImpl;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.dao.DaoAuthenticationProvider;
import org.springframework.security.config.Customizer;
import org.springframework.security.config.annotation.authentication.configuration.AuthenticationConfiguration;
import org.springframework.security.config.annotation.method.configuration.EnableMethodSecurity;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.config.annotation.web.configurers.AbstractHttpConfigurer;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;

/**
 * Configuration class for web security in the application.
 * <p>
 * This class is responsible for setting up the security configuration for the
 * web application. It defines beans for authentication, password encoding, and
 * security filter chain configuration.
 */
@Configuration
@EnableWebSecurity
@EnableMethodSecurity
//(securedEnabled = true,
//jsr250Enabled = true,
//prePostEnabled = true) // by default
public class WebSecurityConfig {

    /**
     * Service for loading user details.
     */
    @Autowired
    UserDetailsServiceImpl userDetailsService;

    /**
     * Entry point for handling unauthorized access.
     */
    @Autowired
    private AuthEntryPointJwt unauthorizedHandler;

    /**
     * Creates an {@link AuthTokenFilter} bean.
     * <p>
     * This method creates an instance of {@link AuthTokenFilter} and exposes it
     * as a Spring bean for use in the security configuration.
     *
     * @return the {@link AuthTokenFilter} instance
     */
    @Bean
    public AuthTokenFilter authenticationJwtTokenFilter() {
        return new AuthTokenFilter();
    }

    /**
     * Configures the authentication provider.
     * <p>
     * This method sets up a {@link DaoAuthenticationProvider} which uses a
     * custom {@code UserDetailsService} for retrieving user details and the
     * application's password encoder for password validation.
     *
     * @return the configured {@link DaoAuthenticationProvider}
     */
    @Bean
    public DaoAuthenticationProvider authenticationProvider() {
        DaoAuthenticationProvider authProvider = new DaoAuthenticationProvider();

        authProvider.setUserDetailsService(userDetailsService);
        authProvider.setPasswordEncoder(passwordEncoder());

        return authProvider;
    }

    /**
     * Configures the authentication manager.
     * <p>
     * This method creates an {@link AuthenticationManager} using the provided
     * AuthenticationConfiguration.
     *
     * @param authConfig the {@link AuthenticationConfiguration} to use
     * @return the configured {@link AuthenticationManager}
     * @throws Exception if an error occurs during the configuration
     */
    @Bean
    public AuthenticationManager authenticationManager(AuthenticationConfiguration authConfig) throws Exception {
        return authConfig.getAuthenticationManager();
    }

    /**
     * Defines the password encoder for the application.
     * <p>
     * This method creates a {@link BCryptPasswordEncoder} for secure password
     * hashing. It currently uses BCrypt version {@code $2b} (which is newer
     * than {@code $2a} and {@code $2x}/{@code $2y}) and a strength of 12.
     *
     * @return The {@link PasswordEncoder} to be used for password hashing
     */
    @Bean
    public PasswordEncoder passwordEncoder() {
        return new BCryptPasswordEncoder(BCryptPasswordEncoder.BCryptVersion.$2B, 12);
    }

    /**
     * Configures the security filter chain for HTTP requests.
     * <p>
     * This method sets up the security rules for different URL patterns,
     * configures form login, logout handling, and session management.
     *
     * @param http The {@link HttpSecurity} to modify
     * @return The built {@link SecurityFilterChain}
     * @throws Exception if there's an error configuring the HttpSecurity
     */
    @Bean
    public SecurityFilterChain filterChain(HttpSecurity http) throws Exception {
        http.cors(Customizer.withDefaults())
                .csrf(AbstractHttpConfigurer::disable)
                .exceptionHandling(exception -> exception.authenticationEntryPoint(unauthorizedHandler))
                .sessionManagement(session -> session.sessionCreationPolicy(SessionCreationPolicy.STATELESS))
                .authorizeHttpRequests(auth -> auth.requestMatchers("/user/signin").permitAll().anyRequest().authenticated());
        // fix H2 database console: Refused to display ' in a frame because it set 'X-Frame-Options' to 'deny'
        //http.headers(headers -> headers.frameOptions(HeadersConfigurer.FrameOptionsConfig::sameOrigin));

        http.authenticationProvider(authenticationProvider());

        http.addFilterBefore(authenticationJwtTokenFilter(), UsernamePasswordAuthenticationFilter.class);

        return http.build();
    }
}
