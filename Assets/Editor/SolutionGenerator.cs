#if UNITY_EDITOR
using System;
using Unity.CodeEditor;

namespace CITools
{
    /// <summary>
    /// This class can be used to generate solution and csproj files for Visual Studio.
    /// </summary>
    public static class SolutionGenerator
    {
        /// <summary>
        /// Creates/synchronizes all solution and csproj files for Visual Studio.
        /// </summary>
        public static void Sync()
        {
            try
            {
                IExternalCodeEditor currentCodeEditor = Unity.CodeEditor.CodeEditor.Editor.CurrentCodeEditor;
                if (currentCodeEditor == null) 
                {
                    Console.WriteLine("Error: There is no code editor set currently.");
                    return;
                }
                Console.WriteLine("Current editor is: " + currentCodeEditor.GetType().Name);
                UnityEditor.AssetDatabase.Refresh();
                currentCodeEditor.SyncAll();

                // The following code suggested at
                // https://forum.unity.com/threads/how-can-i-generate-csproj-files-during-continuous-integration-builds.1116448/
                // does not work. An exception will be thrown because one of the methods assigns
                // 'default' to an output parameter of type StringBuilder, which is a class.
                // The default of a reference type is null. This looks like a bug.
                //Microsoft.Unity.VisualStudio.Editor.ProjectGeneration projectGeneration = new();
                //projectGeneration.GenerateAndWriteSolutionAndProjects();

                Console.WriteLine("Created .sln and .csproj files successfully.");
            }
            catch (Exception e) 
            { 
                Console.WriteLine("Exception thrown: " + e.Message);
                throw;
            }
        }
    }
}

#endif
