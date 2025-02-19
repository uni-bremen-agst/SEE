#!/usr/bin/env bash
# Backs up or restores cache for the GitHub Actions runners.
# This assumes that only the sts5 runner is used!
# Locking functionality based on https://gist.github.com/przemoc/571091 (MIT license).

set -eu

# Prepare locking.
LOCKFILE="/var/lock/seecache"
LOCKFD=99
lock() { flock "-$1" $LOCKFD; }
_no_more_locking() {
  lock u
  (lock xn && rm -f $LOCKFILE) || true
}
eval "exec $LOCKFD>\"$LOCKFILE\""
trap _no_more_locking EXIT

# Actual caching starts here.
CACHEDIR="/local/users/falko1/.actions-cache"
TARGETS="Library/ *.csproj *.sln"

if [ "$OPERATION" = "clean" ]; then
  echo "Cleaning cache..."
  CACHEDIRS=("$CACHEDIR"/*)
  NUM_CACHES=${#CACHEDIRS[@]}
  # Once we have more than four cache directories...
  if [ "$NUM_CACHES" -gt 4 ]; then
    # ...we will remove the oldest one.
    OLDEST=$(find .actions-cache/ -maxdepth 1 -type d -printf '%T+ X%p\n' | sort | head -n 1 | cut -d'X' -f2-)
    echo "Removing $OLDEST..."
    rm -rf "$OLDEST"
  fi
  exit 0
fi

if [ -z "$KEY" ]; then
  echo "\$KEY environment variable must be set to a non-empty string."
  exit 1
fi

KEYCACHE="$CACHEDIR/$KEY/"

if [ "$OPERATION" = "store" ]; then
  # In this case, we will copy all targets over to our caching directory, using the key as a subdir.
  # If the cache exists already, we will update it.
  lock x
  echo "Creating cache for $KEY..."
  mkdir -p "$KEYCACHE"
  # shellcheck disable=SC2086
  rsync -a --update --ignore-missing-args --relative $TARGETS "$KEYCACHE"
  lock u
elif [ "$OPERATION" = "restore" ]; then
  lock s
  # Restore cache if it exists.
  if [ -d "$KEYCACHE" ]; then
    echo "Restoring cache for $KEY..."
    rsync -a --inplace --no-compress "$KEYCACHE" .
  else
    echo "No cache exists for $KEY."
  fi
  lock u
else
  echo "Unknown operation (or not set): $OPERATION"
  exit 2
fi
