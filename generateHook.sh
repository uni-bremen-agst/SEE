#!/bin/sh
# This script is intended for POSIX environments, e.g. Linux or Mac OS. 
# For Windows, please use the corresponding '.bat' script.
# This will install a pre-commit hook which verifies that no code cities
# were left in the scene. Call it by simply executing './generateHook.sh'.

cat <<EOT > .git/hooks/pre-commit
#!/bin/sh
# This pre-commit hook verifies whether any drawn code cities have
# been left in the Assets/Scene folder. Only the diff for the attempted
# commit will be looked at, so existing code cities will be ignored.
# Only staged (to be committed) changes will be looked at.

output=""
while IFS= read -r scene
do
        if [ -n "\$scene" ]; then
                output="\$output
Warnung: Ungelöschte CodeCity-Knoten erkannt in Szene '\$scene'!"
        fi
done << EOF
\$(git diff --staged -S"m_TagString: Node" --name-only Assets/Scenes 2> /dev/null)
EOF

if [ -n "\$output" ]; then
        printf '%s\nBitte alle gezeichneten CodeCities vor dem Commit löschen.' "\$output"
        exit 1
fi
exit 0

EOT
