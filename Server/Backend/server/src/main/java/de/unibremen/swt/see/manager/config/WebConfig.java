package de.unibremen.swt.see.manager.config;

import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.servlet.config.annotation.CorsRegistry;
import org.springframework.web.servlet.config.annotation.EnableWebMvc;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;

import java.net.MalformedURLException;
import java.net.URL;

/**
 * Web configuration class for the SEE Manager back-end application.
 * <p>
 * This class configures web-related components for SEE Manager.
 */
@Configuration
@EnableWebMvc
@Slf4j
public class WebConfig implements WebMvcConfigurer{

    /**
     * Contains the domain name, or IP address, and port of the front-end
     * application.
     * <p>
     * The value is configured in the application properties and gets injected
     * during class initialization.
     */
    @Value("${see.app.frontend.domain}")
    private String frontendDomain;

    /**
     * Contains the URL scheme of the front-end application.
     * <p>
     * The value is configured in the application properties and gets injected
     * during class initialization.
     */
    @Value("${see.app.frontend.scheme}")
    private String frontendScheme;

    /**
     * Creates and configures a {@code WebMvcConfigurer} for Cross-Origin
     * Resource Sharing (CORS) settings.
     * <p>
     * This method sets up CORS configuration for the application, allowing
     * controlled access to resources from different origins. It defines which
     * origins, HTTP methods, and headers are allowed in cross-origin
     * requests.
     * <p>
     * This configuration is applied globally to all endpoints in the
     * application.
     *
     * @return A {@link WebMvcConfigurer} instance with the CORS configuration
     * @see org.springframework.web.servlet.config.annotation.WebMvcConfigurer
     * @see org.springframework.web.servlet.config.annotation.CorsRegistry
     */
    @Bean
    public WebMvcConfigurer createCorsConfiguration() {
        return new WebMvcConfigurer() {
            @Override
            public void addCorsMappings(CorsRegistry registry) {
                if (frontendDomain == null || frontendDomain.isBlank()) {
                    throw new IllegalStateException("Frontend domain must not be empty!");
                }
                
                String frontendUrl;
                try {
                    // Note: Does only minimal verification
                    frontendUrl = new URL(frontendScheme + "://" + frontendDomain).toString();
                } catch (MalformedURLException e) {
                    throw new RuntimeException("Frontend domain and/or scheme is not properly configured!", e);
                }
                log.info("Using frontend URL: {}", frontendUrl);
                
                registry.addMapping("/**")
                        .allowedOrigins(frontendUrl)
                        .allowedMethods("*")
                        .allowCredentials(true)
                        .allowedHeaders("*");
            }
        };
    }
}