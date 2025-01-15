#!/bin/bash

# This checks for issues with the Git repository using `git fsck`.
# For more information on what constitutes a Git error, review the
# documentation of `git-fsck`.

if [ -z "$CI" ]; then
  # This check takes too long to execute.
  # We do not want to run it locally on every commit.
  exit 0
fi

# Dangling errors do not matter.
GIT_FSCK="git fsck --strict --no-dangling --no-progress"

output=$($GIT_FSCK)
if [ $? != 0 ]; then
  echo ""
  echo -n "::error title=Git error detected::This branch contains at least one Git error, as detected by \`git fsck\`.%0A"
  echo -n "The fix depends on the specific error type, which is reproduced below."
  echo -n "You may need to rebase your branch to get rid of the error.%0A"
  echo -n "Check locally by running \`git fsck --strict --no-dangling\`.%0A"
  echo -n "If you are not able to identify or fix the error, please contact @koschke or @falko1 on Mattermost.%0A"
  echo -n "The detected Git errors will be shown below:%0A"
  echo "$output"
  exit 1
fi
