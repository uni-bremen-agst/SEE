#!/bin/sh
~/Unity/Hub/Editor/6000.0.67f1/Editor/Unity -batchmode -nographics -logFile - -executeMethod UnityEditor.SyncVS.SyncSolution -projectPath . -quit

