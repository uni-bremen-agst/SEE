FROM --platform=linux/amd64 docker.io/ubuntu:24.04

ENV SEE_BACKEND_DOMAIN=localhost:8080
ENV SEE_SERVER_ID=
ENV SEE_SERVER_PASSWORD=

COPY run.sh /
COPY bin /app

RUN  \
  apt-get update  &&\
  apt-get upgrade -y  &&\
  apt-get install -y --no-install-recommends unzip  &&\
  rm -rf /var/lib/apt/lists/*  &&\
  #
  chmod 555 /run.sh

WORKDIR /app

EXPOSE 7777/udp

CMD /run.sh
