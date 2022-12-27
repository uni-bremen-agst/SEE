#!/bin/sh

# This commit will only exist on old histories.
git --no-replace-objects show -s d5eb0645d999623b72025d0bf19227a787af1fb1 2>/dev/null
if [ $? = 0 ]; then
    echo "Looks like you have a copy of the repository with a bad commit."
    echo "Please save your work to a temporary location and delete this repository."
    echo "If you create a fresh clone, that should fix this problem."
    exit 1
fi
