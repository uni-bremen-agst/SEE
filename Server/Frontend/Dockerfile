FROM docker.io/node:22-alpine AS builder
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
COPY . /app
WORKDIR /app
RUN --mount=type=cache,id=pnpm,target=/pnpm/store  \
  corepack enable  &&\
  pnpm install --frozen-lockfile  &&\
  pnpm run build

# ----------------------------------------------------------

FROM docker.io/nginx:alpine
COPY --from=builder /app/dist /usr/share/nginx/html
RUN sed -i /etc/nginx/conf.d/default.conf \
        -e 's|\(listen.*\)80\(.*\)|\18080\2|g;'
EXPOSE 8080
