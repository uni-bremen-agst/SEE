package de.unibremen.swt.see.manager.security;

import de.unibremen.swt.see.manager.model.User;
import de.unibremen.swt.see.manager.repository.UserRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.security.core.userdetails.UserDetailsService;
import org.springframework.security.core.userdetails.UsernameNotFoundException;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

/**
 * Implementation of {@link UserDetailsService} for loading user details.
 * <p>
 * This class is responsible for loading user details from the database and
 * mapping them to Spring Security's {@link UserDetails} interface.
 */
@Service
public class UserDetailsServiceImpl implements UserDetailsService {

    /**
     * Repository for accessing user data.
     */
    @Autowired
    UserRepository userRepository;

    /**
     * Loads a user by their username.
     * <p>
     * This method retrieves the user details from the database based on the
     * provided username. It maps the user data to {@link UserDetailsImpl},
     * which is then returned to Spring Security for authentication.
     *
     * @param username the username of the user to load
     * @return the loaded {@link UserDetails} instance
     * @throws UsernameNotFoundException if the user is not found
     */
    @Override
    @Transactional
    public UserDetails loadUserByUsername(String username) throws UsernameNotFoundException {
        User user = userRepository.findByUsername(username)
                .orElseThrow(() -> new UsernameNotFoundException("User Not Found with username: " + username));

        return UserDetailsImpl.build(user);
    }

}
