#!/bin/bash
### This script will change the path of CodeFacts.gxl to your local installation.
set -o errexit
set -o nounset

TARGET="CodeFacts.gxl"
if [[ $# -gt 1 || $1 == "-h" || $1 == "--help" ]]; then
	printf "usage: %s [TARGET]\n\nThis script will change the paths in TARGET to your local paths to SEE. If TARGET is not specified, the CodeFacts.gxl file will be used." "$0"
	exit 0
elif [[ $# == 1 ]]; then
	if [[ -r "$1" && -w "$1" ]]; then
		TARGET="$1"
	else
		echo "The given file either doesn't exist, or isn't readable and writable."
		exit -1
	fi
fi

# ask for validation
echo "This script will change the paths in $TARGET to your path to SEE, this way it can access the source code files correctly."
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
	drive=$(echo "${BASH_REMATCH[1]}" | tr "[:lower:]" "[:upper:]") # make uppercase
	otherpath="${BASH_REMATCH[2]}"
	seepath=$(echo "${drive}:/${otherpath}/" | sed 's/\//\\\//g') # escape slashes
	echo "$seepath"
else
	echo "Couldn't find SEE on your drive, which is very weird, considering this script should be running from it right now. Did you move this script outside of SEE's folder?"
	exit 2
fi

# finally, replace path with this user's path
if perl -pi.bak -e "s/[A-Z]:\/(.*)\/(?=Assets|Library|Temp)/$seepath/g" "$TARGET"; then
	echo "Success, path has been replaced. A backup of the original file is accessible at $TARGET.bak."
	exit 0
else
	echo "There seems to have been an unkown problem while replacing the path. Maybe the $TARGET doesn't exist at that path or in $SCRIPTPATH?"
	exit 3
fi
