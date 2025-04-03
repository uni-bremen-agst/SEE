# List available recipes
@default:
  just --list


[working-directory: 'Backend']
start-backend-dev:
    ./mvnw spring-boot:start

# Generates a new jwt secret key and inserts it into the specified FILE
seed FILE:
    RANDOM_STRING=$(openssl rand -base64 48 | tr -d '/+' | cut -c1-64) && \
    sed -i "s/^JWT_SECRET=.*$/JWT_SECRET=$RANDOM_STRING/" {{FILE}}

# Starts the SEE-Manager compose stack in the background
start:
    docker compose up -d

# Starts the SEE-Manager compose stack in the foreground
up *ARGS:
    docker compose up {{ARGS}}

# Stops the SEE-Manager compose stack
down *ARGS:
    docker compose down {{ARGS}}

# Buildss the SEE-Manager docker images locally
containerize SERVICE="" *ARGS="":
    docker compose build {{ARGS}} {{SERVICE}}

# Pulls the docker images for the SEE-Manager and the SEE-Gameserver
pull-images:
    docker compose pull
    docker pull ghcr.io/uni-bremen-agst/see-gameserver:1.0.0
