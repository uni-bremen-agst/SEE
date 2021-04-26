﻿using UnityEngine;
using UnityEditor;

namespace Michsky.UI.ModernUIPack
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ButtonManagerBasicWithIcon))]
    public class ButtonManagerBasicWithIconEditor : Editor
    {
        private ButtonManagerBasicWithIcon buttonTarget;
        private int currentTab;

        private void OnEnable()
        {
            buttonTarget = (ButtonManagerBasicWithIcon)target;
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

            GUILayout.Box(new GUIContent(""), customSkin.FindStyle("Button Top Header"));

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

            var buttonText = serializedObject.FindProperty("buttonText");
            var buttonIcon = serializedObject.FindProperty("buttonIcon");
            var clickEvent = serializedObject.FindProperty("clickEvent");
            var hoverEvent = serializedObject.FindProperty("hoverEvent");
            var normalText = serializedObject.FindProperty("normalText");
            var normalImage = serializedObject.FindProperty("normalImage");
            var useCustomContent = serializedObject.FindProperty("useCustomContent");
            var enableButtonSounds = serializedObject.FindProperty("enableButtonSounds");
            var useHoverSound = serializedObject.FindProperty("useHoverSound");
            var useClickSound = serializedObject.FindProperty("useClickSound");
            var soundSource = serializedObject.FindProperty("soundSource");
            var hoverSound = serializedObject.FindProperty("hoverSound");
            var clickSound = serializedObject.FindProperty("clickSound");
            var rippleParent = serializedObject.FindProperty("rippleParent");
            var useRipple = serializedObject.FindProperty("useRipple");
            var renderOnTop = serializedObject.FindProperty("renderOnTop");
            var centered = serializedObject.FindProperty("centered");
            var rippleShape = serializedObject.FindProperty("rippleShape");
            var speed = serializedObject.FindProperty("speed");
            var maxSize = serializedObject.FindProperty("maxSize");
            var startColor = serializedObject.FindProperty("startColor");
            var transitionColor = serializedObject.FindProperty("transitionColor");

            switch (currentTab)
            {
                case 0:
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Button Text"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(buttonText, new GUIContent(""));

                    GUILayout.EndHorizontal();

                    if (useCustomContent.boolValue == false && buttonTarget.normalText != null)
                        buttonTarget.normalText.text = buttonText.stringValue;

                    else if (useCustomContent.boolValue == false && buttonTarget.normalText == null)
                    {
                        GUILayout.Space(2);
                        EditorGUILayout.HelpBox("'Text Object' is not assigned. Go to Resources tab and assign the correct variable.", MessageType.Error);
                    }

                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Button Icon"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(buttonIcon, new GUIContent(""));

                    GUILayout.EndHorizontal();

                    if (useCustomContent.boolValue == false && buttonTarget.normalImage != null)
                        buttonTarget.normalImage.sprite = buttonTarget.buttonIcon;

                    else if (useCustomContent.boolValue == false && buttonTarget.normalImage == null)
                    {
                        GUILayout.Space(2);
                        EditorGUILayout.HelpBox("'Image Object' is not assigned. Go to Resources tab and assign the correct variable.", MessageType.Error);
                    }

                    if (enableButtonSounds.boolValue == true && useHoverSound.boolValue == true)
                    {
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);

                        EditorGUILayout.LabelField(new GUIContent("Hover Sound"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                        EditorGUILayout.PropertyField(hoverSound, new GUIContent(""));

                        GUILayout.EndHorizontal();
                    }

                    if (enableButtonSounds.boolValue == true && useClickSound.boolValue == true)
                    {
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);

                        EditorGUILayout.LabelField(new GUIContent("Click Sound"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                        EditorGUILayout.PropertyField(clickSound, new GUIContent(""));

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.Space(4);
                    EditorGUILayout.PropertyField(clickEvent, new GUIContent("On Click Event"), true);
                    EditorGUILayout.PropertyField(hoverEvent, new GUIContent("On Hover Event"), true);
                    break;

                case 1:
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Text Object"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(normalText, new GUIContent(""));

                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Image Object"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(normalImage, new GUIContent(""));

                    GUILayout.EndHorizontal();

                    if (enableButtonSounds.boolValue == true)
                    {
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);

                        EditorGUILayout.LabelField(new GUIContent("Sound Source"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                        EditorGUILayout.PropertyField(soundSource, new GUIContent(""));

                        GUILayout.EndHorizontal();
                    }

                    if (useRipple.boolValue == true)
                    {
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);

                        EditorGUILayout.LabelField(new GUIContent("Ripple Parent"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                        EditorGUILayout.PropertyField(rippleParent, new GUIContent(""));

                        GUILayout.EndHorizontal();
                    }

                    break;

                case 2:
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    useCustomContent.boolValue = GUILayout.Toggle(useCustomContent.boolValue, new GUIContent("Use Custom Content"), customSkin.FindStyle("Toggle"));
                    useCustomContent.boolValue = GUILayout.Toggle(useCustomContent.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    enableButtonSounds.boolValue = GUILayout.Toggle(enableButtonSounds.boolValue, new GUIContent("Enable Button Sounds"), customSkin.FindStyle("Toggle"));
                    enableButtonSounds.boolValue = GUILayout.Toggle(enableButtonSounds.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();

                    if (enableButtonSounds.boolValue == true)
                    {
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);

                        useHoverSound.boolValue = GUILayout.Toggle(useHoverSound.boolValue, new GUIContent("Enable Hover Sound"), customSkin.FindStyle("Toggle"));
                        useHoverSound.boolValue = GUILayout.Toggle(useHoverSound.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);

                        useClickSound.boolValue = GUILayout.Toggle(useClickSound.boolValue, new GUIContent("Enable Click Sound"), customSkin.FindStyle("Toggle"));
                        useClickSound.boolValue = GUILayout.Toggle(useClickSound.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                        GUILayout.EndHorizontal();

                        if (buttonTarget.soundSource == null)
                        {
                            EditorGUILayout.HelpBox("'Sound Source' is not assigned. Go to Resources tab or click the button to create a new audio source.", MessageType.Info);

                            if (GUILayout.Button("Create a new one", customSkin.button))
                            {
                                buttonTarget.soundSource = buttonTarget.gameObject.AddComponent(typeof(AudioSource)) as AudioSource;
                                currentTab = 2;
                            }
                        }
                    }

                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-2);
                    GUILayout.BeginHorizontal();

                    useRipple.boolValue = GUILayout.Toggle(useRipple.boolValue, new GUIContent("Use Ripple"), customSkin.FindStyle("Toggle"));
                    useRipple.boolValue = GUILayout.Toggle(useRipple.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();
                    GUILayout.Space(4);

                    if (useRipple.boolValue == true)
                    {
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);

                        renderOnTop.boolValue = GUILayout.Toggle(renderOnTop.boolValue, new GUIContent("Render On Top"), customSkin.FindStyle("Toggle"));
                        renderOnTop.boolValue = GUILayout.Toggle(renderOnTop.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);

                        centered.boolValue = GUILayout.Toggle(centered.boolValue, new GUIContent("Centered"), customSkin.FindStyle("Toggle"));
                        centered.boolValue = GUILayout.Toggle(centered.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);

                        EditorGUILayout.LabelField(new GUIContent("Shape"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                        EditorGUILayout.PropertyField(rippleShape, new GUIContent(""));

                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);

                        EditorGUILayout.LabelField(new GUIContent("Speed"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                        EditorGUILayout.PropertyField(speed, new GUIContent(""));

                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);

                        EditorGUILayout.LabelField(new GUIContent("Max Size"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                        EditorGUILayout.PropertyField(maxSize, new GUIContent(""));

                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);

                        EditorGUILayout.LabelField(new GUIContent("Start Color"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                        EditorGUILayout.PropertyField(startColor, new GUIContent(""));

                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);

                        EditorGUILayout.LabelField(new GUIContent("Transition Color"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                        EditorGUILayout.PropertyField(transitionColor, new GUIContent(""));

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.EndVertical();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}