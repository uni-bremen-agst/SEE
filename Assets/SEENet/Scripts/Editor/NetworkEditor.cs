using UnityEditor;
using UnityEngine;

namespace SEE.Net.Internal
{

    [CustomEditor(typeof(Network))]
    public class NetworkEditor : Editor
    {
        private bool showInfos = true;
        private bool showSettings = true;
        private bool showDebug = true;

        public override void OnInspectorGUI()
        {
            SerializedProperty serverIPAddress = serializedObject.FindProperty("serverIPAddress");
            SerializedProperty serverPort = serializedObject.FindProperty("serverPort");
            SerializedProperty useInOfflineMode = serializedObject.FindProperty("useInOfflineMode");
            SerializedProperty hostServer = serializedObject.FindProperty("hostServer");
            SerializedProperty loggingEnabled = serializedObject.FindProperty("loggingEnabled");
            SerializedProperty minimalSeverity = serializedObject.FindProperty("minimalSeverity");

            // Infos
            showInfos = EditorGUILayout.BeginFoldoutHeaderGroup(showInfos, "Infos");
            if (showInfos)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    System.Net.IPAddress[] ipAddresses = Network.LookupLocalIPAddresses();
                    foreach (System.Net.IPAddress ipAddress in ipAddresses)
                    {
                        EditorGUILayout.LabelField(ipAddress.AddressFamily.ToString(), ipAddress.ToString());
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Settings
            showSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showSettings, "Settings");
            if (showSettings)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(useInOfflineMode);
                    EditorGUI.BeginDisabledGroup(useInOfflineMode.boolValue);
                    {
                        EditorGUILayout.PropertyField(hostServer);
                        EditorGUI.BeginDisabledGroup(hostServer.boolValue);
                        {
                            EditorGUILayout.PropertyField(serverIPAddress, new GUIContent("Remote IP-Address", "The IP-Address of the remote server."));
                            EditorGUILayout.PropertyField(serverPort, new GUIContent("Remote Server Port", "The Port of the remote server."));
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Debug
            showDebug = EditorGUILayout.BeginFoldoutHeaderGroup(showDebug, "Debug");
            if (showDebug)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUI.BeginDisabledGroup(useInOfflineMode.boolValue);
                    {
                        EditorGUILayout.PropertyField(loggingEnabled);
                        EditorGUI.BeginDisabledGroup(!loggingEnabled.boolValue);
                        {
                            EditorGUILayout.PropertyField(minimalSeverity);
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

}
