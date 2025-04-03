# SEE Managed Server

## About this Project
This is the management server for SEE, consisting of a backend and a frontend project.

The management server can be used to configure, run, and stop SEE game server instances.
Additionally, it provides an API to store and retrieve files for the use in multiple connected SEE clients.

## Build with

* [![React][React.js]][React-url]
* [![Vite][Vite]][Vite-url]
* [![Spring Boot][Springboot]][Springboot-url]
* [![Traefik][Traefik]][Springboot-url]


[React.js]: https://img.shields.io/badge/React-20232A?style=for-the-badge&logo=react&logoColor=61DAFB
[React-url]: https://reactjs.org/

[Springboot]: https://img.shields.io/badge/Spring%20Boot-6DB33F?logo=springboot&style=for-the-badge&logoColor=fff
[Springboot-url]: https://spring.io/projects/spring-boot

[Traefik]: https://img.shields.io/badge/Traefik-24A1C1?style=for-the-badge&logo=traefikproxy&logoColor=black
[Traefik-url]: https://traefik.io/

[Vite]: https://img.shields.io/badge/Vite-B73BFE?style=for-the-badge&logo=vite&logoColor=FFD62E
[Vite-url]: https://vitejs.dev/

## Get Started

### Prerequisites
+ Docker (or Podman) need to be installed (https://www.docker.com/)
+ (Optional) Install [just](https://github.com/casey/just)
  + You can view all available commands with the command `just` or  `just --list`


### :key: Environment Variables
To run this project, you will need to config the following environment variables in the .env file to your needs.

+ DOMAIN_NAME: The domain address under which the frontend and backend should be served.
+ EXTERNAL_PORT: The port under which the compose stack will be served (Default is 80).
+ DOCKER_HOST_EXTERNAL: The address under which the gameserver should be registered - currently only IPv4 addresses are supported.
+ DOCKER_IMAGE_NAME: The docker image name of the gameserver that should be used for spawning a new instance.
+ JWT_SECRET: The JWT secret that should be used when signing auth tokens
+ JWT_EXPIRATION: The duration of how long a  JWT should live.
+ INITIAL_ADMIN_USERNAME: The initial admin username that will be created when the server is started.
+ INITIAL_ADMIN_PASSWORD: The initial admin password that will be created when the server is started.

You can create a new random jwt secret by running the following just command (openssl required):
```console
just seed .env
```
It is recommended that you change the JWT secret before using the server iin production.

Before starting make sure that:
+ DOMAIN_NAME matches a reachable address of this machine
+ EXTERNAL_PORT is exposed to the network
+ DOCKER_HOST_EXTERNAL matches a valid, reachable IPv4 address of your machine

### :running: Run Locally
Please refer to the README files in the Frontend and Backend directories for detailed instructions on how to run the SEE Server Manager locally.

### :triangular_flag_on_post: Deployment
Clone the project

```bash
$ git clone https://github.com/uni-bremen-agst/SEE.git
```
Then cd into the directory
```bash 
$ git clone
$ cd SEE/Server
```

Now pull the docker images with

```bash
$ docker compose pull
```
or using the just command

```bash 
$ just pull-images
```
You also need to pull the images of the actual game server.
The containers from this image will be used to create the server instances.

```bash
$ docker pull ghcr.io/uni-bremen-agst/see-gameserver:1.0.0
```


#### Start the Server

The SEE Server manager uses [Traefik](https://traefik.io/traefik/) as a reverse proxy for the frontend and backend which will be exposed at port 80 by default.

If the server is reachable from the internet, it is recommended to use the SSL support of Traefik to secure the connection.
In that case you need to adjust the `compose.yaml` file and and uncomment necessary [configuration](https://doc.traefik.io/traefik/https/tls/) for the SSL certificates.

Note: Traefik also supports automatic certificate generation using [Let's Encrypt](https://doc.traefik.io/traefik/https/acme/).


To start the server run:
```console
$ docker compose up -d
```
or
```console
$ just start
```

After that you can open the frontend by navigating to the domain address you configured in the `prod.env` file in your browser.

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


--------------------------------------------------------------------------------
## Usage

Using the Frontend, you can create and manage servers on the Backend.

When creating a new server, you can attach several Code City archives that are uploaded to the backend.
Each Code City type has a dedicated table in the virtual world.
Clients will automatically download the archives from the backend when connecting to the server instance and try to instantiate them on the appropriate table.


### Prepare Code Cities

The Code Cities are stored in the `Assets/StreamingAssets/Multiplayer/` directory.

**Heads up:** The `Multiplayer` directory will get cleared if you connect to a server. Keep that in mind while you prepare your Code Cities for upload!

Prepare your Code Cities depending on their type in the respective subdirectories:

- `SEECity`
- `DiffCity`
- `SEECityEvolution`
- `SEEJlgCity`
- `SEEReflexionCity`

This is necessary so that all relative paths are correctly referenced, e.g., in the `.cfg` files.

Each of the Code Cities should contain a valid configuration file (`.cfg`),
which is automatically loaded by the SEE clients after connecting to the server and downloading the files from the backend.

Each of the above Code City directories should be individually zipped.
You can either zip the directories' contents or the folder itself.
