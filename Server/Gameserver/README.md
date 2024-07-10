# SEE Managed Games Server Container

This directory contains the game server container configuration for the SEE management server.


## Building SEE Game Server

To build the game server that can be dropped into the `bin` folder, you need to configure Unity accordingly:

1. Select *File* > *Build Settings…*
2. Select *Platform: Dedicated Server*
3. Select *Target Platform: Linux*
4. Click *Build* and select the output directory and enter name `server`.


**TODO:** Currently, this build type does not work as expected. Further investigation is needed.


## Building Server Image

Drop the compiled SEE server files into the `bin` subdirectory before you build the container image.

To build the game server image, execute the following command from this working directory:

```
docker build -t see-gameserver .
```
