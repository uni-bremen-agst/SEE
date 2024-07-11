# SEE Managed Server Backend

This is the backend for managing running and managing SEE server instances.

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

Manages users and their passwords along with other metadata like role assignment.  
Data is stored in the relational database.

### Server Service

Manages metadata on SEE game server instances and controls them using `ContainerService`.  
Data is stored in the relational database.

### Container Service

Manages SEE game server instances that are launched via Docker containers.

### File Service

The file service allows for storing and retrieving files that are required to render Code Cities in SEE clients.  
Metadata to identify and retrieve files are stored in the relational database.


--------------------------------------------------------------------------------
## Dependencies

This backend requires a container sruntime to manage SEE game server instances.
Refer to the README in the parent directory for additional considerations.

A `compose.yaml` file is provided to be used during backend development.
The backend image itself is configured in the `Dockerfile`.  
The complete stack, including back- and frontend, is configured in the parent directory.


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

Install Project Lombok integration to allow Eclipse to see auto-generated getter/setter functions.

Check the [official manual](https://projectlombok.org/setup/eclipse) on the project website.

The process should boil down to this:

1. Click *Help* > *Install New Software…*
2. Add the source (*Work with:*): `https://projectlombok.org/p2`
3. Select and install Lombok.
4. Quit and open Eclipse again. Don't use the restart option.

**Note:** This did not make it work after all. Please update this section if you can resolve the issue!


--------------------------------------------------------------------------------
## Security Considerations

There are a few things to keep in mind while using the app.
In this section you will find a non-exhaustive list of security considerations.

Please also read the parent README for additional information.


### Passwords

**Passwords get transmitted in plaintext.**

There are two types of passwords at the time of writing:

1. Passwords for users registered in the backend application:

Passwords are transmitted in plaintext during registration and sign-in.
After logging in to the backend (usually using the frontend), a token (JWT) is transmitted to the client that is henceforth used for authentication of requests (see below).

User passwords are stored in the database as a salted bCrypt hashes, which is a state-of-the-art method to secure passwords at rest.

2. *Room* or *server passwords*:

These passwords are auto-generated when a server instance is created.
The passwords are transmitted in plaintext during any request from the SEE client to the backend to retrieve files.
They are also stored in the database as plaintext.
This is much less secure than how user passwords are handled.

Rationale:
- Server instances are frequently created and destroyed, thus passwords are usually only used for a short time.
- Room passwords are only used on certain API end-points and do not grant access to control the backend.
- Passwords are auto-generated: no user-defined secrets are leaked.
- Passwords are displayed in the front-end for convenience: Users can access the passwords at any time during server life-time for their convenience.

However:
- Room passwords are used to secure access to user-provided Code City configurations, optionally including source code and potentially other data that is not intended for the public.


### JSON Web Token (JWT)

As mentioned above, JWTs are used to authenticate users while accessing the API after log-in.
However, this does not (yet) apply to the file requests made by the SEE clients, which operate using plaintext passwords (see above).

JWT rely on a secret configured by the server. If this secret is uncovered, the security measures are completely useless.
Remember to configure an individual secret for your server instance and take measures to keep it a secret.

At the time of writing, tokens are never invalidated.
While they are generated with a TTL (max age) of 24 hours during log-in, they are not invalidated on log-out.
They are merely removed from the client by replacing the cookie with an empty one.
