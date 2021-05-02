using UnityEngine;
using UnityEditor;

namespace Michsky.UI.ModernUIPack
{
    [CustomEditor(typeof(NotificationManager))]
    public class NotificationManagerEditor : Editor
    {
        private NotificationManager ntfTarget;
        private int currentTab;

        private void OnEnable()
        {
            ntfTarget = (NotificationManager)target;
        }

        public override void OnInspectorGUI()
        {
            GUISkin customSkin;
            Color defaultColor = GUI.color;

            if (EditorGUIUtility.isProSkin == true)
                customSkin = (GUISkin)Resources.Load("Editor\\MUI Skin Dark");
            else
                customSkin = (GUISkin)Resources.Load("Editor\\MUI Skin Light");

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = defaultColor;

            GUILayout.Box(new GUIContent(""), customSkin.FindStyle("Notification Top Header"));

            GUILayout.EndHorizontal();
            GUILayout.Space(-42);

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Resources");
            toolbarTabs[2] = new GUIContent("Settings");

            GUILayout.BeginHorizontal();
            GUILayout.Space(17);

            currentTab = GUILayout.Toolbar(currentTab, toolbarTabs, customSkin.FindStyle("Tab Indicator"));

            GUILayout.EndHorizontal();
            GUILayout.Space(-40);
            GUILayout.BeginHorizontal();
            GUILayout.Space(17);

            if (GUILayout.Button(new GUIContent("Content", "Content"), customSkin.FindStyle("Tab Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab Resources")))
                currentTab = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab Settings")))
                currentTab = 2;

            GUILayout.EndHorizontal();

            var icon = serializedObject.FindProperty("icon");
            var title = serializedObject.FindProperty("title");
            var description = serializedObject.FindProperty("description");
            var notificationAnimator = serializedObject.FindProperty("notificationAnimator");
            var iconObj = serializedObject.FindProperty("iconObj");
            var titleObj = serializedObject.FindProperty("titleObj");
            var descriptionObj = serializedObject.FindProperty("descriptionObj");
            var enableTimer = serializedObject.FindProperty("enableTimer");
            var timer = serializedObject.FindProperty("timer");
            var notificationStyle = serializedObject.FindProperty("notificationStyle");
            var useCustomContent = serializedObject.FindProperty("useCustomContent");
            var useStacking = serializedObject.FindProperty("useStacking");
            var destroyAfterPlaying = serializedObject.FindProperty("destroyAfterPlaying");

            switch (currentTab)
            {
                case 0:
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Icon"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(icon, new GUIContent(""));

                    GUILayout.EndHorizontal();

                    if (ntfTarget.iconObj != null)
                        ntfTarget.iconObj.sprite = ntfTarget.icon;

                    else
                    {
                        if (ntfTarget.iconObj == null)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.HelpBox("'Icon Object' is not assigned. Go to Resources tab and assign the correct variable.", MessageType.Error);
                            GUILayout.EndHorizontal();
                        }
                    }

                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Title"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(title, new GUIContent(""));

                    GUILayout.EndHorizontal();

                    if (ntfTarget.titleObj != null)
                        ntfTarget.titleObj.text = title.stringValue;

                    else
                    {
                        if (ntfTarget.titleObj == null)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.HelpBox("'Title Object' is not assigned. Go to Resources tab and assign the correct variable.", MessageType.Error);
                            GUILayout.EndHorizontal();
                        }
                    }

                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Description"), customSkin.FindStyle("Text"), GUILayout.Width(-3));
                    EditorGUILayout.PropertyField(description, new GUIContent(""), GUILayout.Height(50));

                    GUILayout.EndHorizontal();

                    if (ntfTarget.descriptionObj != null)
                        ntfTarget.descriptionObj.text = description.stringValue;

                    else
                    {
                        if (ntfTarget.descriptionObj == null)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.HelpBox("'Description Object' is not assigned. Go to Resources tab and assign the correct variable.", MessageType.Error);
                            GUILayout.EndHorizontal();
                        }
                    }

                    break;

                case 1:
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Animator"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(notificationAnimator, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Icon Object"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(iconObj, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Title Object"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(titleObj, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Description Object"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(descriptionObj, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    break;

                case 2:
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    useCustomContent.boolValue = GUILayout.Toggle(useCustomContent.boolValue, new GUIContent("Use Custom Content"), customSkin.FindStyle("Toggle"));
                    useCustomContent.boolValue = GUILayout.Toggle(useCustomContent.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    useStacking.boolValue = GUILayout.Toggle(useStacking.boolValue, new GUIContent("Use Stacking"), customSkin.FindStyle("Toggle"));
                    useStacking.boolValue = GUILayout.Toggle(useStacking.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    destroyAfterPlaying.boolValue = GUILayout.Toggle(destroyAfterPlaying.boolValue, new GUIContent("Destroy After Playing"), customSkin.FindStyle("Toggle"));
                    destroyAfterPlaying.boolValue = GUILayout.Toggle(destroyAfterPlaying.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    enableTimer.boolValue = GUILayout.Toggle(enableTimer.boolValue, new GUIContent("Enable Timer"), customSkin.FindStyle("Toggle"));
                    enableTimer.boolValue = GUILayout.Toggle(enableTimer.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();

                    if (enableTimer.boolValue == true)
                    {
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);

                        EditorGUILayout.LabelField(new GUIContent("Timer"), customSkin.FindStyle("Text"), GUILayout.Width(120));

                        EditorGUILayout.PropertyField(timer, new GUIContent(""));
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Notification Style"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(notificationStyle, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}