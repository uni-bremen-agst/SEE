package de.unibremen.swt.see.manager.model;

import jakarta.persistence.*;

/**
 * Represents the data model of the different roles a user can assume.
 * <p>
 * This {@code enum} defines the roles that are used in the role-based access 
 * model implemented with the Spring Security framework.
 * 
 * @see de.unibremen.swt.see.manager.model.ERole
 */
@Entity
@Table(name = "roles")
public class Role {
    /**
     * ID of the role.
     */
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Integer id;

    /**
     * Name of the role.
     * <p>
     * The name is converted from the {@code enum} value.
     * 
     * @see de.unibremen.swt.see.manager.model.ERole
     */
    @Enumerated(EnumType.STRING)
    @Column(length = 20, unique = true)
    private ERole name;

    /**
     * Constructs an empty {@code Role}.
     */
    public Role() {}

    /**
     * Constructs a {@code Role} from an {@code enum} value.
     * 
     * @param name {@code enum} value of the role
     */
    public Role(ERole name) {
        this.name = name;
    }

    /**
     * @return  role ID
     */
    public Integer getId() {
        return id;
    }

    /**
     * @param id new ID
     */
    public void setId(Integer id) {
        this.id = id;
    }

    /**
     * @return  role name
     */
    public ERole getName() {
        return name;
    }

    /**
     * @param name new role name
     */
    public void setName(ERole name) {
        this.name = name;
    }
}