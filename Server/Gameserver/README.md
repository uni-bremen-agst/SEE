# Game Server Container

This directory contains the game server container configuration for the SEE management server.


## Building SEE Game Server

To build the game server that can be dropped into the `bin` folder, you need to configure Unity accordingly:

1. Select *File* > *Build Settings…*
2. Select *Platform: Dedicated Server*
3. Select *Target Platform: Linux*
4. Click *Build* and select the output directory and enter name `server`.


## Building Server Image

Drop the compiled SEE server files into the `bin` subdirectory before you build the container image.

To build the game server image, execute the following command from this working directory:

```
docker build -t see-gameserver .
```

## Backend Access

Please note that if the Backend domain is configured as `localhost`, the game server will not be able to access the Backend from within the container.
This is because `localhost` will refer to the game server's container and not the Backend container or the container host.
