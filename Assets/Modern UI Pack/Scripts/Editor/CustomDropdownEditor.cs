using UnityEngine;
using UnityEditor;

namespace Michsky.UI.ModernUIPack
{
    [CustomEditor(typeof(CustomDropdown))]
    public class CustomDropdownEditor : Editor
    {      
        private CustomDropdown dTarget;
        private int currentTab;

        private void OnEnable()
        {
            dTarget = (CustomDropdown)target;
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

            GUILayout.Box(new GUIContent(""), customSkin.FindStyle("Dropdown Top Header"));

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

            var dropdownItems = serializedObject.FindProperty("dropdownItems");
            var dropdownEvent = serializedObject.FindProperty("dropdownEvent");
            var triggerObject = serializedObject.FindProperty("triggerObject");
            var selectedText = serializedObject.FindProperty("selectedText");
            var selectedImage = serializedObject.FindProperty("selectedImage");
            var itemParent = serializedObject.FindProperty("itemParent");
            var itemObject = serializedObject.FindProperty("itemObject");
            var scrollbar = serializedObject.FindProperty("scrollbar");
            var listParent = serializedObject.FindProperty("listParent");
            var saveSelected = serializedObject.FindProperty("saveSelected");
            var dropdownTag = serializedObject.FindProperty("dropdownTag");
            var enableIcon = serializedObject.FindProperty("enableIcon");
            var enableTrigger = serializedObject.FindProperty("enableTrigger");
            var enableScrollbar = serializedObject.FindProperty("enableScrollbar");
            var setHighPriorty = serializedObject.FindProperty("setHighPriorty");
            var outOnPointerExit = serializedObject.FindProperty("outOnPointerExit");
            var isListItem = serializedObject.FindProperty("isListItem");
            var invokeAtStart = serializedObject.FindProperty("invokeAtStart");
            var animationType = serializedObject.FindProperty("animationType");
            var selectedItemIndex = serializedObject.FindProperty("selectedItemIndex");
            var enableDropdownSounds = serializedObject.FindProperty("enableDropdownSounds");
            var useHoverSound = serializedObject.FindProperty("useHoverSound");
            var useClickSound = serializedObject.FindProperty("useClickSound");
            var hoverSound = serializedObject.FindProperty("hoverSound");
            var clickSound = serializedObject.FindProperty("clickSound");
            var soundSource = serializedObject.FindProperty("soundSource");

            switch (currentTab)
            {
                case 0:
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUI.indentLevel = 1;

                    EditorGUILayout.PropertyField(dropdownItems, new GUIContent("Dropdown Items"), true); 
                    dropdownItems.isExpanded = true;
                  
                    EditorGUI.indentLevel = 0;
                
                    if (GUILayout.Button("+  Add a new item", customSkin.button))
                        dTarget.AddNewItem();

                    GUILayout.EndVertical();

                    if (enableDropdownSounds.boolValue == true)
                    {
                        GUILayout.Space(18);
                        GUILayout.Label("SOUNDS", customSkin.FindStyle("Header"));

                        if (enableDropdownSounds.boolValue == true && useHoverSound.boolValue == true)
                        {
                            GUILayout.BeginHorizontal(EditorStyles.helpBox);

                            EditorGUILayout.LabelField(new GUIContent("Hover Sound"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                            EditorGUILayout.PropertyField(hoverSound, new GUIContent(""));

                            GUILayout.EndHorizontal();
                        }

                        if (enableDropdownSounds.boolValue == true && useClickSound.boolValue == true)
                        {
                            GUILayout.BeginHorizontal(EditorStyles.helpBox);

                            EditorGUILayout.LabelField(new GUIContent("Click Sound"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                            EditorGUILayout.PropertyField(clickSound, new GUIContent(""));

                            GUILayout.EndHorizontal();
                        }
                    }

                    GUILayout.Space(10);
                    EditorGUILayout.PropertyField(dropdownEvent, new GUIContent("Dropdown Event"), true);
                    break;

                case 1:
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Trigger Object"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(triggerObject, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Selected Text"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(selectedText, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Selected Image"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(selectedImage, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Item Prefab"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(itemObject, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Item Parent"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(itemParent, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Scrollbar"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(scrollbar, new GUIContent(""));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("List Parent"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(listParent, new GUIContent(""));

                    GUILayout.EndHorizontal();

                    if (enableDropdownSounds.boolValue == true)
                    {
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);

                        EditorGUILayout.LabelField(new GUIContent("Sound Source"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                        EditorGUILayout.PropertyField(soundSource, new GUIContent(""));

                        GUILayout.EndHorizontal();
                    }

                    break;

                case 2:
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    enableIcon.boolValue = GUILayout.Toggle(enableIcon.boolValue, new GUIContent("Enable Icon"), customSkin.FindStyle("Toggle"));
                    enableIcon.boolValue = GUILayout.Toggle(enableIcon.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();

                    if (dTarget.selectedImage != null)
                    {
                        if (enableIcon.boolValue == true)
                            dTarget.selectedImage.enabled = true;
                        else
                            dTarget.selectedImage.enabled = false;
                    }

                    else
                    {
                        if (enableIcon.boolValue == true)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.HelpBox("'Selected Image' is not assigned. Go to Resources tab and assign the correct variable.", MessageType.Error);
                            GUILayout.EndHorizontal();
                        }                       
                    }

                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    enableTrigger.boolValue = GUILayout.Toggle(enableTrigger.boolValue, new GUIContent("Enable Trigger"), customSkin.FindStyle("Toggle"));
                    enableTrigger.boolValue = GUILayout.Toggle(enableTrigger.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();  

                    if (enableTrigger.boolValue == true && dTarget.triggerObject == null)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox("'Trigger Object' is not assigned. Go to Resources tab and assign the correct variable.", MessageType.Error);
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    enableScrollbar.boolValue = GUILayout.Toggle(enableScrollbar.boolValue, new GUIContent("Enable Scrollbar"), customSkin.FindStyle("Toggle"));
                    enableScrollbar.boolValue = GUILayout.Toggle(enableScrollbar.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();

                    if (dTarget.scrollbar != null)
                    {
                        if (enableScrollbar.boolValue == true)
                            dTarget.scrollbar.SetActive(true);
                        else
                            dTarget.scrollbar.SetActive(false);
                    }

                    else
                    {
                        if (enableScrollbar.boolValue == true)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.HelpBox("'Scrollbar' is not assigned. Go to Resources tab and assign the correct variable.", MessageType.Error);
                            GUILayout.EndHorizontal();
                        }                     
                    }

                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    setHighPriorty.boolValue = GUILayout.Toggle(setHighPriorty.boolValue, new GUIContent("Set High Priorty"), customSkin.FindStyle("Toggle"));
                    setHighPriorty.boolValue = GUILayout.Toggle(setHighPriorty.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    outOnPointerExit.boolValue = GUILayout.Toggle(outOnPointerExit.boolValue, new GUIContent("Out On Pointer Exit"), customSkin.FindStyle("Toggle"));
                    outOnPointerExit.boolValue = GUILayout.Toggle(outOnPointerExit.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    isListItem.boolValue = GUILayout.Toggle(isListItem.boolValue, new GUIContent("Is List Item"), customSkin.FindStyle("Toggle"));
                    isListItem.boolValue = GUILayout.Toggle(isListItem.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();
             
                    if (isListItem.boolValue == true && dTarget.listParent == null)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox("'List Parent' is not assigned. Go to Resources tab and assign the correct variable.", MessageType.Error);
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    invokeAtStart.boolValue = GUILayout.Toggle(invokeAtStart.boolValue, new GUIContent("Invoke At Start"), customSkin.FindStyle("Toggle"));
                    invokeAtStart.boolValue = GUILayout.Toggle(invokeAtStart.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    enableDropdownSounds.boolValue = GUILayout.Toggle(enableDropdownSounds.boolValue, new GUIContent("Enable Dropdown Sounds"), customSkin.FindStyle("Toggle"));
                    enableDropdownSounds.boolValue = GUILayout.Toggle(enableDropdownSounds.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();

                    if (enableDropdownSounds.boolValue == true)
                    {
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);

                        useHoverSound.boolValue = GUILayout.Toggle(useHoverSound.boolValue, new GUIContent("Enable Hover Sound"), customSkin.FindStyle("Toggle"));
                        useHoverSound.boolValue = GUILayout.Toggle(useHoverSound.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);

                        useClickSound.boolValue = GUILayout.Toggle(useClickSound.boolValue, new GUIContent("Enable Click Sound"), customSkin.FindStyle("Toggle"));
                        useClickSound.boolValue = GUILayout.Toggle(useClickSound.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                        GUILayout.EndHorizontal();

                        if (dTarget.soundSource == null)
                        {
                            EditorGUILayout.HelpBox("'Sound Source' is not assigned. Go to Resources tab or click the button to create a new audio source.", MessageType.Info);

                            if (GUILayout.Button("+ Create a new one", customSkin.button))
                            {
                                dTarget.soundSource = dTarget.gameObject.AddComponent(typeof(AudioSource)) as AudioSource;
                                currentTab = 2;
                            }
                        }
                    }

                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    saveSelected.boolValue = GUILayout.Toggle(saveSelected.boolValue, new GUIContent("Save Selection"), customSkin.FindStyle("Toggle"));
                    saveSelected.boolValue = GUILayout.Toggle(saveSelected.boolValue, new GUIContent(""), customSkin.FindStyle("Toggle Helper"));

                    GUILayout.EndHorizontal();

                    if (saveSelected.boolValue == true)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(35);

                        EditorGUILayout.LabelField(new GUIContent("Tag:"), customSkin.FindStyle("Text"), GUILayout.Width(40));
                        EditorGUILayout.PropertyField(dropdownTag, new GUIContent(""));

                        GUILayout.EndHorizontal();
                        EditorGUILayout.HelpBox("Each dropdown should has its own unique tag.", MessageType.Info);
                    }

                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(new GUIContent("Animation Type"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                    EditorGUILayout.PropertyField(animationType, new GUIContent(""));

                    GUILayout.EndHorizontal();

                    if (dTarget.dropdownItems.Count != 0)
                    {
                        GUILayout.BeginVertical(EditorStyles.helpBox);

                        EditorGUILayout.LabelField(new GUIContent("Selected Item Index:"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                        selectedItemIndex.intValue = EditorGUILayout.IntSlider(selectedItemIndex.intValue, 0, dTarget.dropdownItems.Count - 1);

                        GUILayout.Space(2);
                        EditorGUILayout.LabelField(new GUIContent(dTarget.dropdownItems[selectedItemIndex.intValue].itemName), customSkin.FindStyle("Text"));
                        GUILayout.EndVertical();

                        if (saveSelected.boolValue == true)
                            EditorGUILayout.HelpBox("Save Selection is enabled. This option won't be used if there's a stored value.", MessageType.Info);
                    }

                    else
                        EditorGUILayout.HelpBox("There is no item in the list.", MessageType.Warning);

                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}