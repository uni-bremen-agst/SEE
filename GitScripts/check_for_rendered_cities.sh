#!/bin/sh
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
while IFS= read -r scene
do
        if [ -n "$scene" ]; then
                output="$output
Warning: Rendered CodeCities detected in scene '$scene'!"
        fi
done << EOF
$($DIFF_COMMAND -S"m_TagString: Node" --name-only Assets/Scenes 2> /dev/null)
EOF

if [ -n "$output" ]; then
        printf '%s\nPlease delete all drawn CodeCities before committing.' "$output"
        if [ -n "$CI" ]; then
            echo ""
            echo "! NOTE !"
            echo "You have already pushed these commits to the repository."
            echo "Before merging into master, identify the commits which have introduced"
            echo "the rendered code cities, then rebase your branch to either remove these"
            echo "commits or modify them so that the relevant scene remains unchanged."
            echo "If you are unsure what to do, please use the ~Entwicklung channel in Mattermost."
        fi
        exit 1
fi
exit 0
