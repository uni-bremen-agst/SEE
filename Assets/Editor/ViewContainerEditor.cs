#if UNITY_EDITOR

using SEE.Net;
using UnityEditor;
using UnityEngine;

namespace SEEEditor
{

    /// <summary>
    /// Custom editor for a <see cref="ViewContainer"/>.
    /// </summary>
    [CustomEditor(typeof(ViewContainer))]
    public class ViewContainerEditor : Editor
    {
        /// <summary>
        /// Whether infos should be displayed.
        /// </summary>
        private bool showInfos = false;

        /// <summary>
        /// Whether setting should be displayed.
        /// </summary>
        private bool showSettings = false;

        public override void OnInspectorGUI()
        {
            // Infos
            showInfos = EditorGUILayout.BeginFoldoutHeaderGroup(showInfos, "Infos");
            if (showInfos)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    ViewContainer viewContainer = (ViewContainer)target;
                    bool useInOfflineMode = SEE.Net.Network.UseInOfflineMode;
                    string defaultMessage = useInOfflineMode ? "Unset in offline mode" : "Set at runtime";

                    EditorGUILayout.LabelField("ID", useInOfflineMode || viewContainer.id == ViewContainer.InvalidID ? defaultMessage : viewContainer.id.ToString());
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Owner IPV6-Address", useInOfflineMode || viewContainer.owner == null ? defaultMessage : viewContainer.owner.Address.ToString());
                    EditorGUILayout.LabelField("Owner Port", useInOfflineMode || viewContainer.owner == null ? defaultMessage : viewContainer.owner.Port.ToString());
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Settings
            showSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showSettings, "Settings");
            if (showSettings)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    SerializedProperty views = serializedObject.FindProperty("views");
                    if (views != null)
                    {
                        EditorGUILayout.PropertyField(views, true);
                    }
                    else
                    {
                        Debug.LogError("There is no property 'views'.\n");
                        showSettings = false;
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

#endif
