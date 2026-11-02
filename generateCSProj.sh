#!/bin/sh
# Generates the .csproj files required to build the project.
# This script must be executed in the root Unity project folder.
# Unity must be contained in the path.
# The UNITY_PATH variable must be set prior or with the exectution of this script
# On Rainer's PC, for instance, it can be found at F:\Programme\Unity\2019.3.14f1\Editor\.
# In the default Unity Folder Structure for GNU/Linux the Path for Unity version 6000.0.67f1 is ~/Unity/Hub/Editor/6000.0.67f1/Editor/Unity
#
# As stated in the wiki, a viable command would be:
# UNITY_PATH=~/Unity/Hub/Editor/6000.0.67f1/Editor/Unity ./generateCSProj.sh

$UNITY_PATH -batchmode -nographics -logFile - -executeMethod UnityEditor.SyncVS.SyncSolution -projectPath . -quit
