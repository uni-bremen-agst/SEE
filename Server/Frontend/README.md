# SEE Managed Server Frontend

This is the front-end application to control the SEE management Backend.


## Development

The front-end application is developed in [TypeScript](https://www.typescriptlang.org/) using [NodeJS](https://nodejs.org/en/download) and [`pnpm`](https://pnpm.io/pnpm-cli).

A configuration for [VS Code](https://vscodium.com/) is provided to enable auto-formatting in the Frontend project.
Note that this ([as of July 2024](https://github.com/Microsoft/vscode/issues/32693)) requires you to open the Frontend project directly (*Open Folder* or *Add Folder to Workspace*) and not one of the parent directories.
Please try not to reformat files in a different manner.


### Frameworks

- [React](https://react.dev/)
- [Material UI](https://mui.com/material-ui/all-components/)


### Build and Run

To install dependencies, run:

`pnpm install`

The following command will run the front-end application in *development mode*:

`pnpm run dev`

To build the application for deployment, use the following command:

`pnpm run build`

The run scripts are defined in the `package.json` file along with the package dependencies.

[See here](https://pnpm.io/cli/run) for `pnpm run` documentation.


### Dependency Management

It is vital to keep dependencies up-to-date to fix bugs including known security flaws.
The following command will bump version numbers:

```
pnpm up
```

Using parameter `--latest` will update versions more rigorously and the app might break.
It is, however, important to check if the used versions are still maintained and receive security fixes.

[See here](https://pnpm.io/cli/update) for `pnpm update` documentation.
