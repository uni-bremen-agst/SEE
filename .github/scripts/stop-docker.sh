#!/bin/bash
docker stop "$(docker ps -q)" || true
