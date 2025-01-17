#!/bin/sh
# Updates the Unity version across the project if it is changed in ProjectVersion.txt.

if git diff --staged --name-only | grep -q 'ProjectSettings/ProjectVersion.txt'; then
  VERSION=$(grep '^m_EditorVersion:' ProjectSettings/ProjectVersion.txt | cut -c 18-)
  sed -Ei "s/60[0-9]+\.[0-9]+\.[0-9f]+/$VERSION/" README.md Axivion/axivion-jenkins.bat
  git add README.md Axivion/axivion-jenkins.bat
fi
