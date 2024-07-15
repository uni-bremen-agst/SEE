# SEE Managed Server

This is the management server for SEE, consisting of a backend and a frontend project.

The management server can be used to configure, run, and stop SEE game server instances.
Additionally, it provides an API to store and retrieve files for the use in multiple connected SEE clients.


--------------------------------------------------------------------------------
## Documentation

Both subprojects have their own README file in their respective subfolders, which comes with additional information.

Keep the information collected here and in the READMEs of the subprojects up-to-date, and update them in a timely fashion, at the latest before merging the development branch back into the main branch.

Please feel free to add additional info whenever you change anything that is covered in this document, or whenever you overcome hurdles and would have liked to have found the information here in the first place.


--------------------------------------------------------------------------------
## Dependencies

The Management Server stack requires additional services during runtime:

- Docker or Podman containerization framework

The stack can be run using Podman/Docker Compose.
A `compose.yaml` file is provided for this purpose.

Read sections *Development Environment* and *Production Environment* for additional information.


--------------------------------------------------------------------------------
## Development Environment

### Containers

It is necessary to set up a container environment in order to:

- run the Management stack via Compose
- allow the Management backend to spawn SEE game server instances

Before you install Docker or Docker Desktop, consider this:

- Any user who has access to the Docker daemon will potentially have **complete root/admin access** to your machine.
- Allowing your user to control Docker (e.g., by adding it to group `docker`) opens a large attack surface. **Any program or script can instantly become root/admin on your system.**

To mitigate this, consider using Podman instead and [configure it to work in rootless mode](https://wiki.archlinux.org/title/Podman#Rootless_Podman).

To make Podman available via socket, it is necessary to enable the SystemD User Unit:

```
systemctl --user enable podman.socket
systemctl --user start podman.socket
```

It should automatically propagate the socket via environment.
Check using `echo "$DOCKER_HOST"` if unsure. Also check that the `.sock` file exists.

**Heads up:** This means, the backend will automatically find your Podman instance and use it to spawn containers.


### Database

SQLite database files can be inspected using [DB Browser for SQLite](https://sqlitebrowser.org/).


--------------------------------------------------------------------------------
## Production Environment

At the current state, the management server is not optimized for production environments.

Please refer to the *Security Considerations* in the backend README for additional information.


### HTTPS

The management server as provided here does not implement HTTPS.
You are strongly advised to set up a reverse proxy and only provide TLS-secured access.

Add `secure` flag to cookies so that they are only transferred over HTTPS.
Using nginx `proxy_pass`, you would add something like this:

```
proxy_cookie_flags ~ secure;
```


### Containerization

The backend – by default – has complete access to the Docker server it is running on.
Being able to launch any container means, it can potentially manipulate anything on the server.
If the backend itself is compromised, the complete server is also.

This can be mitigated by either:

- Running the backend or the complete stack in a VM.
- Giving the backend access to a separate Docker instance.
  - Set up a separate machine or VM to provide a Docker host for the game servers and configure the backend to use this instead.
- Use Podman in rootless mode.
  - This will usually prevent the frontend to open port 80, which should be no problem as a reverse proxy with HTTPs should be used, anyway.
  - See above for additional information on how to achieve this.


### Deployment

Deployment is intended to be done using Podman or Docker.

Please read the `compose.yaml` carefully and adapt the configuration for your needs,
especially for public/production setups!

The container stack can be run from `Server` directory using `podman-compose` or `docker-compose`.  

To build the containers, you can use:

```
podman-compose build
```

Use `--no-cache` parameter to force a rebuild.

To run the container, use:

```
podman-compose up
```

Use `-d` parameter to run in detached mode.

The following command stops and cleans up the container setup:

```
podman-compose down
```
