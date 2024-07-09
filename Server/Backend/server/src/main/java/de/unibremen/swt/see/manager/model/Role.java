package de.unibremen.swt.see.manager.model;

import jakarta.persistence.*;

/**
 * Represents the data model of the different roles a user can assume.
 * <p>
 * Possible values are defined in related {@code enum} {@link RoleType}.
 */
@Entity
@Table(name = "roles",
        uniqueConstraints = {
            @UniqueConstraint(columnNames = {"name"})
        })
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
     * The name is persisted as a {@code String} converted from a value of
     * {@code enum} {@link RoleType}.
     */
    @Enumerated(EnumType.STRING)
    @Column(name = "name", length = 20, unique = true)
    private RoleType name;

    /**
     * Constructs an empty {@code Role}.
     */
    public Role() {}

    /**
     * Constructs a {@code Role} from an {@code enum} value.
     * 
     * @param name {@code enum} value of the role
     */
    public Role(RoleType name) {
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
    public RoleType getName() {
        return name;
    }

    /**
     * @param name new role name
     */
    public void setName(RoleType name) {
        this.name = name;
    }
}