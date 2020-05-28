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
            SerializedProperty localServerPort = serializedObject.FindProperty("localServerPort");
            SerializedProperty remoteServerPort = serializedObject.FindProperty("remoteServerPort");
            SerializedProperty loadCityOnStart = serializedObject.FindProperty("loadCityOnStart");
            SerializedProperty loadCityGameObject = serializedObject.FindProperty("loadCityGameObject");
            SerializedProperty useInOfflineMode = serializedObject.FindProperty("useInOfflineMode");
            SerializedProperty hostServer = serializedObject.FindProperty("hostServer");
            SerializedProperty nativeLoggingEnabled = serializedObject.FindProperty("nativeLoggingEnabled");
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
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUI.BeginDisabledGroup(!useInOfflineMode.boolValue && !hostServer.boolValue);
                    {
                        EditorGUILayout.PropertyField(loadCityOnStart, new GUIContent("Load City On Start", "Whether the city should be loaded on start of the application."));
                        EditorGUI.BeginDisabledGroup(!loadCityOnStart.boolValue);
                        {
                            EditorGUILayout.PropertyField(loadCityGameObject, new GUIContent("City Loading GameObject", "If the given GameObject contains some AbstractSEECity-script, the defined city can be built for each client."));
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUI.BeginDisabledGroup(useInOfflineMode.boolValue);
                    {
                        EditorGUI.BeginDisabledGroup(!hostServer.boolValue);
                        {
                            EditorGUILayout.PropertyField(localServerPort, new GUIContent("Local Server Port", "The Port of the local server."));
                        }
                        EditorGUI.EndDisabledGroup();

                        EditorGUI.BeginDisabledGroup(hostServer.boolValue);
                        {
                            EditorGUILayout.PropertyField(serverIPAddress, new GUIContent("Remote IP-Address", "The IP-Address of the remote server."));
                            EditorGUILayout.PropertyField(remoteServerPort, new GUIContent("Remote Server Port", "The Port of the remote server."));
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
                        EditorGUILayout.PropertyField(nativeLoggingEnabled);
                        EditorGUI.BeginDisabledGroup(!nativeLoggingEnabled.boolValue);
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
