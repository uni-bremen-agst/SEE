#!/bin/sh
set -eu

./GitScripts/check_for_old_commits.sh
./GitScripts/check_for_rendered_cities.sh
