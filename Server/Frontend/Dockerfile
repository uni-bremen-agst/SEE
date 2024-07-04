FROM node:21-alpine AS base
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN corepack enable
COPY . /app
WORKDIR /app

FROM base AS prod-deps
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --prod --frozen-lockfile

FROM base AS build
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --frozen-lockfile
RUN pnpm install
RUN pnpm run build

FROM nginx:alpine
RUN sed -i /etc/nginx/conf.d/default.conf \
        -e 's|\(listen.*\)80\(.*\)|\18080\2|g;'
WORKDIR /usr/share/nginx/html
COPY --from=build /app/dist .
CMD ["nginx", "-g", "daemon off;"]
EXPOSE 8080
