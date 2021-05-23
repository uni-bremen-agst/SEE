#if UNITY_EDITOR

using SEE.Game;
using UnityEditor;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// Provides editing for <see cref="DataPath"/>.
    /// </summary>
    public static class DataPathEditor
    {
        /// <summary>
        /// Adds controls to set the attributes of <paramref name="dataPath"/>.
        /// </summary>
        /// <param name="label">a label in front of the controls shown in the inspector</param>
        /// <param name="dataPath">the path to be set here</param>
        /// <param name="extension">the extension the selected file should have (used as filter in file panel)</param>
        /// <param name="fileDialogue">if true, a file panel is opened; otherwise a directory panel</param>
        /// <returns>the resulting data specified as selected  by the user</returns>
        public static DataPath GetDataPath(string label, DataPath dataPath, string extension = "", bool fileDialogue = true)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label);

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            dataPath.Root = (DataPath.RootKind)EditorGUILayout.EnumPopup(dataPath.Root, GUILayout.Width(100));
            if (dataPath.Root == DataPath.RootKind.Absolute)
            {
                dataPath.AbsolutePath = EditorGUILayout.TextField(dataPath.AbsolutePath);
            }
            else
            {
                dataPath.RelativePath = EditorGUILayout.TextField(dataPath.RelativePath);
            }
            if (GUILayout.Button("...", GUILayout.Width(20)))
            {
                string selectedPath = fileDialogue ?
                      EditorUtility.OpenFilePanel("Select file", dataPath.RootPath, extension)
                    : EditorUtility.OpenFolderPanel("Select directory", dataPath.RootPath, extension);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    dataPath.Set(selectedPath);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(dataPath.Path);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            return dataPath;
        }
    }
}

#endif