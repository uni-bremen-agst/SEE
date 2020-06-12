REM Generates the .csproj files required to build the project.
REM This script must be executed in the root Unity project folder.
REM Unity.exe must be contained in the path.
REM On Rainer's PC, for instance, it can be found at F:\Programme\Unity\2019.3.14f1\Editor\.

Unity.exe -batchmode -nographics -logFile - -executeMethod UnityEditor.SyncVS.SyncSolution -projectPath . -quit

