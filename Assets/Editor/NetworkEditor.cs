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
            // Infos (all read-only)
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
                    SetProperty("GameScene", "Loaded Scene", "The name of the scene to be loaded when the game is started.");
                    SetProperty("loadCityOnStart", "Load City On Start", "Whether the city should be loaded on start of the application.");
                    SetProperty("ServerActionPort", "Server Action Port", "The port of the server where it listens to SEE actions.");
                    SetProperty("VoiceChat", "Voice Chat System", "The kind of voice chat to be enabled (None for no voice chat).");
                    SetProperty("vivoxChannelName", "Vivox Channel Name", "The name of the voice channel.");
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Logging
            showLogging = EditorGUILayout.BeginFoldoutHeaderGroup(showLogging, "Logging");
            if (showLogging)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    SetProperty("internalLoggingEnabled", "Internal", "Whether the internal logging should be enabled.");

                    SerializedProperty networkCommsLoggingEnabled
                        = SetProperty("networkCommsLoggingEnabled", "NetworkComms", "Whether the logging of NetworkComms should be enabled.");
                    EditorGUI.BeginDisabledGroup(networkCommsLoggingEnabled != null && !networkCommsLoggingEnabled.boolValue);
                    {
                        SetProperty("minimalSeverity", "NetworkComms Severity", "The minimal logged severity.");
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Allows the user to set the property of the <see cref="SEE.Net.Network"/> instance
        /// edited by this <see cref="NetworkEditor"/>.
        /// </summary>
        /// <param name="propertyName">name of the property</param>
        /// <param name="label">label to be shown to the user</param>
        /// <param name="toolTip">tool tip to be shown to the user</param>
        /// <returns>the property with <paramref name="propertyName"/> if any, otherwise null</returns>
        private SerializedProperty SetProperty(string propertyName, string label, string toolTip)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label, toolTip));
            }
            else
            {
                Debug.LogError($"Property {propertyName} does not exist for {typeof(SEE.Net.Network)}.\n");
            }
            return property;
        }
    }
}

#endif
