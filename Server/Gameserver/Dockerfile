FROM --platform=linux/amd64 ubuntu:24.04

ENV PASSWORD=
ENV SERVERID=
ENV BACKEND_DOMAIN=localhost:8080

RUN apt-get update  &&\
    apt-get upgrade -y  &&\
    apt-get install -y --no-install-recommends unzip  &&\
    rm -rf /var/lib/apt/lists/*

COPY . /app/

WORKDIR /app

EXPOSE 7777

CMD ./server.x86_64 -port 7777 -password ${PASSWORD} -id ${SERVERID} -domain ${BACKEND_DOMAIN} -launch-as-server -batchmode
