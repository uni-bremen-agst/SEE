FROM docker.io/eclipse-temurin:17-jdk as builder
COPY . /app/
WORKDIR /app
RUN --mount=type=cache,id=maven,target=/maven-cache     \
  ./mvnw -Dmaven.repo.local=/maven-cache  \
         clean package -DskipTests

# ----------------------------------------------------------

FROM docker.io/eclipse-temurin:17-jdk
RUN apt-get update  &&\
    apt-get upgrade -y
COPY --from=builder /app/server/target/see-manager-backend.jar  \
                    /see-backend.jar
WORKDIR /see
CMD ["java","-jar","/see-backend.jar"]
VOLUME /see
EXPOSE 8080
