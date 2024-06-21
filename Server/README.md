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
## How to run:

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

