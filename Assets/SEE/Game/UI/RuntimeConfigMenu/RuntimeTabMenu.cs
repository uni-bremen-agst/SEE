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
using UnityEngine.UI;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Menu for configuring a table/city.
    /// </summary>
    public class RuntimeTabMenu : TabMenu<MenuEntry>
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
        private const string COLOR_PICKER_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeColorPicker";

        /// <summary>
        /// Prefab for a string field.
        /// </summary>
        private const string STRING_FIELD_PREFAB = UI_PREFAB_FOLDER + "Input Group - String Input Field";

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
        public int CityIndex;

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
        /// Triggers when the menu needs to be updated.
        ///
        /// E.g. a different city is loaded.
        /// </summary>
        public event Action OnUpdateMenuValues;

        /// <summary>
        /// Triggers when the city is switched.
        /// </summary>
        public event Action<int> OnSwitchCity;

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
        private string ConfigButtonListPath => "ConfigButtons/Content";

        private string CitySwitcherPath => "City Switcher";

        protected override void StartDesktop()
        {
            base.StartDesktop();
            configButtonList = Content.transform.Find(ConfigButtonListPath).gameObject;
            citySwitcher = Content.transform.Find(CitySwitcherPath).GetComponent<HorizontalSelector>();

            SetupCitySwitcher();
        }

        /// <summary>
        /// Updates the menu and adds listeners.
        /// </summary>
        protected override void OnStartFinished()
        {
            base.OnStartFinished();
            city = RuntimeConfigMenu.GetCities()[CityIndex];
            SyncMethod += methodName =>
            {
                if (methodName == nameof(TriggerImmediateRedraw))
                {
                    TriggerImmediateRedraw();
                }
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
            IOrderedEnumerable<MemberInfo> members = city.GetType().GetMembers().
                Where(IsCityAttribute).OrderBy(HasTabAttribute).ThenBy(GetTabName).ThenBy(SortIsNotNested);
            members.ForEach(memberInfo => CreateSetting(memberInfo, null, city));
            SelectEntry(Entries.First());

            // creates the buttons for methods
            IOrderedEnumerable<MethodInfo> methods = city.GetType().GetMethods().Where(IsCityAttribute)
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

            // ordered depending if a setting is primitive or has nested settings
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
                    string => true,
                    _ => false
                };
            }
        }

        /// <summary>
        /// Initializes the horizontal selector for switching cities.
        /// </summary>
        private void SetupCitySwitcher()
        {
            // init the list
            citySwitcher.itemList.Clear();
            citySwitcher.defaultIndex = CityIndex;
            RuntimeConfigMenu.GetCities().ForEach(c => citySwitcher.CreateNewItem(c.name));
            citySwitcher.SetupSelector();

            citySwitcher.selectorEvent.AddListener(index =>
            {
                OnSwitchCity?.Invoke(index);
                citySwitcher.index = CityIndex;
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
        private void CreateButton(MethodInfo methodInfo)
        {
            RuntimeButtonAttribute buttonAttribute =
                methodInfo.GetCustomAttributes().OfType<RuntimeButtonAttribute>().FirstOrDefault();
            // only methods with the button attribute
            if (buttonAttribute == null)
            {
                return;
            }

            // only methods with no parameters
            if (methodInfo.GetParameters().Length > 0)
            {
                return;
            }

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
                UpdateCityMethodNetAction netAction = new()
                {
                    CityIndex = CityIndex,
                    MethodName = methodInfo.Name
                };
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
        /// Returns the view game object.
        /// Adds an entry if necessary.
        /// </summary>
        /// <param name="attributes"></param>
        /// <returns>view game object</returns>
        private GameObject CreateOrGetViewGameObject(IEnumerable<Attribute> attributes)
        {
            // get the tab attribute
            string tabName = attributes.OfType<RuntimeTabAttribute>().FirstOrDefault()?.Name ?? "Misc";
            MenuEntry entry = Entries.FirstOrDefault(entry => entry.Title == tabName);

            // add an entry (tab + view) if necessary
            if (entry == null)
            {
                entry = new MenuEntry(
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
        /// <param name="parent">parent (container game object: <see cref="CreateNestedSetting"/>)</param>
        /// <param name="obj">object which contains the field or property</param>
        private void CreateSetting(MemberInfo memberInfo, GameObject parent, object obj)
        {
            // obsolete members are ignored
            if (memberInfo.GetCustomAttributes().Any(attribute => attribute is ObsoleteAttribute))
            {
                return;
            }

            switch (memberInfo)
            {
                case FieldInfo fieldInfo:
                    // field needs to be editable
                    if (fieldInfo.IsLiteral || fieldInfo.IsInitOnly)
                    {
                        return;
                    }

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
                        !propertyInfo.CanRead || !propertyInfo.CanWrite)
                    {
                        return;
                    }
                    if (propertyInfo.GetMethod.IsAbstract)
                    {
                        return;
                    }

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
        /// <param name="getter">getter of the setting value</param>
        /// <param name="settingName">setting name</param>
        /// <param name="parent">parent game object</param>
        /// <param name="setter">setter of the setting value</param>
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
            if (value == null)
            {
                return;
            }

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
                    Debug.LogWarning("Missing: " + settingName + ", " + value.GetType().GetNiceName() + "\n");
                    break;
            }
        }

        /// <summary>
        /// Creates a container game object which contains multiple settings.
        ///
        /// Uses the `SETTINGS_OBJECT_PREFAB` prefab.
        /// </summary>
        /// <param name="settingName">setting name</param>
        /// <param name="parent">container</param>
        /// <returns>container for child settings</returns>
        private GameObject CreateNestedSetting(string settingName, GameObject parent)
        {
            GameObject container =
                PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB, parent.transform, false);
            container.name = settingName;
            container.GetComponentInChildren<TextMeshProUGUI>().text = settingName;
            return container.transform.Find("Content").gameObject;
        }

        /// <summary>
        /// Creates a slider widget.
        /// </summary>
        /// <param name="settingName">setting name</param>
        /// <param name="range">slider range</param>
        /// <param name="setter">setter of the setting value</param>
        /// <param name="getter">getter of the setting value</param>
        /// <param name="useRoundValue">whether to use round or float values</param>
        /// <param name="parent">parent (container game object: <see cref="CreateNestedSetting"/>)</param>
        /// <param name="recursive">whether it is called recursively (small editor menu)</param>
        /// <param name="getWidgetName">widget name (unique identifier for setting)</param>
        private void CreateSlider(string settingName, RangeAttribute range, UnityAction<float> setter,
            Func<float> getter, bool useRoundValue, GameObject parent,
            bool recursive = false, Func<string> getWidgetName = null)
        {
            // use range 0-2 if non provided
            range ??= new RangeAttribute(0, 2);

            // init the widget
            GameObject sliderGameObject =
                PrefabInstantiator.InstantiatePrefab(SLIDER_PREFAB, parent.transform, false);
            sliderGameObject.name = settingName;
            AddLayoutElement(sliderGameObject);
            SliderManager sliderManager = sliderGameObject.GetComponentInChildren<SliderManager>();
            Slider slider = sliderGameObject.GetComponentInChildren<Slider>();
            TextMeshProUGUI text = sliderGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            text.text = settingName;

            // getter of widget name (if not provided)
            getWidgetName ??= () => sliderGameObject.FullName();

            // slider settings
            sliderManager.usePercent = false;
            sliderManager.useRoundValue = useRoundValue;
            slider.minValue = range.min;
            slider.maxValue = range.max;
            slider.value = getter();

            // add listeners
            RuntimeSliderManager endEditManager = slider.gameObject.AddComponent<RuntimeSliderManager>();

            endEditManager.OnEndEdit += () => setter(slider.value);
            endEditManager.OnEndEdit += () =>
            {
                UpdateCityAttributeNetAction<float> action = new()
                {
                    CityIndex = CityIndex,
                    WidgetPath = getWidgetName(),
                    Value = slider.value
                };
                action.Execute();
            };

            endEditManager.OnEndEdit += CheckImmediateRedraw;

            OnUpdateMenuValues += () =>
            {
                slider.value = getter();
                sliderManager.UpdateUI();
            };

            SyncField += (widgetPath, value) =>
            {
                if (sliderGameObject != null && widgetPath == getWidgetName())
                {
                    setter((float)value);
                    slider.value = (float)value;
                    sliderManager.UpdateUI();
                }
            };

            // add small editor window component
            if (!recursive)
            {
                RuntimeSmallEditorButton smallEditorButton = sliderGameObject.AddComponent<RuntimeSmallEditorButton>();
                smallEditorButton.OnShowMenuChanged += () =>
                {
                    immediateRedraw = smallEditorButton.ShowMenu;
                    ShowMenu = !smallEditorButton.ShowMenu;
                    OnUpdateMenuValues?.Invoke();
                };

                OnShowMenuChanged += () =>
                {
                    if (ShowMenu)
                    {
                        smallEditorButton.ShowMenu = false;
                    }
                };

                smallEditorButton.CreateWidget = smallEditor =>
                    CreateSlider(settingName, range, setter, getter, useRoundValue, smallEditor, true, getWidgetName);
            }
        }

        /// <summary>
        /// Creates switch widget.
        /// </summary>
        /// <param name="settingName">setting name</param>
        /// <param name="setter">setter of the setting value</param>
        /// <param name="getter">getter of the setting value</param>
        /// <param name="parent">parent (container game object: <see cref="CreateNestedSetting"/>)</param>
        /// <param name="recursive">whether it is called recursively (small editor menu)</param>
        /// <param name="getWidgetName">widget name (unique identifier for setting)</param>
        private void CreateSwitch(string settingName, UnityAction<bool> setter, Func<bool> getter, GameObject parent,
            bool recursive = false, Func<string> getWidgetName = null)
        {
            // init the widget
            GameObject switchGameObject =
                PrefabInstantiator.InstantiatePrefab(SWITCH_PREFAB, parent.transform, false);
            switchGameObject.name = settingName;
            AddLayoutElement(switchGameObject);
            SwitchManager switchManager = switchGameObject.GetComponentInChildren<SwitchManager>();
            TextMeshProUGUI text = switchGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            text.text = settingName;

            // getter of widget name (if not provided)
            getWidgetName ??= () => switchGameObject.FullName();

            // switch settings
            switchManager.isOn = getter();
            switchManager.UpdateUI();

            // add listeners
            switchManager.OnEvents.AddListener(() => setter(true));
            switchManager.OnEvents.AddListener(() =>
            {
                UpdateCityAttributeNetAction<bool> action = new()
                {
                    CityIndex = CityIndex,
                    WidgetPath = getWidgetName(),
                    Value = true
                };
                action.Execute();
            });

            switchManager.OnEvents.AddListener(CheckImmediateRedraw);

            switchManager.OffEvents.AddListener(() => setter(false));
            switchManager.OffEvents.AddListener(() =>
            {
                UpdateCityAttributeNetAction<bool> action = new()
                {
                    CityIndex = CityIndex,
                    WidgetPath = getWidgetName(),
                    Value = false
                };
                action.Execute();
            });

            switchManager.OffEvents.AddListener(CheckImmediateRedraw);

            OnUpdateMenuValues += () =>
            {
                switchManager.isOn = getter();
                switchManager.UpdateUI();
            };

            SyncField += (widgetPath, value) =>
            {
                if (switchGameObject != null && widgetPath == getWidgetName())
                {
                    setter((bool)value);
                    switchManager.isOn = (bool)value;
                    switchManager.UpdateUI();
                }
            };

            // add small editor window component
            if (!recursive)
            {
                RuntimeSmallEditorButton smallEditorButton = switchGameObject.AddComponent<RuntimeSmallEditorButton>();

                smallEditorButton.OnShowMenuChanged += () =>
                {
                    immediateRedraw = smallEditorButton.ShowMenu;
                    ShowMenu = !smallEditorButton.ShowMenu;
                    OnUpdateMenuValues?.Invoke();
                };

                OnShowMenuChanged += () =>
                {
                    if (ShowMenu)
                    {
                        smallEditorButton.ShowMenu = false;
                    }
                };

                smallEditorButton.CreateWidget = smallEditor =>
                    CreateSwitch(settingName, setter, getter, smallEditor, true, getWidgetName);
            }
        }

        /// <summary>
        /// Creates a string field widget.
        /// </summary>
        /// <param name="settingName">setting name</param>
        /// <param name="setter">setter</param>
        /// <param name="getter">getter</param>
        /// <param name="parent">parent (container game object: <see cref="CreateNestedSetting"/>)</param>
        /// <param name="recursive">whether it is called recursively (small editor menu)</param>
        /// <param name="getWidgetName">widget name (unique identifier for setting)</param>
        private void CreateStringField(string settingName, UnityAction<string> setter, Func<string> getter,
            GameObject parent, bool recursive = false, Func<string> getWidgetName = null)
        {
            // init the widget
            GameObject stringGameObject =
                PrefabInstantiator.InstantiatePrefab(STRING_FIELD_PREFAB, parent.transform, false);
            stringGameObject.name = settingName;
            AddLayoutElement(stringGameObject);
            TextMeshProUGUI text = stringGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            text.text = settingName;

            // getter of widget name (if not provided)
            getWidgetName ??= () => stringGameObject.FullName();

            // string field settings
            TMP_InputField inputField = stringGameObject.GetComponentInChildren<TMP_InputField>();
            inputField.text = getter();

            // add listeners
            inputField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
            inputField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);

            inputField.onEndEdit.AddListener(setter);
            inputField.onEndEdit.AddListener(changedValue =>
            {
                UpdateCityAttributeNetAction<string> action = new()
                {
                    CityIndex = CityIndex,
                    WidgetPath = getWidgetName(),
                    Value = changedValue
                };
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

            // add small editor window component
            if (!recursive)
            {
                RuntimeSmallEditorButton smallEditorButton = stringGameObject.AddComponent<RuntimeSmallEditorButton>();

                smallEditorButton.OnShowMenuChanged += () =>
                {
                    immediateRedraw = smallEditorButton.ShowMenu;
                    ShowMenu = !smallEditorButton.ShowMenu;
                    OnUpdateMenuValues?.Invoke();
                };

                OnShowMenuChanged += () =>
                {
                    if (ShowMenu)
                    {
                        smallEditorButton.ShowMenu = false;
                    }
                };

                smallEditorButton.CreateWidget = smallEditor =>
                    CreateStringField(settingName, setter, getter, smallEditor, true, getWidgetName);
            }
        }

        /// <summary>
        /// Creates a dropdown widget.
        /// </summary>
        /// <param name="settingName">setting name</param>
        /// <param name="setter">setter of the setting value</param>
        /// <param name="values">dropdown names</param>
        /// <param name="getter">getter of the setting value</param>
        /// <param name="parent">parent (container game object: <see cref="CreateNestedSetting"/>)</param>
        /// <param name="recursive">whether it is called recursively (small editor menu)</param>
        /// <param name="getWidgetName">widget name (unique identifier for setting)</param>
        private void CreateDropDown(string settingName, UnityAction<int> setter, IEnumerable<string> values,
            Func<string> getter, GameObject parent, bool recursive = false, Func<string> getWidgetName = null)
        {
            // convert the value names to an array
            string[] valueArray = values as string[] ?? values.ToArray();

            // init the widget
            GameObject dropDownGameObject =
                PrefabInstantiator.InstantiatePrefab(DROPDOWN_PREFAB, parent.transform, false);
            dropDownGameObject.name = settingName;
            AddLayoutElement(dropDownGameObject);
            TextMeshProUGUI text = dropDownGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            CustomDropdown dropdown = dropDownGameObject.transform.Find("Dropdown").GetComponent<CustomDropdown>();
            text.text = settingName;

            // getter of widget name (if not provided)
            getWidgetName ??= () => dropDownGameObject.FullName();

            // dropdown settings
            dropdown.isListItem = true;
            dropdown.listParent = !recursive ? Menu.transform : Canvas.transform;
            dropdown.selectedItemIndex = Array.IndexOf(valueArray, getter());
            valueArray.ForEach(s => dropdown.CreateNewItemFast(s, null));
            dropdown.SetupDropdown();

            // add listeners
            dropdown.dropdownEvent.AddListener(setter);
            dropdown.dropdownEvent.AddListener(changedValue =>
            {
                UpdateCityAttributeNetAction<int> action = new()
                {
                    CityIndex = CityIndex,
                    WidgetPath = getWidgetName(),
                    Value = changedValue
                };
                action.Execute();
            });

            dropdown.dropdownEvent.AddListener(_ => CheckImmediateRedraw());

            OnUpdateMenuValues += () => dropdown.ChangeDropdownInfo(Array.IndexOf(valueArray, getter()));

            SyncField += (widgetPath, value) =>
            {
                if (dropDownGameObject != null && widgetPath == getWidgetName())
                {
                    setter((int)value);
                    dropdown.ChangeDropdownInfo((int)value);
                }
            };

            // add small editor window component
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

                OnShowMenuChanged += () =>
                {
                    if (ShowMenu)
                    {
                        smallEditorButton.ShowMenu = false;
                    }
                };

                smallEditorButton.CreateWidget = smallEditor =>
                    CreateDropDown(settingName, setter, valueArray, getter, smallEditor, true, getWidgetName);
            }
        }

        /// <summary>
        /// Creates a color picker widget.
        /// </summary>
        /// <param name="settingName">setting name</param>
        /// <param name="parent">parent (container game object: <see cref="CreateNestedSetting"/>)</param>
        /// <param name="setter">setter of the setting value</param>
        /// <param name="getter">getter of the setting value</param>
        /// <param name="recursive">whether it is called recursively (small editor menu)</param>
        /// <param name="getWidgetName">widget name (unique identifier for setting)</param>
        private void CreateColorPicker(string settingName, GameObject parent, UnityAction<Color> setter,
            Func<Color> getter, bool recursive = false, Func<string> getWidgetName = null)
        {
            // init the widget
            GameObject colorPickerGameObject =
                PrefabInstantiator.InstantiatePrefab(COLOR_PICKER_PREFAB, parent.transform, false);
            colorPickerGameObject.name = settingName;
            AddLayoutElement(colorPickerGameObject);

            // Deactivate presets and sliders
            colorPickerGameObject.transform.Find("Presets").gameObject.SetActive(false);
            colorPickerGameObject.transform.Find("Sliders").gameObject.SetActive(false);

            // color picker settings
            ColorPicker colorPicker = colorPickerGameObject.GetComponent<ColorPicker>();
            colorPicker.CurrentColor = getter();
            colorPicker.onValueChanged.AddListener(setter);

            // collapse by default
            if (!recursive)
            {
                colorPickerGameObject.transform.parent.parent.GetComponent<RuntimeConfigMenuCollapse>()
                    .OnClickCollapse();
            }

            // getter of widget name (if not provided)
            getWidgetName ??= () => colorPickerGameObject.FullName();

            // add listeners to BoxSlider
            BoxSlider boxSlider = colorPickerGameObject.GetComponentInChildren<BoxSlider>();
            RuntimeSliderManager boxEndEditManager = boxSlider.gameObject.AddComponent<RuntimeSliderManager>();

            // Add netAction to boxSlider element
            boxEndEditManager.OnEndEdit += () =>
            {
                CheckImmediateRedraw();
                UpdateColorCityFieldNetAction action = new()
                {
                    CityIndex = CityIndex,
                    WidgetPath = getWidgetName(),
                    Value = getter()
                };
                action.Execute();
            };

            // add listeners to Slider
            Slider hueSlider = colorPickerGameObject.GetComponentInChildren<Slider>();
            RuntimeSliderManager hueEndEditManager = hueSlider.gameObject.AddComponent<RuntimeSliderManager>();

            // Add netAction to hueSlider element
            hueEndEditManager.OnEndEdit += () =>
            {
                CheckImmediateRedraw();
                UpdateColorCityFieldNetAction action = new()
                {
                    CityIndex = CityIndex,
                    WidgetPath = getWidgetName(),
                    Value = getter()
                };
                action.Execute();
            };

            // add listeners to TMP_InputField
            TMP_InputField inputField = colorPickerGameObject.GetComponentInChildren<TMP_InputField>();
            inputField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
            inputField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);

            // Add netAction to string input element
            inputField.onEndEdit.AddListener(_ =>
            {
                CheckImmediateRedraw();
                UpdateCityAttributeNetAction<Color> action = new()
                {
                    CityIndex = CityIndex,
                    WidgetPath = getWidgetName(),
                    Value = getter()
                };
                action.Execute();
            });

            // add listeners to OnUpdateMenuValues and SyncField
            OnUpdateMenuValues += () => colorPicker.CurrentColor = getter();
            SyncField += (widgetPath, value) =>
            {
                if (colorPickerGameObject != null && widgetPath == getWidgetName())
                {
                    setter((Color)value);
                    colorPicker.CurrentColor = getter();
                }
            };

            // add small editor window component
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

                OnShowMenuChanged += () =>
                {
                    if (ShowMenu)
                    {
                        smallEditorButton.ShowMenu = false;
                    }
                };

                smallEditorButton.CreateWidget = smallEditor =>
                    CreateColorPicker(settingName, smallEditor, setter, getter, true, getWidgetName);
            }
        }

        /// <summary>
        /// Creates file picker widget.
        /// </summary>
        /// <param name="settingName">setting name</param>
        /// <param name="dataPath">data path (<see cref="FilePath"/> and <see cref="DirectoryPath"/>)</param>
        /// <param name="parent">parent (container game object: <see cref="CreateNestedSetting"/>)</param>
        private void CreateFilePicker(string settingName, DataPath dataPath, GameObject parent)
        {
            // init widget
            FilePicker.FilePicker filePicker = parent.AddComponent<FilePicker.FilePicker>();
            filePicker.DataPathInstance = dataPath;
            filePicker.Label = settingName;
            filePicker.PickingMode = FileBrowser.PickMode.Files;

            // getter of widget name (if not provided)
            string GetWidgetName() => filePicker.gameObject.FullName() + "/" + settingName;

            // add listeners
            OnShowMenuChanged += () => {
                if (!ShowMenu)
                {
                    filePicker.CloseDropdown();
                }

            };
            filePicker.OnMenuInitialized +=
                () => AddLayoutElement(parent.transform.Find(settingName).gameObject);

            // listener when the dropdown or path is changed
            filePicker.OnChangedDropdown += () =>
            {
                UpdateCityAttributeNetAction<int> netAction = new()
                {
                    Value = (int)dataPath.Root,
                    CityIndex = CityIndex,
                    WidgetPath = GetWidgetName()
                };
                netAction.Execute();
            };

            filePicker.OnChangedPath += () =>
            {
                UpdatePathCityFieldNetAction netAction = new()
                {
                    IsAbsolute = dataPath.Root == DataPath.RootKind.Absolute
                };
                netAction.Value = netAction.IsAbsolute ? dataPath.AbsolutePath : dataPath.RelativePath;
                netAction.CityIndex = CityIndex;
                netAction.WidgetPath = GetWidgetName();
                netAction.Execute();
            };

            // listeners for net actions
            SyncPath += (widgetPath, newValue, isAbsolute) =>
            {
                if (widgetPath == GetWidgetName())
                {
                    filePicker.SyncPath(newValue, isAbsolute);
                }
            };

            SyncField += (widgetPath, newValue) =>
            {
                if (widgetPath == GetWidgetName())
                {
                    filePicker.SyncDropdown((int)newValue);
                }
            };
        }

        /// <summary>
        /// Creates a list widget.
        /// </summary>
        /// <param name="list">list to create widget</param>
        /// <param name="parent">parent (container game object: <see cref="CreateNestedSetting"/>)</param>
        private void CreateList(IList<string> list, GameObject parent)
        {
            // init the add and remove buttons
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

            // list settings
            UpdateListChildren(list, parent);
            buttonContainer.transform.SetAsLastSibling();

            // AddButton listener
            addButtonManager.clickEvent.AddListener(() =>
            {
                list.Add("");
                UpdateListChildren(list, parent);
                buttonContainer.transform.SetAsLastSibling();
                AddListElementNetAction netAction = new()
                {
                    CityIndex = CityIndex,
                    WidgetPath = parent.FullName()
                };
                netAction.Execute();
            });
            // RemoveButton listener
            removeButtonManager.clickEvent.AddListener(() =>
            {
                if (list.Count == 0)
                {
                    return;
                }
                list.RemoveAt(list.Count - 1);
                UpdateListChildren(list, parent);
                buttonContainer.transform.SetAsLastSibling();
                RemoveListElementNetAction netAction = new()
                {
                    CityIndex = CityIndex,
                    WidgetPath = parent.FullName()
                };
                netAction.Execute();
            });

            // listeners for net actions
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

            // add listener to update list
            OnUpdateMenuValues += () =>
            {
                UpdateListChildren(list, parent);
                addButton.transform.SetAsLastSibling();
                removeButton.transform.SetAsLastSibling();
            };
        }

        /// <summary>
        /// Updates the elements of a list widget.
        /// </summary>
        /// <param name="list">list elements</param>
        /// <param name="parent">list widget</param>
        private void UpdateListChildren(IList<string> list, GameObject parent)
        {
            // remove children that are no longer part of the list
            foreach (Transform child in parent.transform)
            {
                if (int.TryParse(child.name, out int index))
                {
                    if (index >= list.Count)
                    {
                        Destroyer.Destroy(child.gameObject);
                    }
                }
            }

            // create children for new list elements
            for (int i = 0; i < list.Count; i++)
            {
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

        }

        /// <summary>
        /// Updates the elements of a dictionary widget.
        /// </summary>
        /// <param name="parent">parent (container game object: <see cref="CreateNestedSetting"/>)</param>
        /// <param name="dict">dictionary</param>
        private void UpdateDictChildren(GameObject parent, IDictionary dict)
        {
            // remove children that are no longer part of the dictionary
            foreach (Transform child in parent.transform)
            {
                if (!dict.Contains(child.name))
                {
                    Destroyer.Destroy(child);
                }
            }

            // create children for new dictionary elements
            foreach (object key in dict.Keys)
            {
                if (parent.transform.Find(key.ToString()) == null)
                {
                    CreateSetting(
                        () => dict[key],
                        key.ToString(),
                        parent,
                        changedValue => dict[key] = changedValue
                    );
                }
            }
        }

        /// <summary>
        /// Adds a layout element to a widget.
        ///
        /// Uses the widget size as the minimum layout size.
        /// </summary>
        /// <param name="widget">widget where the layout element is added</param>
        private void AddLayoutElement(GameObject widget)
        {
            LayoutElement layoutElement = widget.AddComponent<LayoutElement>();
            layoutElement.minWidth = ((RectTransform)widget.transform).rect.width;
            layoutElement.minHeight = ((RectTransform)widget.transform).rect.height;
        }

        /// <summary>
        /// Checks whether the city should be immediately redrawn.
        ///
        /// Is used in the small editor window.
        /// <see cref="immediateRedraw"/>
        /// </summary>
        private void CheckImmediateRedraw()
        {
            if (!immediateRedraw)
            {
                return;
            }

            TriggerImmediateRedraw();

            UpdateCityMethodNetAction netAction = new()
            {
                CityIndex = CityIndex,
                MethodName = nameof(TriggerImmediateRedraw)
            };
            netAction.Execute();
        }

        /// <summary>
        /// Immediately redraws the city.
        /// </summary>
        private void TriggerImmediateRedraw()
        {
            // does nothing if no graph is loaded
            if (city.LoadedGraph == null)
            {
                return;
            }

            city.Invoke(nameof(SEECity.LoadData), 0);
            StartCoroutine(DrawNextFrame());

            IEnumerator DrawNextFrame()
            {
                yield return 0;
                city.Invoke(nameof(SEECity.DrawGraph), 0);
            }
        }

        /// <summary>
        /// Assigns a color to a tab button.
        ///
        /// Uses a base brightness and alternately makes that color slightly brighter/darker.
        /// </summary>
        /// <returns>color</returns>
        private Color GetColorForTab()
        {
            int tabCount = ViewList.transform.childCount;

            // default base value
            float baseBrightness = 0.5f;

            // switch slightly between bright and dark
            baseBrightness *= tabCount % 2 == 1 ? 0.75f : 1.25f;

            return Color.Lerp(Color.black, Color.white, baseBrightness);
        }
    }
}
