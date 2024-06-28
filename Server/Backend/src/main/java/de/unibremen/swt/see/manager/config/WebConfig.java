package de.unibremen.swt.see.manager.config;

import java.net.MalformedURLException;
import java.net.URL;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.servlet.config.annotation.CorsRegistry;
import org.springframework.web.servlet.config.annotation.EnableWebMvc;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;


@Configuration
@EnableWebMvc
@Slf4j
public class WebConfig {

    @Value("${see.app.frontend.domain}")
    private String frontendDomain;
    
    @Value("${see.app.frontend.scheme}")
    private String frontendScheme;

    @Bean
    public WebMvcConfigurer getCorsConfiguration() {
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