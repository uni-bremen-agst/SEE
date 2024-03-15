
# How to run:

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

