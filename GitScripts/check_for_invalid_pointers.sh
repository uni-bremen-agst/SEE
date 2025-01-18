#!/bin/bash

# This checks for invalid git LFS pointers, which can make a local copy
# of the repository unusable, so it should be avoided.
# It's unclear to me how such a situation occurs.
# So far it just had to be fixed once, in PR #545.

if [ -z "$CI" ]; then
  # This check takes too long to execute.
  # We do not want to run it locally on every commit.
  exit 0
fi

set -e

GIT_FSCK="git lfs fsck"

if [ -z "$CI" ]; then
  GIT_FSCK="$GIT_FSCK --pointers"
fi

output=$($GIT_FSCK || true)
if [ "$output" != 'Git LFS fsck OK' ]; then
  if [ -n "$CI" ]; then
    echo ""
    echo -n "::error title=Bad LFS pointer::!!! DO NOT MERGE INTO MASTER !!!%0A"
    echo -n "This branch contains a bad LFS pointer or object.%0A"
    echo -n "If it is a pointer error, it can be fixed by running \`git add --renormalize <bad-file>\`,%0A"
    echo -n "then committing the changes. Check locally by running \`git lfs fsck\`.%0A"
    echo -n "If this does not work, please contact @koschke or @falko1 on Mattermost.%0A"
    echo -n "The original error message will be shown below:%0A"
    echo "$output"
  else
    echo "You have an invalid Git LFS reference!"
    echo "Fix this before committing anything."
    echo "The easiest way to do this is by running \`git add --renormalize <file>\`"
    echo "on the file that is mentioned below (reproduced by \`git lfs fsck --pointers\`):"
    echo "$output"
  fi
  exit 1
fi
