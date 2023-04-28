using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HSVPicker;
using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Game.UI.Menu;
using SEE.GO;
using SEE.Layout.NodeLayouts.Cose;
using SEE.Net.Actions.RuntimeConfig;
using SEE.Utils;
using SimpleFileBrowser;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Menu for configuring a table/city.
    /// </summary>
    public class RuntimeTabMenu : TabMenu<ToggleMenuEntry>
    {
        /// <summary>
        /// Path which contains the prefabs for the runtime config menu. 
        /// </summary>
        public const string RUNTIME_CONFIG_PREFAB_FOLDER = UI_PREFAB_FOLDER + "RuntimeConfigMenu/";
        
        /// <summary>
        /// Prefab for a setting object.
        /// </summary>
        private const string SETTINGS_OBJECT_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeSettingsObject";
        
        /// <summary>
        /// Prefab for a switch.
        /// </summary>
        private const string SWITCH_PREFAB = UI_PREFAB_FOLDER + "Input Group - Switch";

        /// <summary>
        /// Prefab for a slider.
        /// </summary>
        private const string SLIDER_PREFAB = UI_PREFAB_FOLDER + "Input Group - Slider";
        
        /// <summary>
        /// Prefab for a dropdown.
        /// </summary>
        private const string DROPDOWN_PREFAB = UI_PREFAB_FOLDER + "Input Group - Dropdown 2";
        
        /// <summary>
        /// Prefab for a color picker.
        /// </summary>
        private const string COLORPICKER_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeColorPicker";
        
        /// <summary>
        /// Prefab for a string field.
        /// </summary>
        private const string STRINGFIELD_PREFAB = UI_PREFAB_FOLDER + "Input Group - String Input Field";
        
        /// <summary>
        /// Prefab for a button.
        /// </summary>
        private const string BUTTON_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeConfigButton";
        
        /// <summary>
        /// Prefab for a add button.
        /// </summary>
        private const string ADD_ELEMENT_BUTTON_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeAddButton";
        
        /// <summary>
        /// Prefab for a remove button.
        /// </summary>
        private const string REMOVE_ELEMENT_BUTTON_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeRemoveButton";

        /// <summary>
        /// The city index
        /// </summary>
        public int cityIndex;
        
        /// <summary>
        /// The city
        /// </summary>
        private AbstractSEECity city;

        /// <summary>
        /// The city switcher
        /// </summary>
        private HorizontalSelector citySwitcher;
        
        /// <summary>
        /// The list of configuration buttons which are displayed at the right side of the menu.
        ///
        /// <see cref="RuntimeButtonAttribute"/>
        /// </summary>
        private GameObject configButtonList;
        
        /// <summary>
        /// Whether the city should be immediately redrawn when a setting is changed.
        /// </summary>
        private bool immediateRedraw;

        /// <summary>
        /// Triggers when a field was changed by a different player.
        /// </summary>
        public Action<string, object> SyncField;
        
        /// <summary>
        /// Triggers when a method was used by a different player.
        /// </summary>
        public Action<string> SyncMethod;
        
        /// <summary>
        /// Triggers when a file path was changed by a different player.
        /// </summary>
        public Action<string, string, bool> SyncPath;
        
        /// <summary>
        /// Triggers when a list element was added by a different player.
        /// </summary>
        public Action<string> SyncAddListElement;
        
        /// <summary>
        /// Triggers when a list element was removed by a different player. 
        /// </summary>
        public Action<string> SyncRemoveListElement;
        
        /// <summary>
        /// Prefab for the menu
        /// </summary>
        protected override string MenuPrefab => RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeConfigMenu";
        
        /// <summary>
        /// Prefab for a view
        /// </summary>
        protected override string ViewPrefab => RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeSettingsView";
        
        /// <summary>
        /// Prefab for a tab button
        /// </summary>
        protected override string EntryPrefab => RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeTabButton";

        /// <summary>
        /// Path to the content game object
        /// </summary>
        protected override string ContentPath => "Main Content";
        
        /// <summary>
        /// Path to the view list game object
        /// </summary>
        protected override string ViewListPath => "ViewList";
        
        /// <summary>
        /// Path to the entry list game object
        /// </summary>
        protected override string EntryListPath => "TabButtons/TabObjects";
        
        /// <summary>
        /// Path to game object containing the configuration buttons
        /// </summary>
        protected virtual string ConfigButtonListPath => "ConfigButtons/Content";

        protected virtual string CitySwitcherPath => "City Switcher";

        protected override void StartDesktop()
        {
            base.StartDesktop();
            configButtonList = Content.transform.Find(ConfigButtonListPath).gameObject;
            citySwitcher = Content.transform.Find(CitySwitcherPath).GetComponent<HorizontalSelector>();

            SetupCitySwitcher();
        }

        /// <summary>
        ///     Updates the menu and adds listeners.
        /// </summary>
        protected override void OnStartFinished()
        {
            base.OnStartFinished();
            city = RuntimeConfigMenu.GetCities()[cityIndex];
            SyncMethod += methodName =>
            {
                if (methodName == nameof(TriggerImmediateRedraw)) TriggerImmediateRedraw();
            };

            SetupMenu();
        }

        /// <summary>
        /// Initializes the menu.
        ///
        /// Creates the widgets for fields and buttons for methods.
        /// </summary>
        private void SetupMenu()
        {
            // creates the widgets for fields
            var members = city.GetType().GetMembers().
                Where(IsCityAttribute).OrderBy(HasTabAttribute).ThenBy(GetTabName).ThenBy(SortIsNotNested);
            members.ForEach(memberInfo => CreateSetting(memberInfo, null, city));
            SelectEntry(Entries.First());

            // creates the buttons for methods
            var methods = city.GetType().GetMethods().Where(IsCityAttribute)
                .OrderBy(GetButtonGroup).ThenBy(GetOrderOfMemberInfo).ThenBy(GetButtonName);
            methods.ForEach(CreateButton);

            // methods used for ordering the buttons and settings
            string GetButtonName(MemberInfo memberInfo) => memberInfo.Name;

            string GetTabName(MemberInfo memberInfo) =>
                 memberInfo.GetCustomAttributes().OfType<RuntimeTabAttribute>().FirstOrDefault()?.Name;
            
            bool IsCityAttribute(MemberInfo memberInfo) => 
                memberInfo.DeclaringType == typeof(AbstractSEECity) || 
                memberInfo.DeclaringType!.IsSubclassOf(typeof(AbstractSEECity));
            
            bool HasTabAttribute(MemberInfo memberInfo) => 
                !memberInfo.GetCustomAttributes().Any(a => a is RuntimeTabAttribute);

            float GetOrderOfMemberInfo(MemberInfo memberInfo) =>
                (memberInfo.GetCustomAttributes().OfType<PropertyOrderAttribute>()
                    .FirstOrDefault() ?? new PropertyOrderAttribute()).Order;

            string GetButtonGroup(MemberInfo memberInfo) =>
                (memberInfo.GetCustomAttributes().OfType<RuntimeButtonAttribute>().FirstOrDefault()
                    ?? new RuntimeButtonAttribute(null, null)).Name;
            
            // ordered whether a setting is primitive or has nested settings
            bool SortIsNotNested(MemberInfo memberInfo)
            {
                object value;
                switch (memberInfo)
                {
                    case FieldInfo { IsLiteral: false, IsInitOnly: false } fieldInfo:
                        value = fieldInfo.GetValue(city);
                        break;
                    case PropertyInfo propertyInfo when !(propertyInfo.GetMethod == null 
                                                          || propertyInfo.SetMethod == null 
                                                          || !propertyInfo.CanRead 
                                                          || !propertyInfo.CanWrite
                                                          ):
                        value = propertyInfo.GetValue(city);
                        break;
                    default:
                        return false;
                }
                return value switch
                {
                    bool => true,
                    float => true,
                    int => true,
                    uint => true,
                    _ => false
                };
            }
        }

        /// <summary>
        /// Initializes the horizontal selector for switching cities.
        /// </summary>
        private void SetupCitySwitcher()
        {
            // initializes the list
            citySwitcher.itemList.Clear();
            citySwitcher.defaultIndex = cityIndex;
            RuntimeConfigMenu.GetCities().ForEach(c => citySwitcher.CreateNewItem(c.name));
            citySwitcher.SetupSelector();
            
            citySwitcher.selectorEvent.AddListener(index =>
            {
                OnSwitchCity?.Invoke(index);
                citySwitcher.index = cityIndex;
                citySwitcher.UpdateUI();
            });
        }
        
        /// <summary>
        /// Creates a configuration button.
        ///
        /// Checks whether a method with no parameters has the <see cref="RuntimeButtonAttribute"/>.
        /// </summary>
        /// <param name="methodInfo">method info</param>
        /// <see cref="RuntimeButtonAttribute"/>
        protected virtual void CreateButton(MethodInfo methodInfo)
        {
            RuntimeButtonAttribute buttonAttribute =
                methodInfo.GetCustomAttributes().OfType<RuntimeButtonAttribute>().FirstOrDefault();
            // only methods with the button attribute
            if (buttonAttribute == null) return;
            // only methods with no parameters
            if (methodInfo.GetParameters().Length > 0) return;

            // creates the button
            GameObject button = PrefabInstantiator.InstantiatePrefab(BUTTON_PREFAB, configButtonList.transform, false);
            button.name = methodInfo.GetCustomAttribute<RuntimeButtonAttribute>().Label;
            ButtonManagerWithIcon buttonManager = button.GetComponent<ButtonManagerWithIcon>();
            buttonManager.buttonText = methodInfo.GetCustomAttribute<RuntimeButtonAttribute>().Label;

            // add button listeners
            buttonManager.clickEvent.AddListener(() =>
            {
                // calls the method and updates the menu
                methodInfo.Invoke(city, null); 
                OnUpdateMenuValues?.Invoke();
            });
            buttonManager.clickEvent.AddListener(() =>
            {
                UpdateCityMethodNetAction netAction = new();
                netAction.CityIndex = cityIndex;
                netAction.MethodName = methodInfo.Name;
                netAction.Execute();
            });
            
            // add network listener
            SyncMethod += methodName =>
            {
                if (methodName == methodInfo.Name)
                {
                    // calls the method and updates the menu
                    methodInfo.Invoke(city, null); 
                    OnUpdateMenuValues?.Invoke();
                }
            };
        }

        /// <summary>
        ///     Returns the view game object.
        ///     Adds an entry if necessary.
        /// </summary>
        /// <param name="attributes"></param>
        /// <returns>view game object</returns>
        private GameObject CreateOrGetViewGameObject(IEnumerable<Attribute> attributes)
        {
            // get the tab attribute
            var tabName = attributes.OfType<RuntimeTabAttribute>().FirstOrDefault()?.Name ?? "Misc";
            ToggleMenuEntry entry = Entries.FirstOrDefault(entry => entry.Title == tabName);
            
            // add an entry (tab + view) if necessary
            if (entry == null)
            {
                entry = new ToggleMenuEntry(
                    () => { },
                    () => { },
                    tabName,
                    $"Settings for {tabName}",
                    GetColorForTab(),
                    Resources.Load<Sprite>("Materials/Charts/MoveIcon")
                );
                AddEntry(entry);
            }
            return ViewGameObject(entry);
        }

        /// <summary>
        /// Creates a setting for a field or property.
        ///
        /// For fields and properties that cannot be changed nothing is done.
        /// </summary>
        /// <param name="memberInfo">field or property info</param>
        /// <param name="parent">parent game object</param>
        /// <param name="obj">object which contains the field or property</param>
        private void CreateSetting(MemberInfo memberInfo, GameObject parent, object obj)
        {
            // obsolete members are ignored
            if (memberInfo.GetCustomAttributes().Any(attribute => attribute is ObsoleteAttribute)) return;
            switch (memberInfo)
            {
                case FieldInfo fieldInfo:
                    // field needs to be editable
                    if (fieldInfo.IsLiteral || fieldInfo.IsInitOnly) return;
                    CreateSetting(
                        () => fieldInfo.GetValue(obj),
                        memberInfo.Name,
                        parent,
                        changedValue => fieldInfo.SetValue(obj, changedValue),
                        memberInfo.GetCustomAttributes()
                    );
                    break;
                case PropertyInfo propertyInfo:
                    // property needs to be editable
                    if (propertyInfo.GetMethod == null || propertyInfo.SetMethod == null ||
                        !propertyInfo.CanRead || !propertyInfo.CanWrite) return;
                    if (propertyInfo.GetMethod.IsAbstract) return;
                    CreateSetting(
                        () => propertyInfo.GetValue(obj),
                        memberInfo.Name,
                        parent,
                        changedValue => propertyInfo.SetValue(obj, changedValue),
                        memberInfo.GetCustomAttributes()
                    );
                    break;
            }
        }

        /// <summary>
        /// Creates a widget for a setting.
        ///
        /// If the setting has nested members the method is called recursively.
        /// </summary>
        /// <param name="getter">getter</param>
        /// <param name="settingName">setting name</param>
        /// <param name="parent">parent game object</param>
        /// <param name="setter">setter</param>
        /// <param name="attributes">attributes</param>
        private void CreateSetting(Func<object> getter, string settingName, GameObject parent,
            UnityAction<object> setter = null, IEnumerable<Attribute> attributes = null)
        {
            // stores the attributes in an array so it can be accessed multiple times
            Attribute[] attributeArray = attributes as Attribute[] ?? attributes?.ToArray() ?? Array.Empty<Attribute>();
            parent ??= CreateOrGetViewGameObject(attributeArray).transform.Find("Content").gameObject;

            // create widget depending on the value type
            // bool -> Switch
            // int/uint/float -> Slider
            // string -> StringField
            // Color -> Color Picker
            // DataPath -> File Picker
            // Enum -> Dropdown
            // nested -> Settings Object with recursion
            // HashSet -> not supported
            object value = getter();
            // non-instanced settings cannot be edited 
            if (value == null) return;
            switch (value)
            {
                case bool:
                    CreateSwitch(
                        settingName,
                        changedValue => setter!(changedValue),
                        () => (bool)getter(),
                        parent
                    );
                    break;
                case int:
                    CreateSlider(
                        settingName,
                        attributeArray.OfType<RangeAttribute>().ElementAtOrDefault(0),
                        changedValue => setter!((int)changedValue),
                        () => (int)getter(),
                        true,
                        parent
                    );
                    break;
                case uint:
                    CreateSlider(
                        settingName,
                        attributeArray.OfType<RangeAttribute>().ElementAtOrDefault(0),
                        changedValue => setter!((uint)changedValue),
                        () => (uint)getter(),
                        true,
                        parent
                    );
                    break;
                case float:
                    CreateSlider(
                        settingName,
                        attributeArray.OfType<RangeAttribute>().ElementAtOrDefault(0),
                        changedValue => setter!(changedValue),
                        () => (float)getter(),
                        false,
                        parent
                    );
                    break;
                case string:
                    CreateStringField(
                        settingName,
                        changedValue => setter!(changedValue),
                        () => (string)getter(),
                        parent
                    );
                    break;
                case Color:
                    parent = CreateNestedSetting(settingName, parent);
                    CreateColorPicker(
                        settingName,
                        parent,
                        changedValue => setter!(changedValue),
                        () => (Color)getter()
                    );
                    break;
                case DataPath dataPath:
                    parent = CreateNestedSetting(settingName, parent);
                    CreateFilePicker(settingName, dataPath, parent);
                    break;
                case Enum:
                    CreateDropDown(
                        settingName,
                        changedValue => setter!(Enum.ToObject(value.GetType(), changedValue)),
                        value.GetType().GetEnumNames(),
                        () => getter().ToString(),
                        parent
                    );
                    break;
                // from here on come nested settings
                case NodeTypeVisualsMap:
                case ColorMap:
                    FieldInfo mapInfo =
                        value.GetType().GetField("map", BindingFlags.Instance | BindingFlags.NonPublic)!;
                    CreateSetting(
                        () => mapInfo.GetValue(value),
                        settingName,
                        parent,
                        null,
                        attributeArray
                    );
                    break;
                case AntennaAttributes:
                    FieldInfo antennaInfo = value.GetType().GetField("AntennaSections")!;
                    CreateSetting(
                        () => antennaInfo.GetValue(value),
                        settingName,
                        parent,
                        null,
                        attributeArray
                    );
                    break;

                // types that shouldn't be in the configuration menu
                case Graph:
                    break;
             
                // from here on come nested settings
                case HashSet<string>:
                    // not supported
                    break;
                case IDictionary dict:
                    parent = CreateNestedSetting(settingName, parent);
                    UpdateDictChildren(parent, dict);
                    OnUpdateMenuValues += () => UpdateDictChildren(parent, dict);
                    break;
                case IList<string> list:
                    parent = CreateNestedSetting(settingName, parent);
                    CreateList(list, parent);
                    break;
                
                // confirmed types where the nested fields should be edited
                case ColorRange:
                case ColorProperty:
                case CoseGraphAttributes:
                case VisualNodeAttributes:
                case NodeLayoutAttributes:
                case EdgeLayoutAttributes:
                case EdgeSelectionAttributes:
                case ErosionAttributes:
                case BoardAttributes:
                    parent = CreateNestedSetting(settingName, parent);
                    value.GetType().GetMembers().ForEach(nestedInfo => CreateSetting(nestedInfo, parent, value));
                    break;
                
                // unconfirmed types where the nested fields should be edited
                case VisualAttributes:
                case ConfigIO.PersistentConfigItem:
                case LabelAttributes:
                    if (value.GetType() != typeof(VisualAttributes)
                        && value.GetType() != typeof(ConfigIO.PersistentConfigItem)
                        && value.GetType() != typeof(LabelAttributes)
                       )
                    {
                        Debug.LogWarning("Missing: (Maybe)" + settingName + " " + 
                                         value.GetType().GetNiceName() + "\n");
                    }
                    parent = CreateNestedSetting(settingName, parent);
                    value.GetType().GetMembers().ForEach(nestedInfo => CreateSetting(nestedInfo, parent, value));
                    break;
                
                default:
                    Debug.LogWarning("Missing: " + settingName + ", " + value?.GetType().GetNiceName() + "\n");
                    break;
            }
        }

        private GameObject CreateNestedSetting(string settingName, GameObject parent)
        {
            GameObject container =
                PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB, parent.transform, false);
            container.name = settingName;
            container.GetComponentInChildren<TextMeshProUGUI>().text = settingName;
            return container.transform.Find("Content").gameObject;
        }

        private void CreateSlider(string settingName, RangeAttribute range, UnityAction<float> setter,
            Func<float> getter, bool useRoundValue, GameObject parent, 
            bool recursive = false, Func<string> getWidgetName = null)
        {
            range ??= new RangeAttribute(0, 2);

            GameObject sliderGameObject =
                PrefabInstantiator.InstantiatePrefab(SLIDER_PREFAB, parent.transform, false);
            sliderGameObject.name = settingName;
            AddLayoutElement(sliderGameObject);
            SliderManager sliderManager = sliderGameObject.GetComponentInChildren<SliderManager>();
            Slider slider = sliderGameObject.GetComponentInChildren<Slider>();
            TextMeshProUGUI text = sliderGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            text.text = settingName;
            
            getWidgetName ??= () => sliderGameObject.FullName();

            sliderManager.usePercent = false;
            sliderManager.useRoundValue = useRoundValue;
            slider.minValue = range.min;
            slider.maxValue = range.max;

            RuntimeSliderManager endEditManager = slider.gameObject.AddComponent<RuntimeSliderManager>();

            slider.value = getter();
            endEditManager.OnEndEdit += () => setter(slider.value);

            endEditManager.OnEndEdit += () =>
            {
                UpdateFloatCityFieldNetAction action = new();
                action.CityIndex = cityIndex;
                action.WidgetPath = getWidgetName();
                action.Value = slider.value;
                action.Execute();
            };
            endEditManager.OnEndEdit += CheckImmediateRedraw;

            SyncField += (widgetPath, value) =>
            {
                if (sliderGameObject != null && widgetPath == getWidgetName())
                {
                    setter((float)value);
                    slider.value = (float)value;
                    sliderManager.UpdateUI();
                }
            };

            OnUpdateMenuValues += () =>
            {
                slider.value = getter();
                sliderManager.UpdateUI();
            };


            if (!recursive)
            {
                RuntimeSmallEditorButton smallEditorButton = sliderGameObject.AddComponent<RuntimeSmallEditorButton>();
                smallEditorButton.OnShowMenuChanged += () =>
                {
                    immediateRedraw = smallEditorButton.ShowMenu;
                    ShowMenu = !smallEditorButton.ShowMenu;
                    OnUpdateMenuValues?.Invoke();
                };
                smallEditorButton.CreateWidget = smallEditor =>
                    CreateSlider(settingName, range, setter, getter, useRoundValue, smallEditor, true, getWidgetName);
            }
        }

        private void CreateSwitch(string settingName, UnityAction<bool> setter, Func<bool> getter, GameObject parent,
            bool recursive = false, Func<string> getWidgetName = null)
        {
            GameObject switchGameObject =
                PrefabInstantiator.InstantiatePrefab(SWITCH_PREFAB, parent.transform, false);
            switchGameObject.name = settingName;
            AddLayoutElement(switchGameObject);
            SwitchManager switchManager = switchGameObject.GetComponentInChildren<SwitchManager>();
            TextMeshProUGUI text = switchGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            text.text = settingName;
            
            getWidgetName ??= () => switchGameObject.FullName();

            switchManager.isOn = getter();
            switchManager.UpdateUI();
            switchManager.OnEvents.AddListener(() => setter(true));
            switchManager.OnEvents.AddListener(() =>
            {
                UpdateBoolCityFieldNetAction action = new();
                action.CityIndex = cityIndex;
                action.WidgetPath = getWidgetName();
                action.Value = true;
                action.Execute();
            });
            switchManager.OnEvents.AddListener(CheckImmediateRedraw);
            switchManager.OffEvents.AddListener(() => setter(false));
            switchManager.OffEvents.AddListener(() =>
            {
                UpdateBoolCityFieldNetAction action = new();
                action.CityIndex = cityIndex;
                action.WidgetPath = getWidgetName();
                action.Value = false;
                action.Execute();
            });
            switchManager.OffEvents.AddListener(CheckImmediateRedraw);

            SyncField += (widgetPath, value) =>
            {
                if (switchGameObject != null && widgetPath == getWidgetName())
                {
                    setter((bool)value);
                    switchManager.isOn = (bool)value;
                    switchManager.UpdateUI();
                }
            };

            OnUpdateMenuValues += () =>
            {
                switchManager.isOn = getter();
                switchManager.UpdateUI();
            };

            if (!recursive)
            {
                RuntimeSmallEditorButton smallEditorButton = switchGameObject.AddComponent<RuntimeSmallEditorButton>();
                smallEditorButton.OnShowMenuChanged += () =>
                {
                    immediateRedraw = smallEditorButton.ShowMenu;
                    ShowMenu = !smallEditorButton.ShowMenu;
                    OnUpdateMenuValues?.Invoke();
                };
                smallEditorButton.CreateWidget = smallEditor =>
                    CreateSwitch(settingName, setter, getter, smallEditor, true, getWidgetName);
            }
        }

        private void CreateStringField(string settingName, UnityAction<string> setter, Func<string> getter,
            GameObject parent, bool recursive = false, Func<string> getWidgetName = null)
        {
            GameObject stringGameObject =
                PrefabInstantiator.InstantiatePrefab(STRINGFIELD_PREFAB, parent.transform, false);
            stringGameObject.name = settingName;
            AddLayoutElement(stringGameObject);
            TextMeshProUGUI text = stringGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            text.text = settingName;
            
            getWidgetName ??= () => stringGameObject.FullName();

            TMP_InputField inputField = stringGameObject.GetComponentInChildren<TMP_InputField>();
            inputField.text = getter();
            inputField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
            inputField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);
            inputField.onEndEdit.AddListener(setter);
            inputField.onEndEdit.AddListener(changedValue =>
            {
                UpdateStringCityFieldNetAction action = new();
                action.CityIndex = cityIndex;
                action.WidgetPath = getWidgetName();
                action.Value = changedValue;
                action.Execute();
            });
            inputField.onEndEdit.AddListener(_ => CheckImmediateRedraw());

            OnUpdateMenuValues += () => inputField.text = getter();

            SyncField += (widgetPath, value) =>
            {
                if (stringGameObject != null && widgetPath == getWidgetName())
                {
                    setter(value as string);
                    inputField.text = value as string;
                }
            };


            if (!recursive)
            {
                RuntimeSmallEditorButton smallEditorButton = stringGameObject.AddComponent<RuntimeSmallEditorButton>();
                smallEditorButton.OnShowMenuChanged += () =>
                {
                    immediateRedraw = smallEditorButton.ShowMenu;
                    ShowMenu = !smallEditorButton.ShowMenu;
                    OnUpdateMenuValues?.Invoke();
                };
                smallEditorButton.CreateWidget = smallEditor =>
                    CreateStringField(settingName, setter, getter, smallEditor, true, getWidgetName);
            }
        }

        // TODO: Add action
        private void CreateDropDown(string settingName, UnityAction<int> setter, IEnumerable<string> values,
            Func<string> getter, GameObject parent, bool recursive = false, Func<string> getWidgetName = null)
        {
            string[] valueArray = values as string[] ?? values.ToArray();

            GameObject dropDownGameObject =
                PrefabInstantiator.InstantiatePrefab(DROPDOWN_PREFAB, parent.transform, false);
            dropDownGameObject.name = settingName;
            AddLayoutElement(dropDownGameObject);
            TextMeshProUGUI text = dropDownGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            text.text = settingName;
            // TODO: value and setter
            
            getWidgetName ??= () => dropDownGameObject.FullName();

            CustomDropdown dropdown = dropDownGameObject.transform.Find("Dropdown").GetComponent<CustomDropdown>();

            dropdown.isListItem = true;
            dropdown.listParent = !recursive ? Menu.transform : Canvas.transform;
            dropdown.selectedItemIndex = Array.IndexOf(valueArray, getter());
            valueArray.ForEach(s => dropdown.CreateNewItemFast(s, null));

            dropdown.SetupDropdown();

            dropdown.dropdownEvent.AddListener(setter);
            dropdown.dropdownEvent.AddListener(changedValue =>
            {
                UpdateIntCityFieldNetAction action = new();
                action.CityIndex = cityIndex;
                action.WidgetPath = getWidgetName();
                action.Value = changedValue;
                action.Execute();
            });
            dropdown.dropdownEvent.AddListener(_ => CheckImmediateRedraw());

            SyncField += (widgetPath, value) =>
            {
                if (dropDownGameObject != null && widgetPath == getWidgetName())
                {
                    setter((int)value);
                    dropdown.ChangeDropdownInfo((int)value);
                }
            };

            OnUpdateMenuValues += () => dropdown.ChangeDropdownInfo(Array.IndexOf(valueArray, getter()));

            if (!recursive)
            {
                RuntimeSmallEditorButton smallEditorButton =
                    dropDownGameObject.AddComponent<RuntimeSmallEditorButton>();
                smallEditorButton.OnShowMenuChanged += () =>
                {
                    immediateRedraw = smallEditorButton.ShowMenu;
                    ShowMenu = !smallEditorButton.ShowMenu;
                    OnUpdateMenuValues?.Invoke();
                };
                smallEditorButton.CreateWidget = smallEditor =>
                    CreateDropDown(settingName, setter, valueArray, getter, smallEditor, true, getWidgetName);
            }
        }

        // TODO: Add action
        private void CreateColorPicker(string settingName, GameObject parent, UnityAction<Color> setter,
            Func<Color> getter, bool recursive = false, Func<string> getWidgetName = null)
        {
            GameObject colorPickerGameObject =
                PrefabInstantiator.InstantiatePrefab(COLORPICKER_PREFAB, parent.transform, false);
            colorPickerGameObject.name = settingName;
            AddLayoutElement(colorPickerGameObject);
            // Set values for colorPicker
            ColorPicker colorPicker = colorPickerGameObject.GetComponent<ColorPicker>();
            colorPicker.CurrentColor = getter();
            colorPicker.onValueChanged.AddListener(setter);
            // colorPicker.onValueChanged.AddListener(_ => CheckImmediateRedraw());
            
            getWidgetName ??= () => colorPickerGameObject.FullName();

            // Add netAction to boxSlider element ossf colorPicker
            BoxSlider boxSlider = colorPickerGameObject.GetComponentInChildren<BoxSlider>();
            RuntimeSliderManager boxEndEditManager = boxSlider.gameObject.AddComponent<RuntimeSliderManager>();
            boxEndEditManager.OnEndEdit += () =>
            {
                CheckImmediateRedraw();
                UpdateColorCityFieldNetAction action = new();
                action.CityIndex = cityIndex;
                action.WidgetPath = getWidgetName();
                action.Value = getter();
                action.Execute();
            };

            // Add netAction to hueSlider element of colorPicker
            Slider hueSlider = colorPickerGameObject.GetComponentInChildren<Slider>();
            RuntimeSliderManager hueEndEditManager = hueSlider.gameObject.AddComponent<RuntimeSliderManager>();
            hueEndEditManager.OnEndEdit += () =>
            {
                CheckImmediateRedraw();
                UpdateColorCityFieldNetAction action = new();
                action.CityIndex = cityIndex;
                action.WidgetPath = getWidgetName();
                action.Value = getter();
                action.Execute();
            };

            // Add netAction to string input element of colorPicker
            TMP_InputField inputField = colorPickerGameObject.GetComponentInChildren<TMP_InputField>();
            inputField.onSelect.AddListener(str => SEEInput.KeyboardShortcutsEnabled = false);
            inputField.onDeselect.AddListener(str => SEEInput.KeyboardShortcutsEnabled = true);
            inputField.onEndEdit.AddListener(str =>
            {
                CheckImmediateRedraw();
                UpdateColorCityFieldNetAction action = new();
                action.CityIndex = cityIndex;
                action.WidgetPath = getWidgetName();
                action.Value = getter();
                action.Execute();
            });

            // Deactivate presets and sliders
            colorPickerGameObject.transform.Find("Presets").gameObject.SetActive(false);
            colorPickerGameObject.transform.Find("Sliders").gameObject.SetActive(false);

            // Colorpicker should be collapsed by default
            if (!recursive)
                colorPickerGameObject.transform.parent.parent.GetComponentInChildren<RuntimeConfigMenuCollapse>()
                    .OnClickCollapse();

            SyncField += (widgetPath, value) =>
            {
                if (colorPickerGameObject != null && widgetPath == getWidgetName())
                {
                    setter((Color)value);
                    colorPicker.CurrentColor = getter();
                }
            };
            OnUpdateMenuValues += () => { colorPicker.CurrentColor = getter(); };

            if (!recursive)
            {
                RuntimeSmallEditorButton smallEditorButton =
                    colorPickerGameObject.AddComponent<RuntimeSmallEditorButton>();
                smallEditorButton.OnShowMenuChanged += () =>
                {
                    immediateRedraw = smallEditorButton.ShowMenu;
                    ShowMenu = !smallEditorButton.ShowMenu;
                    OnUpdateMenuValues?.Invoke();
                };
                smallEditorButton.CreateWidget = smallEditor =>
                    CreateColorPicker(settingName, smallEditor, setter, getter, true, getWidgetName);
            }
        }

        private void CreateFilePicker(string settingName, DataPath dataPath, GameObject parent)
        {
            FilePicker.FilePicker filePicker = parent.AddComponent<FilePicker.FilePicker>();
            filePicker.DataPathInstance = dataPath;
            filePicker.Label = settingName;
            filePicker.PickingMode = FileBrowser.PickMode.Files;
            filePicker.OnMenuInitialized +=
                () => AddLayoutElement(parent.transform.Find(settingName).gameObject);

            Func<string> getWidgetName = () => filePicker.gameObject.FullName() + "/" + settingName;

            OnShowMenuChanged += () => { if (!ShowMenu) filePicker.CloseDropdown();};

            filePicker.OnChangedDropdown += () =>
            {
                UpdateIntCityFieldNetAction netAction = new();
                netAction.Value = (int)dataPath.Root;
                netAction.CityIndex = cityIndex;
                netAction.WidgetPath = getWidgetName();
                netAction.Execute();
            };

            filePicker.OnChangedPath += () =>
            {
                UpdatePathCityFieldNetAction netAction = new();
                netAction.IsAbsolute = dataPath.Root == DataPath.RootKind.Absolute;
                netAction.Value = netAction.IsAbsolute ? dataPath.AbsolutePath : dataPath.RelativePath;
                netAction.CityIndex = cityIndex;
                netAction.WidgetPath = getWidgetName();
                netAction.Execute();
            };

            SyncPath += (widgetPath, newValue, isAbsolute) =>
            {
                if (widgetPath == getWidgetName())
                    filePicker.SyncPath(newValue, isAbsolute);
            };

            SyncField += (widgetPath, newValue) =>
            {
                if (widgetPath == getWidgetName())
                    filePicker.SyncDropdown((int)newValue);
            };
        }

        private void CreateList(IList<string> list, GameObject parent)
        {
            // Buttons erstellen
            GameObject buttonContainer = new("ButtonContainer");
            buttonContainer.AddComponent<HorizontalLayoutGroup>();
            buttonContainer.transform.SetParent(parent.transform);

            GameObject addButton =
                PrefabInstantiator.InstantiatePrefab(ADD_ELEMENT_BUTTON_PREFAB, buttonContainer.transform);
            GameObject removeButton =
                PrefabInstantiator.InstantiatePrefab(REMOVE_ELEMENT_BUTTON_PREFAB, buttonContainer.transform);
            removeButton.name = "RemoveElementButton";
            ButtonManagerWithIcon removeButtonManager = removeButton.GetComponent<ButtonManagerWithIcon>();
            addButton.name = "AddElementButton";
            ButtonManagerWithIcon addButtonManager = addButton.GetComponent<ButtonManagerWithIcon>();
            // Listener AddButton
            addButtonManager.clickEvent.AddListener(() =>
            {
                list.Add("");
                UpdateListChildren(list, parent);
                buttonContainer.transform.SetAsLastSibling();
                AddListElementNetAction netAction = new();
                netAction.CityIndex = cityIndex;
                netAction.WidgetPath = parent.FullName();
                netAction.Execute();
            });
            // Listener RemoveButton
            removeButtonManager.clickEvent.AddListener(() =>
            {
                if (list.Count == 0) return;
                list.RemoveAt(list.Count - 1);
                UpdateListChildren(list, parent);
                buttonContainer.transform.SetAsLastSibling();
                RemoveListElementNetAction netAction = new();
                netAction.CityIndex = cityIndex;
                netAction.WidgetPath = parent.FullName();
                netAction.Execute();
            });

            SyncAddListElement += widgetPath =>
            {
                if (widgetPath == parent.FullName())
                {
                    list.Add("");
                    UpdateListChildren(list, parent);
                    buttonContainer.transform.SetAsLastSibling();
                }
            };
            SyncRemoveListElement += widgetPath =>
            {
                if (widgetPath == parent.FullName())
                {
                    list.RemoveAt(list.Count - 1);
                    UpdateListChildren(list, parent);
                    buttonContainer.transform.SetAsLastSibling();
                }
            };
            // Update
            UpdateListChildren(list, parent);
            buttonContainer.transform.SetAsLastSibling();
            OnUpdateMenuValues += () =>
            {
                UpdateListChildren(list, parent);
                addButton.transform.SetAsLastSibling();
                removeButton.transform.SetAsLastSibling();
            };
        }

        private void UpdateListChildren(IList<string> list, GameObject parent)
        {
            // removes superfluous children
            foreach (Transform child in parent.transform)
                if (int.TryParse(child.name, out int index))
                    if (index >= list.Count)
                        Destroyer.Destroy(child.gameObject);
            // creates needed children
            for (int i = 0; i < list.Count; i++)
                if (parent.transform.Find(i.ToString()) == null)
                {
                    int iCopy = i;
                    CreateSetting(
                        () => list[iCopy],
                        i.ToString(),
                        parent,
                        changedValue => list[iCopy] = changedValue as string
                    );
                }
        }

        private void UpdateDictChildren(GameObject parent, IDictionary dict)
        {
            // removes children that aren't in the dictionary any more
            foreach (Transform child in parent.transform)
                if (!dict.Contains(child.name))
                    Destroyer.Destroy(child);
            // goes through all dictionary keys
            foreach (object key in dict.Keys)
                // creates a child if it doesn't exist yet
                if (parent.transform.Find(key.ToString()) == null)
                    CreateSetting(
                        () => dict[key],
                        key.ToString(),
                        parent,
                        changedValue => dict[key] = changedValue
                    );
        }

        private void AddLayoutElement(GameObject go)
        {
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minWidth = ((RectTransform)go.transform).rect.width;
            le.minHeight = ((RectTransform)go.transform).rect.height;
        }

        private void CheckImmediateRedraw()
        {
            if (!immediateRedraw) return;
            TriggerImmediateRedraw();
            UpdateCityMethodNetAction netAction = new();
            netAction.CityIndex = cityIndex;
            netAction.MethodName = nameof(TriggerImmediateRedraw);
            netAction.Execute();
        }

        private void TriggerImmediateRedraw()
        {
            if (city.LoadedGraph == null) return;
            city.Invoke("LoadData", 0);
            StartCoroutine(DrawNextFrame());

            IEnumerator DrawNextFrame()
            {
                yield return 0;
                city.Invoke("DrawGraph", 0);
            }
        }
        
        /// <summary>
        /// Assigns a color to a tab button.
        ///
        /// Uses a base color and alternately makes that color slightly brighter/darker.
        /// </summary>
        /// <returns>color</returns>
        private Color GetColorForTab()
        {
            int tabCount = ViewList.transform.childCount;
            
            // default base value
            float baseColor = 0.5f;
            
            // switch slightly between bright and dark
            baseColor *= tabCount % 2 == 1 ? 0.75f : 1.25f;
            
            return Color.Lerp(Color.black, Color.white, baseColor);
        }

        public event Action OnUpdateMenuValues;

        public event Action<int> OnSwitchCity;
    }
}