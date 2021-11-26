#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace SEEEditor
{

    [CustomEditor(typeof(SEE.Net.Network))]
    public class NetworkEditor : Editor
    {
        /// <summary>
        /// Whether infos should be displayed.
        /// </summary>
        public bool showInfos = true;

        /// <summary>
        /// Whether setting should be displayed.
        /// </summary>
        public bool showSettings = true;

        /// <summary>
        /// Whether logging-info should be displayed.
        /// </summary>
        private bool showLogging = true;

        public override void OnInspectorGUI()
        {
            SerializedProperty remoteServerIPAddress = serializedObject.FindProperty("remoteServerIPAddress");
            SerializedProperty localServerPort = serializedObject.FindProperty("localServerPort");
            SerializedProperty remoteServerPort = serializedObject.FindProperty("remoteServerPort");
            SerializedProperty loadCityOnStart = serializedObject.FindProperty("loadCityOnStart");
            SerializedProperty useInOfflineMode = serializedObject.FindProperty("useInOfflineMode");
            SerializedProperty hostServer = serializedObject.FindProperty("hostServer");
            SerializedProperty networkCommsLoggingEnabled = serializedObject.FindProperty("networkCommsLoggingEnabled");
            SerializedProperty internalLoggingEnabled = serializedObject.FindProperty("internalLoggingEnabled");
            SerializedProperty minimalSeverity = serializedObject.FindProperty("minimalSeverity");

            // Infos
            showInfos = EditorGUILayout.BeginFoldoutHeaderGroup(showInfos, "Infos");
            if (showInfos)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    System.Net.IPAddress[] ipAddresses = SEE.Net.Network.LookupLocalIPAddresses();
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
                            EditorGUILayout.PropertyField(remoteServerIPAddress, new GUIContent("Remote IP-Address", "The IP-Address of the remote server."));
                            EditorGUILayout.PropertyField(remoteServerPort, new GUIContent("Remote Server Port", "The Port of the remote server."));
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUI.EndDisabledGroup();

                    #region Vivox
                    EditorGUI.BeginDisabledGroup(useInOfflineMode.boolValue);
                    {
                        SerializedProperty enableVivox = serializedObject.FindProperty("enableVivox");
                        SerializedProperty vivoxChannelName = serializedObject.FindProperty("vivoxChannelName");
                        EditorGUILayout.PropertyField(enableVivox, new GUIContent("Enable Vivox", "Whether Vivox voice chat is to be enabled."));
                        EditorGUILayout.PropertyField(vivoxChannelName, new GUIContent("Vivox Channel Name", "The name of the voice channel."));
                    }
                    EditorGUI.EndDisabledGroup();
                    #endregion
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Logging
            showLogging = EditorGUILayout.BeginFoldoutHeaderGroup(showLogging, "Logging");
            if (showLogging)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUI.BeginDisabledGroup(useInOfflineMode.boolValue);
                    {
                        EditorGUILayout.PropertyField(internalLoggingEnabled, new GUIContent("Internal"));

                        EditorGUILayout.PropertyField(networkCommsLoggingEnabled, new GUIContent("NetworkComms"));
                        EditorGUI.BeginDisabledGroup(!networkCommsLoggingEnabled.boolValue);
                        {
                            EditorGUILayout.PropertyField(minimalSeverity, new GUIContent("NetworkComms Severity"));
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

#endif
