# SEE Managed Server Backend

This is the backend for running and managing SEE server instances.

--------------------------------------------------------------------------------
## Overview

The project provides a REST API based on Spring Boot with the following features:

- Manage users and roles (`UserController`, `UserService`)
- Manage SEE game servers (`ServerController`, `ServerService`)
  - Server instances are managed in containers (`ContainerService`)
- Manage file storage for shared Code Cities (`FileController`, `FileService`)

The REST API is defined in the controllers while business logic is implemented in the services.

Please read the JavaDoc documentation for additional details.

### User Service

Manages users and their passwords along with other metadata like role assignment.<br>
Data is stored in the relational database.

### Server Service

Manages metadata on SEE game server instances and controls them using `ContainerService`.<br>
Data is stored in the relational database.

### Container Service

Manages SEE game server instances that are launched via Docker/Podman containers.

### File Service

The file service allows for storing and retrieving files that are required to render Code Cities in SEE clients.<br>
Metadata to identify and retrieve files are stored in the relational database, while the actual files are stored in the backend-local file system.


--------------------------------------------------------------------------------
## Dependencies

The backend itself manages its dependencies using Maven.
However, there are additional dependencies to run the complete stack effectively.

### Maven

You can use the following command to check for outdated versions of direct dependencies:

```sh
./mvnw versions:display-dependency-updates
```

Please note that updating dependencies might require code changes.
It is usually safe -- and strongly suggested -- to bump patch level versions.
This is typically the third part of the version number, e.g., `1.2.3` -> `1.2.5`.
Changes to the first two segments are often associated to breaking changes and need additional manual review and testing.

The first part is referred to as *major*, the second is *minor* and the third as *incremental*.
You can use `-DallowMinorUpdates=false` to limit the report to *incremental* updates,
or `-DallowMajorUpdates=false` to include *minor* version updates as well.
Omitting these parameters will result in a complete report, including *major* versions and all below.

The version numbers are typically defined as properties in the respective `pom.xml` file.
This allows us also to use the following command:

```sh
./mvnw versions:display-property-updates
```

A quick way to upgrade version numbers is the following command:

```sh
./mvnw versions:update-properties
```

Use parameters like `-DallowMinorUpdates=false` as described above if desired.

Similarly to the dependency versions, the following command checks for Maven plugin updates -- updates for the build system:

```sh
./mvnw versions:display-plugin-updates
```

### Stack

This backend requires a container runtime to manage SEE game server instances.
Refer to the README in the parent directory for additional considerations.

The backend container image is configured in the `Dockerfile`.<br>
The complete stack, including back- and frontend, is configured vie `compose.yaml` in the parent directory.


--------------------------------------------------------------------------------
## Configuration

Default configuration is done via `application.properties` file in `src/main/resources/` directory during the development process.
The file will be integrated into the compiled server distribution (e.g., `jar` file).

Some of the values are defined in a way that allow being overridden by environment variables.
Consider the following example:

```
my.data.key=${VALUE_FROM_ENV:default_value}
```

The value for `my.data.key` is defined as `${VALUE_FROM_ENV:default_value}`.
This allows for overriding the default value `default_value` via the `$VALUE_FROM_ENV` environment variable.

Environment variables can be passed via command line, or defined in the Docker Compose file.


--------------------------------------------------------------------------------
## Development Environment

Useful information concerning the development environment are collected here.

### NetBeans

NetBeans seems to work pretty well, enjoy coding!

### Eclipse

Eclipse seems to have problems recognizing getters/setters generated using Lombok.
There was an attempt to fix this issue, but to no avail.
If you are using Eclipse and know how to fix it, please do so and update this section.

#### Lombok

**Heads up:** This did not make it work after all. Please update this section if you can resolve the issue!

Install Project Lombok integration to allow Eclipse to see auto-generated getter/setter functions.

Check the [official manual](https://projectlombok.org/setup/eclipse) on the project website.

The process should boil down to this:

1. Click *Help* > *Install New Software…*
2. Add the source (*Work with:*): `https://projectlombok.org/p2`
3. Select and install Lombok.
4. Quit and open Eclipse again. Don't use the restart option.


--------------------------------------------------------------------------------
## Security Considerations

There are a few things to keep in mind while using the app.
In this section you will find a non-exhaustive list of security considerations.

Please also read the parent README for additional information.


### Passwords

There are two types of passwords at the time of writing:

#### 1. Passwords for users registered in the backend application:

Passwords are **transmitted in plaintext** during registration, sign-in, password change, and similar operations.
After logging in to the backend (either using the frontend or via SEE client), a token (JWT) is transmitted to the client that is henceforth used for authentication of requests (see below).

User passwords are stored in the database as a salted bcrypt hashes, which is a state-of-the-art method to secure passwords at rest.

**However,** server passwords (also called *room passwords*) are stored in plaintext in the database. Read on…

#### 2. *Server* or *room passwords*:

These passwords are auto-generated whenever a server instance is created.
Along with the server data, a user is created with this password.
This user is associated with the server so that SEE clients can access the server-related data using the generated password.
During API requests, this password is handled exactly as a usual password for registered users (using JWT after log-in, etc.).
Although this is much less secure, these passwords are additionally kept in the database as plain text to allow admins controlling the management server to retrieve server passwords.

Rationale:
- Server instances are frequently created and destroyed, thus passwords are usually only used for a short time.
- Server passwords are only used on certain API end-points and do not grant full access to control the backend.
- Passwords are auto-generated: no user-defined passwords are jeopardized.
- Passwords are displayed in the front-end for convenience: Users with access to the management server can retrieve the passwords at any time during server life-time for their convenience.

However:
- Room passwords are used to secure access to user-provided Code City configurations, often including source code and potentially other data that is not intended for the public.


### JSON Web Token (JWT)

As mentioned above, JWTs are used to authenticate users while accessing the API after log-in.

JWT rely on a secret configured by the server. If this secret is uncovered, the security measures are completely useless.
Remember to configure an individual secret for your server instance and take measures to keep it a secret.

#### Tokens are never invalidated

At the time of writing, tokens are never invalidated.
While they are generated with a TTL (max age) of 24 hours during login, they are not invalidated on logout.
They are merely removed from the client by replacing the cookie with an empty one.
