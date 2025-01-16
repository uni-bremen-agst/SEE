#!/bin/bash
# This pre-commit hook verifies that no LFS-suitable files are being committed
# into the repository, as they should be placed within LFS instead.
# This check will trigger if at least one of the following is true
# ("Changes" refers to non-LFS changes):
#
# 1. Changes contain a file bigger than 10 MB.
# 2. Changes as a whole (additions only) constitute more than 50 MB.
# 3. Changes contain directories within `Assets` which are not a part of SEE.
#    These should be placed in LFS as they may be subject to copyright
#    and thus should not be contained in the (potentially in the future) public
#    GitHub repository.
#
# Note that this script is only run as part of CI, because we can only check
# blob sizes after the commit has been made, so it can't be run as a
# pre-commit hook.

# Maximum allowed file size.
FILE_SIZE="10 * 2^20" # 10 MB.
# Maximum allowed total diff size.
DIFF_SIZE="50 * 2^20" # 50 MB.

if [ -n "$CI" ]; then

  set -e

  # Check for non-SEE Assets.
  CHANGED_ASSETS=$(git diff --name-only --diff-filter=AM origin/master -- Assets ':!Assets/Editor' ':!Assets/Native' ':!Assets/StreamingAssets' ':!Assets/csc.rsp*' ':!Assets/NuGet.config*' ':!Assets/packages.config*' ':!Assets/Resources' ':!Assets/Scenes' ':!Assets/SEE*' ':!Assets/XR' | git check-attr --stdin filter | grep -v "filter: lfs" || exit 0)
  if [ -n "$CHANGED_ASSETS" ]; then
    echo ""
    echo -n "::error title=Assets not in LFS::You have committed non-SEE assets outside of LFS.%0A"
    echo -n "All (bought) non-SEE assets should be moved into LFS for space saving and copyright reasons.%0A"
    echo -n "Please rebase your branch and move affected assets into LFS, so that no commit%0A"
    echo -n "remains which references them.%0A"
    echo -n "If you need help with this, or believe that moving these into LFS is not the right thing to do, please contact @koschke or @falko1 on Mattermost.%0A"
    echo -n "Files which were detected as non-SEE assets are shown below.%0A"
    echo "$CHANGED_ASSETS"
    exit 1
  fi

  # Check for single changes bigger than 10 MB.
  # From: https://stackoverflow.com/a/42544963 (slightly modified)
  TOO_BIG=$(git rev-list --objects HEAD ^origin/master |
    git cat-file --batch-check='%(objecttype) %(objectname) %(objectsize) %(rest)' |
    sed -n 's/^blob //p' |
    awk "\$2 >= $FILE_SIZE { print \$2,\$3}" |
    sort --numeric-sort --key=1)
  if [ -n "$TOO_BIG" ]; then
    # There were files which were too big.
    echo ""
    echo -n "::error title=Big files not in LFS::You have committed files bigger than 10 MB.%0A"
    echo -n "Such files should be moved into LFS, otherwise the normal Git repository will%0A"
    echo -n "become too big and slow.%0A"
    echo -n "Please rebase your branch and move affected files into LFS, so that no commit%0A"
    echo -n "remains which references them.%0A"
    echo -n "If you need help with this, or believe that moving these into LFS is not the right thing to do, please contact @koschke or @falko1 on Mattermost.%0A"
    echo -n "Files which were above the set size limit are shown below.%0A"
    echo "$TOO_BIG"
    exit 2
  fi

  # Check for aggregate changes (additions only) bigger than 50 MB.
  TOTAL_SIZE=$(git format-patch origin/master --stdout | grep '^+' | wc -c | awk "\$1 >= $DIFF_SIZE")
  if [ -n "$TOTAL_SIZE" ]; then
    echo ""
    echo -n "::error title=Huge diff size::Your PR has a diff size of > 50 MB.%0A"
    echo -n "You should put big directories into LFS.%0A"
    echo -n "Please rebase your branch and move affected directories into LFS, so that no commit%0A"
    echo -n "remains which references them.%0A"
    echo -n "If you need help with this, or believe that moving these into LFS is not the right thing to do, please contact @koschke or @falko1 on Mattermost.%0A"
    echo "Your PR diff size in bytes: $TOTAL_SIZE"
    exit 3
  fi

fi
