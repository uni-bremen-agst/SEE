

# List available recipes
@default:
  just --list


[working-directory: 'Backend']
start-backend-dev:
    ./mvnw spring-boot:start

# Generates a new jwt secret key and inserts it into the specified FILE
seed-jwt FILE:
    RANDOM_STRING=$(openssl rand -base64 48 | tr -d '/+' | cut -c1-64) && \
    sed -i "s/^JWT_SECRET=.*$/JWT_SECRET=$RANDOM_STRING/" {{FILE}}

# Starts the SEE-Manager stack
up *ARGS:
    docker compose up {{ARGS}}

# Stops the SEE-Manager stack
down *ARGS:
    docker compose down {{ARGS}}

containerize SERVICE="" *ARGS="":
    docker compose build {{ARGS}} {{SERVICE}}

pull-images:
    docker compose pull