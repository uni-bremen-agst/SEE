#!/usr/bin/env bash
# for_each_revision.sh first..last

let i=0
# iterate on all merge commits into master in reverse chronological order
for rev in $(git rev-list --merges --first-parent master HEAD | tac)
do
  #git checkout "$rev"
  let i++
  timestamp=`git show --no-patch --no-notes --pretty=%ct $rev`
  echo "$i" "$rev" "$timestamp"
done

exit 0
