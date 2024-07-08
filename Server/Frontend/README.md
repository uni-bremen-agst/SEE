# SEE Managed Server Frontend

This is the front-end application to control the SEE management backend.


## Development

The front-end application is developed in [TypeScript](https://www.typescriptlang.org/) using [NodeJS](https://nodejs.org/en/download) and [`pnpm`](https://pnpm.io/pnpm-cli).


### Frameworks

- [React](https://react.dev/) 
- [Material UI](https://mui.com/material-ui/all-components/)


### Build and Run

To install dependencies, run:

`pnpm install`

[The following command](https://pnpm.io/cli/run) will run the front-end application in *development mode*:

`pnpm run dev`

To build the application for deployment, use the [following command](https://pnpm.io/cli/run):

`pnpm run build`

The run scripts are defined in the `package.json` file along with the package dependencies.


### Dependency Management

It is vital to keep dependencies up-to-date to fix bugs including known security flaws.
[The following command](https://pnpm.io/cli/update) will bump version numbers:

```
pnpm up
```

Using parameter `--latest` will update versions more rigorously and the app might break.
It is, however, important to check if the used versions are still maintained and receive security fixes.


### Deployment

Deployment is done using Podman or Docker.

The front-end container alone can be run from frontend directory using `podman-compose` or `docker-compose`.  
See parent README for information on how to deploy the whole stack.

To build the frontend container, you can use:

```
podman-compose build --no-cache
```

To run the container, use:

```
podman-compose up
```

The following command cleans up the container setup:

```
podman-compose down
```
