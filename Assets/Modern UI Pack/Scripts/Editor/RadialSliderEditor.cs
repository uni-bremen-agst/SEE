using UnityEngine;
using UnityEditor;

namespace Michsky.UI.ModernUIPack
{
    [CustomEditor(typeof(RadialSlider))]
    public class RadialSliderEditor : Editor
    {
        private RadialSlider rsTarget;
        private int currentTab;

        private void OnEnable()
        {
            rsTarget = (RadialSlider)target;
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

            GUILayout.Box(new GUIContent(""), customSkin.FindStyle("Slider Top Header"));

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

            var currentValue = serializedObject.FindProperty("currentValue");
            var onValueChanged = serializedObject.FindProperty("onValueChanged");
            var onPointerEnter = serializedObject.FindProperty("onPointerEnter");
            var onPointerExit = serializedObject.FindProperty("onPointerExit");
            var sliderImage = serializedObject.FindProperty("sliderImage");
            var indicatorPivot = serializedObject.FindProperty("indicatorPivot");
            var valueText = serializedObject.FindProperty("valueText");
            var rememberValue = serializedObject.FindProperty("rememberValue");
            var sliderTag = serializedObject.FindProperty("sliderTag");
            var minValue = serializedObject.FindProperty("minValue");
            var maxValue = serializedObject.FindProperty("maxValue");
            var isPercent = serializedObject.FindProperty("isPercent");
            var decimals = serializedObject.FindProperty("decimals");

            // Draw content depending on tab index
            switch (currentTab)
            {
                case 0:
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Current Value"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    currentValue.floatValue = EditorGUILayout.Slider(currentValue.floatValue, rsTarget.minValue, rsTarget.maxValue);

                    GUILayout.EndHorizontal();

                    if(rsTarget.sliderImage != null && rsTarget.indicatorPivot != null && rsTarget.valueText != null)
                    {
                        rsTarget.SliderValueRaw = currentValue.floatValue;
                        float normalizedAngle = rsTarget.SliderAngle / 360.0f;
                        rsTarget.indicatorPivot.transform.localEulerAngles = new Vector3(180.0f, 0.0f, rsTarget.SliderAngle);
                        rsTarget.sliderImage.fillAmount = normalizedAngle;
                        rsTarget.valueText.text = string.Format("{0}{1}", currentValue.floatValue, rsTarget.isPercent ? "%" : "");
                    }

                    GUILayout.Space(10);

                    EditorGUILayout.PropertyField(onValueChanged, new GUIContent("On Value Changed"), true);
                    EditorGUILayout.PropertyField(onPointerEnter, new GUIContent("On Pointer Enter"), true);
                    EditorGUILayout.PropertyField(onPointerExit, new GUIContent("On Pointer Exit"), true);
                    break;

                case 1:
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Slider Image"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(sliderImage, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Indicator Pivot"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(indicatorPivot, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Indicator Text"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(valueText, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    break;

                case 2:
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Min Value"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(minValue, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Max Value"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(maxValue, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Decimals"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(decimals, new GUIContent(""));

                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    isPercent.boolValue = GUILayout.Toggle(isPercent.boolValue, new GUIContent("Is Percent"), customSkin.FindStyle("Toggle"));
                    isPercent.boolValue = GUILayout.Toggle(isPercent.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    rememberValue.boolValue = GUILayout.Toggle(rememberValue.boolValue, new GUIContent("Save Value"), customSkin.FindStyle("Toggle"));
                    rememberValue.boolValue = GUILayout.Toggle(rememberValue.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();

                    if (rememberValue.boolValue == true)
                    {
                        EditorGUI.indentLevel = 2;
                        GUILayout.BeginHorizontal();

                        EditorGUILayout.LabelField(new GUIContent("Tag:"), customSkin.FindStyle("Text"), GUILayout.Width(40));
                        EditorGUILayout.PropertyField(sliderTag, new GUIContent(""));

                        GUILayout.EndHorizontal();
                        EditorGUI.indentLevel = 0;
                        GUILayout.Space(2);
                        EditorGUILayout.HelpBox("Each slider should has its own unique tag.", MessageType.Info);
                    }
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}