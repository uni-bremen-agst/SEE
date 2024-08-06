#!/bin/sh
set -e

/app/server.x86_64  \
    --launch-as-server  \
    --port 7777  \
    --host "$SEE_BACKEND_DOMAIN"  \
    --id "$SEE_SERVER_ID"  \
    --password "$SEE_SERVER_PASSWORD"
