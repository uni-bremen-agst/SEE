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

See section *Development Environment* for additional considerations.


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

As the backend – by default – has complete access to the Docker server it is running on, it can potentially manipulate anything on the server.

This can be mitigated by either:

- Running the complete stack in a VM.
  - Use a reverse proxy (for HTTPs) outside the VM to keep key material safe.
- Giving the backend access to a separate Docker instance.
  - Set up a separate machine or VM for the game servers and configure the backend to use this instead.
- Use Podman in rootless mode.
  - This will usually prevent the frontend to open port 80, which should be no problem as a reverse proxy with HTTPs should be used, anyway.


--------------------------------------------------------------------------------
## How to run:

**TODO:** This is the original content of this file. It will be replaced soon.

- Vorbereitung: 
Um das Projekt zu starten, wird Docker benötigt. Docker kann unter Linux mit den meisten 
Paketmanagern heruntergeladen werden, unter Windows kann man Docker über 
https://www.docker.com/ herunterladen und installieren.

- Nachdem Docker installiert ist, muss man in den Ordner Gameserver-Linux über das Terminal 
navigieren und dort den Befehl ```docker build -t see-gameserver .``` ausführen.

- Nachdem der Befehl ausgeführt wurde, kann man über das Terminal zurück in den übergeordneten 
Ordner wechseln (in dem auch das ```compose.yaml``` liegt) und dort den Befehl 
```docker-compose up --build``` ausführen. Damit sollen alle Dienste für den Betrieb gestartet 
werden.

- Sobald alle Dienste gestartet sind, kann man über den Webbrowser mit dem Link 
http://localhost/ auf die Verwaltungsübersicht zugreifen.

- Hier kann man sich nun mit dem Benutzernamen "thorsten" und dem Passwort "password" anmelden.

- Bei verbinden mit einem Server, kann der aktuelle Port mithilfe des "IP Teilen" Knopfes 
gefunden werden.

- Dieser muss beim Starten von SEE in das Feld "Server UDP Port" eingefügt werden.

