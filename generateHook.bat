@ECHO OFF
REM This script is intended for Windows environments.
REM For POSIX environments (e.g. Linux/MacOS), please use the corresponding '.sh' script.
REM This will install a pre-commit hook which verifies that no code cities
REM were left in the scene. Call it by simply executing './setup_hook.bat'.

ECHO Installing pre-commit hook...

COPY GitScripts\run_all.sh .git\hooks\pre-commit

ECHO Pre-commit hook successfully installed!
