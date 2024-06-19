package de.unibremen.swt.see.manager.security;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.servlet.config.annotation.CorsRegistry;
import org.springframework.web.servlet.config.annotation.EnableWebMvc;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;

/**
 * Copyright 2021 LFS-Software UG, all rights reserved.
 *
 * @author Thorsten Friedewold (friedewold@lfs-software.de)
 */


@Configuration
@EnableWebMvc
public class WebConfig {

    @Value("${see.app.domain}")
    private String domain;

    @Value("${see.app.domain.port}")
    private String port;

    @Bean
    public WebMvcConfigurer getCorsConfiguration() {
        return new WebMvcConfigurer() {
            @Override
            public void addCorsMappings(CorsRegistry registry) {
                registry.addMapping("/**")
                        .allowedOrigins("http://" + domain)
                        .allowedMethods("*")
                        .allowCredentials(true)
                        .allowedHeaders("*");
            }
        };
    }
}