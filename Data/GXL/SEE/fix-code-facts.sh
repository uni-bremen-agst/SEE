#!/bin/bash
### This script will change the path of CodeFacts.gxl to your local installation.
set -o errexit
set -o nounset

echo "This script will change the paths in CodeFacts.gxl to your path to SEE, this way it can access the source code files correctly."
if [[ $# -gt 1 ]]; then
	echo "Run this script without any parameters to execute it now."
	exit 0
fi

# ask for validation
echo -n "Proceed? [y/n]: "
read -rn 1 answer
if [[ "$answer" != "y" ]]; then
	echo "Aborting."
	exit 0;
fi
echo ""

# check whether the path is of the form /c/..., otherwise future regexes will not work.
# (https://stackoverflow.com/a/4774063)
SCRIPTPATH="$( cd -- "$(dirname "$0")" >/dev/null 2>&1 ; pwd -P )"
if [[ ! "$SCRIPTPATH" =~ ^/[a-z]/ ]]; then
	echo "Sorry, this script only supports GitBash/git-scm/MINGW (https://gitforwindows.org) installations for now."
	exit 1
fi

# extract path to SEE
if [[ "$SCRIPTPATH" =~ ^/([a-z])/(.*)/Data/GXL ]]; then
	drive=$(echo "${BASH_REMATCH[1]}" | tr [:lower:] [:upper:]) # make uppercase
	otherpath="${BASH_REMATCH[2]}"
	seepath="${drive}:/${otherpath}"
	echo "$seepath"
else
	echo "Couldn't find SEE on your drive, which is very weird, considering this script should be running from it right now. Did you move this script outside of SEE's folder?"
	exit 2
fi

# finally, replace path with this user's path
if perl -pi.bak -e "s/[A-Z]:\/(.*)\/(?=Assets|Library|Temp)/C:\/Users\/Falko\/Documents\/SEE\//g" "$SCRIPTPATH/CodeFacts.gxl"; then
	echo "Success, path has been replaced. A backup of the original file is accessible at CodeFacts.gxl.bak."
	exit 0
else
	echo "There seems to have been an unkown problem while replacing the path. Maybe the CodeFacts.gxl doesn't exist in $SCRIPTPATH?"
	exit 3
fi
