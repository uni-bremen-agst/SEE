#!/bin/bash
# Modified from example script present in Axivion suite.

set -ex -o pipefail

# Setup environment for Axivion Suite
# (bauhaus-kshrc is located directly in the installation directory)
. /home/falko17/Dokumente/git/bauhaus-suite/bauhaus-kshrc

# update these variables according to your local setup
export AXIVION_DATABASES_DIR=/home/falko17/Dokumente/axivion
export AXIVION_DASHBOARD_URL=http://localhost:9090/

# Assume project configuration is stored in the same directory as this script
# locate that directory (variable confdir)
confdir="$(dirname "$(realpath "$0")")"

# for convenience in make calls or similar setups
# export NUM_PROCESSES=$(nproc)

# for on the fly configuration of compiler profile
# gccsetup --gcc gcc --g++ g++ --toolchain_name MyToolchain --config "$confdir/compiler_config.json"

# Invoke the build and analysis for the project
export AXIVION_PROJECTNAME=SEE
export BAUHAUS_CONFIG="$confdir"
axivion_ci -j "$@"
