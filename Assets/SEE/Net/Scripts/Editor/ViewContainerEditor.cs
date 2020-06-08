#if UNITY_EDITOR

using SEE.Net;
using UnityEditor;

namespace SEEEditor
{

    [CustomEditor(typeof(ViewContainer))]
    public class ViewContainerEditor : Editor
    {
        public bool showInfos = true;
        public bool showSettings = true;
        public bool isFocused = false;

        public override void OnInspectorGUI()
        {
            // Infos
            showInfos = EditorGUILayout.BeginFoldoutHeaderGroup(showInfos, "Infos");
            if (showInfos)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    ViewContainer viewContainer = (ViewContainer)target;
                    bool useInOfflineMode = Network.UseInOfflineMode;
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
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("views"), true);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

}

#endif
