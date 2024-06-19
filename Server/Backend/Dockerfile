# Verwende ein offizielles Maven-Image für den Build-Prozess
FROM maven:3.8.4-openjdk-17-slim AS build

# Arbeitsverzeichnis für den Build
WORKDIR /app

# Kopiere die Quellcode-Dateien und die POM-Datei
COPY src ./src
COPY pom.xml .

# Führe den Maven-Build aus
RUN mvn clean package -DskipTests

# Verwende das offizielle Ubuntu 20.04 als Basisimage
FROM ubuntu:20.04
# Aktualisiere die Paketliste und installiere OpenJDK 17
RUN apt-get update && \
    apt-get install -y openjdk-17-jdk && \
    apt-get install docker.io -y
# Setze die Umgebungsvariable JAVA_HOME
ENV JAVA_HOME /usr/lib/jvm/java-17-openjdk-amd64
# Füge JAVA_HOME zum PATH hinzu
ENV PATH $PATH:$JAVA_HOME/bin
EXPOSE 8080
COPY --from=build app/target/see-backend.jar see-backend.jar
ENTRYPOINT ["java","-jar","/see-backend.jar"]
