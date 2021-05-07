using UnityEngine;
using UnityEditor;

namespace Michsky.UI.ModernUIPack
{
    [CustomEditor(typeof(WindowManager))]
    public class WindowManagerEditor : Editor
    {
        private WindowManager wmTarget;
        private int currentTab;

        private void OnEnable()
        {
            wmTarget = (WindowManager)target;
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

            GUILayout.Box(new GUIContent(""), customSkin.FindStyle("WM Top Header"));

            GUILayout.EndHorizontal();
            GUILayout.Space(-42);

            GUIContent[] toolbarTabs = new GUIContent[2];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Settings");

            GUILayout.BeginHorizontal();
            GUILayout.Space(17);

            currentTab = GUILayout.Toolbar(currentTab, toolbarTabs, customSkin.FindStyle("Tab Indicator"));

            GUILayout.EndHorizontal();
            GUILayout.Space(-40);
            GUILayout.BeginHorizontal();
            GUILayout.Space(17);

            if (GUILayout.Button(new GUIContent("Content", "Content"), customSkin.FindStyle("Tab Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab Settings")))
                currentTab = 1;

            GUILayout.EndHorizontal();

            var windows = serializedObject.FindProperty("windows");
            var currentWindowIndex = serializedObject.FindProperty("currentWindowIndex");
            var windowFadeIn = serializedObject.FindProperty("windowFadeIn");
            var windowFadeOut = serializedObject.FindProperty("windowFadeOut");
            var buttonFadeIn = serializedObject.FindProperty("buttonFadeIn");
            var buttonFadeOut = serializedObject.FindProperty("buttonFadeOut");
            var editMode = serializedObject.FindProperty("editMode");

            switch (currentTab)
            {
                case 0:
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUI.indentLevel = 1;

                    EditorGUILayout.PropertyField(windows, new GUIContent("Window Items"), true);
                    windows.isExpanded = true;

                    GUILayout.EndHorizontal();

                    if (GUILayout.Button("+  Add a new window", customSkin.button))
                        wmTarget.AddNewItem();

                    break;

                case 1:
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Window In Anim"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(windowFadeIn, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Window Out Anim"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(windowFadeOut, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Button In Anim"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(buttonFadeIn, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Button Out Anim"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(buttonFadeOut, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    editMode.boolValue = GUILayout.Toggle(editMode.boolValue, new GUIContent("Edit Mode"), customSkin.FindStyle("Toggle"));
                    editMode.boolValue = GUILayout.Toggle(editMode.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();

                    if (wmTarget.windows.Count != 0)
                    {
                        GUILayout.BeginVertical(EditorStyles.helpBox);

                        EditorGUILayout.LabelField(new GUIContent("Selected Window:"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                        currentWindowIndex.intValue = EditorGUILayout.IntSlider(currentWindowIndex.intValue, 0, wmTarget.windows.Count - 1);

                        GUILayout.Space(2);
                        EditorGUILayout.LabelField(new GUIContent(wmTarget.windows[currentWindowIndex.intValue].windowName), customSkin.FindStyle("Text"));

                        if (editMode.boolValue == true)
                        {
                            EditorGUILayout.HelpBox("While Edit Mode is enabled, you can change the visibility of window objects by changing the slider value.", MessageType.Info);

                            for (int i = 0; i < wmTarget.windows.Count; i++)
                            {
                                if (i == currentWindowIndex.intValue)
                                    wmTarget.windows[currentWindowIndex.intValue].windowObject.GetComponent<CanvasGroup>().alpha = 1;
                                else
                                    wmTarget.windows[i].windowObject.GetComponent<CanvasGroup>().alpha = 0;
                            }
                        }

                        GUILayout.EndVertical();
                    }

                    else
                        EditorGUILayout.HelpBox("Window List is empty. Create a new item to see more options.", MessageType.Info);

                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}