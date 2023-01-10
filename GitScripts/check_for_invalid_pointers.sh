#!/bin/sh

# This checks for invalid git LFS pointers, which can make a local copy
# of the repository unusable, so it should be avoided.
# It's unclear to me how such a situation occurs.
# So far it just had to be fixed once, in PR #545.

set -e

GIT_FSCK="git lfs fsck"

if [ -z "$CI" ]; then
    GIT_FSCK="$GIT_FSCK --pointers"
fi

if [ -n "$CI" ]; then
    git lfs fsck
else
    output=$($GIT_FSCK)
    if [ "$output" != 'Git LFS fsck OK' ]; then
        if [ -n "$CI" ]; then
            echo "!!! DO NOT MERGE INTO MASTER !!!"
            echo "This branch contains a bad LFS pointer or object, as described below:"
            echo "$output"
            echo "If it is a pointer error, it can be fixed by running \`git add --renormalize <bad-file>\`,"
            echo "then committing the changes. Check locally by running \`git lfs fsck\`."
            echo "If this does not work, please contact @koschke or @falko1 on Mattermost."
        else
            echo "You have an invalid Git LFS reference!"
            echo "Fix this before committing anything."
            echo "The easiest way to do this is by running \`git add --renormalize <file>\`"
            echo "on the file that is mentioned below (reproduced by \`git lfs fsck --pointers\`):"
            echo "$output"
        fi
    fi
fi
