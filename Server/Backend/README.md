# SEE Managed Server Backend

This is the backend for managing running and managing SEE server instances.

--------------------------------------------------------------------------------
## Overview

The project provides a REST API based on Spring Boot with the following features:

- Manage users and roles (`UserService`)
- Manage SEE game server configurations (`ServerService`)
- Manage SEE game server instances via Docker containers (`ContainerService`)
- Manage file storage (`FileService`)

### Dependencies

This backend requires additional services during runtime:

- MySQL database
- MinIO data storage engine
- Docker containerization framework

The original authors intended these services to be run using Docker Compose.
A `compose.yaml` file is provided to be used during backend development.
The complete stack, including back- and frontend, is configured in the parent directory.

The backend container itself is built using `Dockerfile`.

### User Service

Manages users and their passwords along with other metadata like role assignment.
Data is stored in the relational database.

### Server Service

Manages metadata on SEE game server instances.
Data is stored in the relational database.

### Container Service

Manages SEE game server instances that are launched via Docker containers.

### File Service

The file service allows for storing and retrieving files that are stored in MinIO buckets.
Metadata to identify and retrieve files are stored in the relational database.


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
