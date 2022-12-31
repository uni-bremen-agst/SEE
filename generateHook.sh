#!/bin/sh
# This script is intended for POSIX environments, e.g. Linux or Mac OS.
# For Windows, please use the corresponding '.bat' script.
# This will install a pre-commit hook which verifies that no code cities
# were left in the scene. Call it by simply executing './generateHook.sh'.

cp GitScripts/run_all.sh .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
