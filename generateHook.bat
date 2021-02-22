@ECHO OFF
REM This script is intended for Windows environments.
REM For POSIX environments (e.g. Linux/MacOS), please use the corresponding '.sh' script.
REM This will install a pre-commit hook which verifies that no code cities
REM were left in the scene. Call it by simply executing './setup_hook.bat'.

ECHO Installing pre-commit hook...

(
ECHO #!/bin/sh
ECHO # This pre-commit hook verifies whether any drawn code cities have
ECHO # been left in the Assets/Scene folder. Only the diff for the attempted
ECHO # commit will be looked at, so existing code cities will be ignored.
ECHO # Only staged (to be committed^) changes will be looked at.
ECHO ^output=""
ECHO while IFS= read -r scene
ECHO do
ECHO         if [ -n "$scene" ]; then
ECHO                 output="$output
ECHO Warning: Rendered CodeCities detected in scene '$scene'!"
ECHO         fi
ECHO done ^<^< EOF
ECHO $(git diff --staged -S"m_TagString: Node" --name-only Assets/Scenes 2^> /dev/null^)
ECHO EOF
ECHO if [ -n "$output" ]; then
ECHO         printf '%%s\nPlease delete all drawn CodeCities before committing.' "$output"
ECHO         exit 1
ECHO fi
ECHO exit 0
)>".git/hooks/pre-commit"

ECHO Pre-commit hook successfully installed!
