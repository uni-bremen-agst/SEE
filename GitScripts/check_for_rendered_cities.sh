#!/bin/bash
# This pre-commit hook verifies whether any drawn code cities have
# been left in the Assets/Scene folder. Only the diff for the attempted
# commit will be looked at, so existing code cities will be ignored.
# Only staged (to be committed) changes will be looked at.

set -e
if [ -n "$CI" ]; then
  DIFF_COMMAND="git diff origin/master"
else
  DIFF_COMMAND="git diff --staged"
fi

output=""
while IFS= read -r scene; do
  if [ -n "$scene" ]; then
    output="$output
Warning: Rendered CodeCities detected in scene '$scene'!"
  fi
done <<EOF
$($DIFF_COMMAND -S"m_TagString: Node" --name-only Assets/Scenes 2>/dev/null)
EOF

if [ -n "$output" ]; then
  printf "Please delete all drawn CodeCities before committing.\n%s" "$output"
  if [ -n "$CI" ]; then
    first_line=$(echo "$output" | head -n 1)
    echo ""
    echo -n "::error file=$first_line,title=Drawn code cities detected::Please delete all drawn CodeCities before committing.%0A"
    echo -n "%0A"
    echo -n "** NOTE **%0A"
    echo -n "You have already pushed these commits to the repository.%0A"
    echo -n "Before merging into master, identify the commits which have introduced%0A"
    echo -n "the rendered code cities, then rebase your branch to either remove these%0A"
    echo -n "commits or modify them so that the relevant scene remains unchanged.%0A"
    echo "If you are unsure what to do, please use the ~Entwicklung channel in Mattermost."
  fi
  exit 1
fi
exit 0
