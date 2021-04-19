using UnityEngine;
using UnityEditor;

namespace Michsky.UI.ModernUIPack
{
    [CustomEditor(typeof(ProgressBar))]
    public class ProgressBarEditor : Editor
    {
        private ProgressBar pbTarget;
        private int currentTab;

        private void OnEnable()
        {
            pbTarget = (ProgressBar)target;
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

            GUILayout.Box(new GUIContent(""), customSkin.FindStyle("PB Top Header"));

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

            var currentPercent = serializedObject.FindProperty("currentPercent");
            var speed = serializedObject.FindProperty("speed");
            var maxValue = serializedObject.FindProperty("maxValue");
            var loadingBar = serializedObject.FindProperty("loadingBar");
            var textPercent = serializedObject.FindProperty("textPercent");
            var isOn = serializedObject.FindProperty("isOn");
            var restart = serializedObject.FindProperty("restart");
            var invert = serializedObject.FindProperty("invert");
            var isPercent = serializedObject.FindProperty("isPercent");

            switch (currentTab)
            {
                case 0:
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Current Percent"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    pbTarget.currentPercent = EditorGUILayout.Slider(pbTarget.currentPercent, 0, pbTarget.maxValue);

                    GUILayout.EndHorizontal();

                    if (pbTarget.loadingBar != null && pbTarget.textPercent != null)
                    {
                        pbTarget.loadingBar.fillAmount = pbTarget.currentPercent / pbTarget.maxValue;
                       
                        if (isPercent.boolValue == true)
                            pbTarget.textPercent.text = ((int)pbTarget.currentPercent).ToString("F0") + "%";
                        else
                            pbTarget.textPercent.text = ((int)pbTarget.currentPercent).ToString("F0");
                    }

                    else
                    {
                        if (pbTarget.loadingBar == null || pbTarget.textPercent == null)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.HelpBox("Some resources are not assigned. Go to Resources tab and assign the correct variable.", MessageType.Error);
                            GUILayout.EndHorizontal();
                        }
                    }

                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Speed"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(speed, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Max Value"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(maxValue, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    break;

                case 1:
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Loading Bar"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(loadingBar, new GUIContent(""));

                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Text Indicator"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(textPercent, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    break;

                case 2:
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    isOn.boolValue = GUILayout.Toggle(isOn.boolValue, new GUIContent("Is On"), customSkin.FindStyle("Toggle"));
                    isOn.boolValue = GUILayout.Toggle(isOn.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();                 
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    restart.boolValue = GUILayout.Toggle(restart.boolValue, new GUIContent("Restart"), customSkin.FindStyle("Toggle"));
                    restart.boolValue = GUILayout.Toggle(restart.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    invert.boolValue = GUILayout.Toggle(invert.boolValue, new GUIContent("Invert"), customSkin.FindStyle("Toggle"));
                    invert.boolValue = GUILayout.Toggle(invert.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    isPercent.boolValue = GUILayout.Toggle(isPercent.boolValue, new GUIContent("Is Percent"), customSkin.FindStyle("Toggle"));
                    isPercent.boolValue = GUILayout.Toggle(isPercent.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}