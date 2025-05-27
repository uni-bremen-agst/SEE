using Cysharp.Threading.Tasks;
using HSVPicker;
using Michsky.UI.ModernUIPack;
using MoreLinq;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.SceneManipulation;
using SEE.GO;
using SEE.GraphProviders;
using SEE.Net.Actions.RuntimeConfig;
using SEE.UI.Menu;
using SEE.UI.Notification;
using SEE.UI.PropertyDialog;
using SEE.Utils;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using SimpleFileBrowser;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SEE.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Menu for configuring a table/city.
    /// </summary>
    public class RuntimeTabMenu : TabMenu<MenuEntry>
    {
        /// <summary>
        /// Path which contains the prefabs for the runtime config menu.
        /// </summary>
        public const string RuntimeConfigPrefabFolder = UIPrefabFolder + "RuntimeConfigMenu/";

        /// <summary>
        /// Prefab for a setting object.
        /// </summary>
        private const string settingsObjectPrefab = RuntimeConfigPrefabFolder + "RuntimeSettingsObject";

        /// <summary>
        /// Prefab for a switch.
        /// </summary>
        private const string switchPrefab = UIPrefabFolder + "Input Group - Switch";

        /// <summary>
        /// Prefab for a slider.
        /// </summary>
        private const string sliderPrefab = UIPrefabFolder + "Input Group - Slider";

        /// <summary>
        /// Prefab for a dropdown.
        /// </summary>
        private const string dropDownPrefab = UIPrefabFolder + "Input Group - Dropdown 2";

        /// <summary>
        /// Prefab for a color picker.
        /// </summary>
        private const string colorPickerPrefab = RuntimeConfigPrefabFolder + "RuntimeColorPicker";

        /// <summary>
        /// Prefab for a string field.
        /// </summary>
        private const string stringFieldPrefab = UIPrefabFolder + "Input Group - String Input Field";

        /// <summary>
        /// Prefab for a button.
        /// </summary>
        private const string buttonPrefab = RuntimeConfigPrefabFolder + "RuntimeConfigButton";

        /// <summary>
        /// Prefab for an add button.
        /// </summary>
        private const string addElementButtonPrefab = RuntimeConfigPrefabFolder + "RuntimeAddButton";

        /// <summary>
        /// Prefab for a remove button.
        /// </summary>
        private const string removeElementButtonPrefab = RuntimeConfigPrefabFolder + "RuntimeRemoveButton";

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
        /// Triggers when a dict entry was added by a different player.
        /// </summary>
        public Action<string, string, string> SyncAddDictEntry;

        /// <summary>
        /// Triggers when a dict entry was removed by a different player.
        /// </summary>
        public Action<string, string> SyncRemoveDictEntry;

        /// <summary>
        /// Triggers when a dict entry should be removed.
        /// </summary>
        public Action<string, string> RemoveDictEntryAction;

        /// <summary>
        /// Prefab for the menu
        /// </summary>
        protected override string MenuPrefab => RuntimeConfigPrefabFolder + "RuntimeConfigMenu";

        /// <summary>
        /// Prefab for a view
        /// </summary>
        protected override string ViewPrefab => RuntimeConfigPrefabFolder + "RuntimeSettingsView";

        /// <summary>
        /// Prefab for a tab button
        /// </summary>
        protected override string EntryPrefab => RuntimeConfigPrefabFolder + "RuntimeTabButton";

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
        private const string configButtonListPath = "ConfigButtons/Content";

        /// <summary>
        /// Path to game object containing the <see cref="citySwitcher"/>.
        /// </summary>
        private const string citySwitcherPath = "City Switcher";

        protected override void StartDesktop()
        {
            base.StartDesktop();
            configButtonList = Content.transform.Find(configButtonListPath).gameObject;
            citySwitcher = Content.transform.Find(citySwitcherPath).GetComponent<HorizontalSelector>();

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
            Debug.Log($"---- City {city.name}");
            // For all *public* fields of city annotated by RuntimeTab.
            // Note that Type.GetMember yields only public members.
            // A member can be a field, property, method, event, or other things.
            city.GetType().GetMembers()
                .Where(IsCityAttribute)
                .OrderBy(HasTabAttribute).ThenBy(GetTabName).ThenBy(SortIsNotNested)
                .ForEach(memberInfo => CreateSetting(memberInfo, null, city));
            SelectEntry(Entries.First());

            // creates the buttons for methods
            city.GetType().GetMethods().Where(IsCityAttribute)
                .OrderBy(GetButtonGroup).ThenBy(GetOrderOfMemberInfo).ThenBy(GetButtonName)
                .ForEach(CreateButton);
            return;

            // methods used for ordering the buttons and settings
            string GetButtonName(MemberInfo memberInfo) => memberInfo.Name;

            string GetTabName(MemberInfo memberInfo) =>
                memberInfo.GetCustomAttributes().OfType<RuntimeTabAttribute>().FirstOrDefault()?.Name;

            // True if memberInfo is declared in a class that is or derives from AbstractSEECity;
            // this is to ignore fields in AbstractSEECity that are inherited from classes from which
            // AbstractSEECity derives.
            // Note: A MemberInfo can be a field, property, method, event and other things.
            bool IsCityAttribute(MemberInfo memberInfo) =>
                memberInfo.DeclaringType == typeof(AbstractSEECity) ||
                memberInfo.DeclaringType!.IsSubclassOf(typeof(AbstractSEECity));

            // True if memberInfo has a RuntimeTab annotation.
            bool HasTabAttribute(MemberInfo memberInfo) =>
                !memberInfo.GetCustomAttributes().Any(a => a is RuntimeTabAttribute);

            float GetOrderOfMemberInfo(MemberInfo memberInfo) =>
                (memberInfo.GetCustomAttributes().OfType<PropertyOrderAttribute>()
                    .FirstOrDefault() ?? new PropertyOrderAttribute()).Order;

            string GetButtonGroup(MemberInfo memberInfo) =>
                (memberInfo.GetCustomAttributes().OfType<RuntimeButtonAttribute>().FirstOrDefault()
                 ?? new RuntimeButtonAttribute(null, null)).Name;

            // ordered depending on whether a setting is primitive or has nested settings
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
            GameObject button = PrefabInstantiator.InstantiatePrefab(buttonPrefab, configButtonList.transform, false);
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
                    SelectAction: () => { },
                    Title: tabName,
                    Description: $"Settings for {tabName}",
                    EntryColor: GetColorForTab(),
                    Icon: Icons.List
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
            if (!ValidateShowIf(memberInfo, obj))
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
                        false,
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
                        false,
                        changedValue => propertyInfo.SetValue(obj, changedValue),
                        memberInfo.GetCustomAttributes()
                    );
                    break;
            }
        }

        /// <summary>
        /// Checks whether the specified <see cref="MemberInfo"/>
        /// has a <see cref="ShowIfAttribute"/> and evaluates its condition.
        /// If the condition is not met, the member should be skipped (not shown).
        /// </summary>
        /// <param name="memberInfo">The member to check.</param>
        /// <param name="obj">The instance that contains the member.</param>
        /// <returns><c>true</c> if the member should be shown. Otherwise, <c>false</c>.</returns>
        private bool ValidateShowIf(MemberInfo memberInfo, object obj)
        {
            if (memberInfo.GetCustomAttribute<ShowIfAttribute>() is { } showIf)
            {
                Debug.Log($"{memberInfo.Name} has a ShowIfAttribut with condition: {showIf.Condition}, value: {showIf.Value}, value null?: {showIf.Value == null}, field? {memberInfo is FieldInfo}, property? {memberInfo is PropertyInfo}, method? {memberInfo is MethodInfo}");
                Type type = obj.GetType();
                FieldInfo field = FindMemberRecursive(type, showIf.Condition, (t, n, flags) => t.GetField(n, flags));
                if (field != null)
                {
                    if (field.FieldType == typeof(bool))
                    {
                        Debug.Log($"{field.Name} has {(bool)field.GetValue(obj)}");
                        if (!(bool)field.GetValue(obj))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (showIf.Value != null)
                        {
                            Debug.Log($"Field value check {Equals(field.GetValue(obj), showIf.Value)}");
                            return Equals(field.GetValue(obj), showIf.Value);
                        }
                    }
                }
                PropertyInfo prop = FindMemberRecursive(type, showIf.Condition, (t, n, flags) => t.GetProperty(n, flags));
                if (prop != null)
                {
                    if (prop.PropertyType == typeof(bool))
                    {
                        Debug.Log($"{prop.Name} has {(bool)prop.GetValue(obj)}");
                        if (!(bool)prop.GetValue(obj))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (showIf.Value != null)
                        {
                            Debug.Log($"Prop value check {Equals(prop.GetValue(obj), showIf.Value)}");
                            return Equals(prop.GetValue(obj), showIf.Value);
                        }
                    }
                }
                MethodInfo m = FindMemberRecursive(type, showIf.Condition, (t, n, flags) => t.GetMethod(n, flags));
                if (m != null && m.ReturnType == typeof(bool))
                {
                    Debug.Log($"{m.Name} has {(bool)m.Invoke(obj, null)}");
                    if (!(bool)m.Invoke(obj, null))
                    {
                        return false;
                    }
                }
            }
            return true;

            T FindMemberRecursive<T>(Type type, string name, Func<Type, string, BindingFlags, T> resolver)
                where T : MemberInfo
            {
                while (type != null)
                {
                    T member = resolver(type, name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (member != null)
                    {
                        return member;
                    }
                    type = type.BaseType;
                }
                return null;
            }
        }

        /// <summary>
        /// Creates a widget for a setting.
        ///
        /// If the setting has nested members, the method is called recursively.
        /// </summary>
        /// <param name="getter">getter of the setting value</param>
        /// <param name="settingName">setting name; in case of a list element, this would be index of the list
        /// element; otherwise this would be the name of the field</param>
        /// <param name="parent">parent game object</param>
        /// <param name="setter">setter of the setting value</param>
        /// <param name="attributes">attributes</param>
        private void CreateSetting(Func<object> getter, string settingName, GameObject parent, bool removable,
            UnityAction<object> setter = null, IEnumerable<Attribute> attributes = null)
        {
            // stores the attributes in an array so it can be accessed multiple times
            Attribute[] attributeArray = attributes?.ToArray() ?? Array.Empty<Attribute>();
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
                    CreateSwitch(settingName,
                        changedValue => setter!(changedValue),
                        () => (bool)getter(),
                        parent);
                    break;
                case int:
                    CreateSlider(settingName,
                        attributeArray.OfType<RangeAttribute>().ElementAtOrDefault(0),
                        changedValue => setter!((int)changedValue),
                        () => (int)getter(),
                        true,
                        parent);
                    break;
                case uint:
                    CreateSlider(settingName,
                        attributeArray.OfType<RangeAttribute>().ElementAtOrDefault(0),
                        changedValue => setter!((uint)changedValue),
                        () => (uint)getter(),
                        true,
                        parent);
                    break;
                case float:
                    CreateSlider(settingName,
                        attributeArray.OfType<RangeAttribute>().ElementAtOrDefault(0),
                        changedValue => setter!(changedValue),
                        () => (float)getter(),
                        false,
                        parent);
                    break;
                case string:
                    CreateStringField(settingName,
                        changedValue => setter!(changedValue),
                        () => (string)getter(),
                        parent);
                    break;
                case Color:
                    parent = CreateNestedSetting(settingName, parent, removable);
                    CreateColorPicker(settingName,
                        parent,
                        changedValue => setter!(changedValue),
                        () => (Color)getter());
                    break;
                case DataPath dataPath:
                    parent = CreateNestedSetting(settingName, parent, removable);
                    CreateFilePicker(settingName, dataPath, parent);
                    break;
                case Enum:
                    CreateDropDown(settingName,
                        // changedValue is the enum value as an integer; here we will
                        // convert it back to the enum. We pass on the value to the
                        // setter of the caller because only the caller has the context to
                        // change the value. Here we have only the knowledge what value was
                        // selected from the drop-down menu.
                        changedValue => setter!(Enum.ToObject(value.GetType(), changedValue)),
                        value.GetType().GetEnumNames(),
                        () => getter().ToString(),
                        parent);
                    break;
                // from here on come nested settings
                case NodeTypeVisualsMap:
                case ColorMap:
                    /// Note: "map" refers to the private attribute map of <see cref="ColorMap"/>.
                    /// We cannot refer to it using nameof(..) because of its private visibility.
                    FieldInfo mapInfo =
                        value.GetType().GetField("map", BindingFlags.Instance | BindingFlags.NonPublic)!;
                    CreateSetting(() => mapInfo.GetValue(value),
                        settingName,
                        parent,
                        removable,
                        null,
                        attributeArray);
                    break;
                case AntennaAttributes:
                    FieldInfo antennaInfo = value.GetType().GetField(nameof(AntennaAttributes.AntennaSections))!;
                    CreateSetting(() => antennaInfo.GetValue(value),
                        settingName,
                        parent,
                        removable,
                        null,
                        attributeArray);
                    break;

                case SingleGraphPipelineProvider:
                    FieldInfo pipeline = value.GetType().GetField(nameof(SingleGraphPipelineProvider.Pipeline))!;
                    CreateSetting(() => pipeline.GetValue(value),
                        settingName,
                        parent,
                        removable,
                        null,
                        attributeArray);
                    break;

                case MultiGraphPipelineProvider:
                    FieldInfo pipeline2 =
                        value.GetType().GetField(nameof(MultiGraphPipelineProvider.Pipeline))!;
                    CreateSetting(() => pipeline2.GetValue(value),
                        settingName,
                        parent,
                        removable,
                        null,
                        attributeArray);
                    break;
                // types that shouldn't be in the configuration menu
                case Graph:
                    break;

                // from here on come nested settings
                case HashSet<string>:
                    // not supported
                    break;
                case IDictionary dict:
                    parent = CreateNestedSetting(settingName, parent, removable, true);
                    CreateDictEntryManagement(parent, dict);
                    UpdateDictChildren(parent, dict);
                    OnUpdateMenuValues += () => UpdateDictChildren(parent, dict);
                    break;
                case IList<string> list:
                    parent = CreateNestedSetting(settingName, parent, removable);
                    CreateList(list, parent, () => string.Empty);
                    break;
                case List<SingleGraphProvider> providerList:
                    parent = CreateNestedSetting(settingName, parent, removable);
                    CreateList(providerList, parent, () => new SingleGraphPipelineProvider());
                    break;
                case List<MultiGraphProvider> providerList:
                    parent = CreateNestedSetting(settingName, parent, removable);
                    CreateList(providerList, parent, () => new MultiGraphPipelineProvider());
                    break;

                // confirmed types where the nested fields should be edited
                case ColorRange:
                case ColorProperty:
                case VisualNodeAttributes:
                case NodeLayoutAttributes:
                case EdgeLayoutAttributes:
                case EdgeSelectionAttributes:
                case ErosionAttributes:
                case BoardAttributes:
                case IncrementalTreeMapAttributes:
                case VisualAttributes:
                case ConfigIO.IPersistentConfigItem:
                case LabelAttributes:
                    parent = CreateNestedSetting(settingName, parent, removable);
                    value.GetType().GetMembers().ForEach(nestedInfo => CreateSetting(nestedInfo, parent, value));
                    break;
                case CSVGraphProvider:
                case DashboardGraphProvider:
                case GXLSingleGraphProvider:
                case JaCoCoGraphProvider:
                case ReflexionGraphProvider:
                    parent = CreateNestedSetting(settingName, parent, removable);
                    CreateTypeField(parent, value as SingleGraphProvider);
                    value.GetType().GetMembers().ForEach(nestedInfo => CreateSetting(nestedInfo, parent, value));
                    break;
                default:
                    Debug.LogWarning($"Missing: {settingName}, {value.GetType().FullName}.\n");
                    break;
            }
        }

        /// <summary>
        /// Creates a container game object which contains multiple settings.
        ///
        /// Uses the <see cref="settingsObjectPrefab"/>.
        /// </summary>
        /// <param name="settingName">setting name</param>
        /// <param name="parent">container</param>
        /// <param name="expandable">Enables the add button to add new items.</param>
        /// <param name="removable">Enables the remove button to remove this entry from its dictionary.</param>
        /// <returns>container for child settings</returns>
        private GameObject CreateNestedSetting(string settingName, GameObject parent,
            bool removable, bool expandable = false)
        {
            GameObject container =
                PrefabInstantiator.InstantiatePrefab(settingsObjectPrefab, parent.transform, false);
            container.name = settingName;
            container.GetComponentInChildren<TextMeshProUGUI>().text = settingName;
            if (expandable)
            {
                EnableAddButton();
            }
            if (removable && CanRemoveKey())
            {
                InitRemoveButton();
            }
            return container.transform.Find("Content").gameObject;

            void EnableAddButton()
            {
                Transform addBtnTransform = container.transform.Find("Buttons/AddBtn");
                addBtnTransform.gameObject.SetActive(true);
            }

            bool CanRemoveKey()
            {
                return !Graph.RootTypes.Contains(settingName);
            }

            void InitRemoveButton()
            {
                Transform removeBtnTransform = container.transform.Find("Buttons/RemoveBtn");
                removeBtnTransform.gameObject.SetActive(true);
                ButtonManagerBasicIcon removeBtn = removeBtnTransform.GetComponent<ButtonManagerBasicIcon>();
                removeBtn.clickEvent.AddListener(() =>
                {
                    RemoveDictEntryAction.Invoke(parent.FullName(), settingName);
                });
            }
        }

        /// <summary>
        /// Adds functionality for managing dictionary entries.
        /// This includes adding new entries as well as removing existing ones.
        /// </summary>
        /// <param name="parent">container</param>
        /// <param name="dict">the dictionary</param>
        private void CreateDictEntryManagement(GameObject parent, IDictionary dict)
        {
            ButtonManagerBasicIcon addBtn = parent.transform.parent.Find("Buttons/AddBtn").GetComponent<ButtonManagerBasicIcon>();
            addBtn.clickEvent.AddListener(() =>
            {
                Type keyType = GetType(true);
                if (keyType != typeof(string))
                {
                    string message = keyType != null ?
                        $"currently {keyType} is not supported as a key value." :
                        "the key type could not be determined since the dictionary is empty.";
                    ShowNotification.Error("Entry cannot be added.", $"The entry cannot be added because {message}");
                    return;
                }
                Type valueType = GetType(false);
                if (valueType != null)
                {
                    AddEntry(valueType).Forget();
                }
                else
                {
                    ShowNotification.Error("Entry cannot be added.", "The entry cannot be added because the " +
                        "value type could not be determined since the dictionary is empty.");
                }
            });

            /// The action to be executed when an entry should be removed from a dictionary.
            /// Additionally, it is checked whether it is a <see cref="VisualNodeAttributes"/>.
            /// If this is the case, it is verified whether it can be removed at all.
            RemoveDictEntryAction += (widgetPath, key) =>
            {
                if (widgetPath == parent.FullName()
                    && dict.Contains(key))
                {
                    if (dict[key].GetType() == typeof(VisualNodeAttributes))
                    {
                        ValidateAndRemoveNodeType((VisualNodeAttributes)dict[key], key);
                    }
                    else
                    {
                        RemoveDictEntry(key);
                    }
                }
            };

            // listeners for net actions; broadcasts the addition of a new dict
            // entry to all clients
            SyncAddDictEntry += (widgetPath, key, valueTypeName) =>
            {
                if (widgetPath == parent.FullName())
                {
                    Type valueType = Type.GetType(valueTypeName);
                    dict.Add(key, Activator.CreateInstance(valueType));
                    UpdateDictChildren(parent, dict);
                }
            };

            // broadcasts the removal of a new dict entry to all cients
            SyncRemoveDictEntry += (widgetPath, key) =>
            {
                if (widgetPath == parent.FullName()
                    && dict.Contains(key))
                {
                    dict.Remove(key);
                    UpdateDictChildren(parent, dict);
                }
            };
            return;

            void RemoveDictEntry(string key)
            {
                dict.Remove(key);
                UpdateDictChildren(parent, dict);
                RemoveDictEntryNetAction netAction = new()
                {
                    CityIndex = CityIndex,
                    WidgetPath = parent.FullName(),
                    Key = key
                };
                netAction.Execute();
            }

            void ValidateAndRemoveNodeType(VisualNodeAttributes nodeType, string key)
            {
                if (city.LoadedGraph == null)
                {
                    ShowNotification.Warn("Node type cannot be deleted.", $"The node type {key} " +
                        "cannot be deleted because the graph is not loaded.");
                    return;
                }
                if (city.LoadedGraph.Nodes().Any(node => node.Type == key))
                {
                    ShowNotification.Warn("Node type cannot be deleted.", $"The node type {key} " +
                        "cannot be deleted because it is used in the graph. Only unused node types can be deleted.");
                }
                else
                {
                    RemoveDictEntry(key);
                }
            }

            /// Returns the desired type from a dictionary, depending on searchKeyType.
            /// First, it checks whether the dictionary being examined has generic arguments for keys and values.
            /// If it’s an inheritance scenario, such as with the <see cref="VisualNodeAttributesMapping"> dictionary,
            /// the second case is used, where the base type is examined.
            /// If the types cannot be determined here either, it tries to extract the types directly from the first entry.
            /// However, this only works if the dictionary is not empty.
            /// If none of the cases can determine the desired type, null is returned.
            Type GetType(bool searchKeyType)
            {
                Type dictType = dict.GetType();
                int i = searchKeyType ? 0 : 1;
                ICollection collection = searchKeyType ? dict.Keys : dict.Values;
                return dictType.GetGenericArguments().Length == 2 ?
                    dictType.GetGenericArguments()[i] : dictType.BaseType.GetGenericArguments().Length == 2 ?
                        dictType.BaseType.GetGenericArguments()[i] : collection.Count > 0 ?
                            collection.Cast<object>().FirstOrDefault()?.GetType() : null;
            }

            async UniTask AddEntry(Type valueType)
            {
                RuntimeMenuAddDictEntryProperty addEntryProperty = new(dict);
                addEntryProperty.Open();
                string key = null;
                await UniTask.WaitUntil(() => addEntryProperty.TryGetKey(out key) || addEntryProperty.WasCanceled());
                if (key != null)
                {
                    dict.Add(key, Activator.CreateInstance(valueType));
                    UpdateDictChildren(parent, dict);
                    AddDictEntryNetAction netAction = new()
                    {
                        CityIndex = CityIndex,
                        WidgetPath = parent.FullName(),
                        Key = key,
                        ValueType = valueType.FullName
                    };
                    netAction.Execute();
                    ImmediateRedraw();
                }
            }
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
                PrefabInstantiator.InstantiatePrefab(sliderPrefab, parent.transform, false);
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
            if (settingName.Equals(nameof(NodeLayoutAttributes.ArchitectureLayoutProportion)))
            {
                sliderManager.mainSlider.onValueChanged.AddListener((f) => RoundAfterFrame().Forget());
            }

            // add and init listeners
            InitSlider(slider.gameObject.AddComponent<RuntimeSliderManager>());

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
                    TwoDecimalPlaces(settingName.Equals(nameof(NodeLayoutAttributes.ArchitectureLayoutProportion)));
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

            async UniTask RoundAfterFrame()
            {
                await UniTask.Yield();
                TwoDecimalPlaces(true);
            }

            void TwoDecimalPlaces(bool condition)
            {
                if (!condition)
                {
                    return;
                }
                if (sliderManager.valueText != null)
                {
                    sliderManager.valueText.text = slider.value.ToString("F2");
                }
                if (sliderManager.popupValueText != null)
                {
                    sliderManager.popupValueText.text = slider.value.ToString("F2");
                }
            }

            void InitSlider(RuntimeSliderManager endEditManager)
            {
                if (!settingName.Equals(nameof(NodeLayoutAttributes.ArchitectureLayoutProportion)))
                {
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
                }
                else
                {
                    endEditManager.OnEndEdit += () =>
                    {
                        if (!ValidateChildrenFitAfterProportionChange())
                        {
                            return;
                        }
                        setter(slider.value);
                        UpdateCityAttributeNetAction<float> action = new()
                        {
                            CityIndex = CityIndex,
                            WidgetPath = getWidgetName(),
                            Value = slider.value
                        };
                        action.Execute();
                        CheckImmediateRedraw();
                    };
                }
            }

            bool ValidateChildrenFitAfterProportionChange()
            {
                SEEReflexionCity rc = (SEEReflexionCity)city;
                GameObject archRoot = rc.ReflexionGraph.ArchitectureRoot.GameObject();
                if (archRoot.transform.localScale.z <= slider.value
                    || archRoot.GetNode().Children().Count() == 0)
                {
                    return true;
                }
                if (archRoot.GetNode().Children().Count() > 0)
                {
                    if (slider.value > 0)
                    {
                        archRoot.GetNode().Children().ForEach(child =>
                            child.GameObject().transform.SetParent(null, true));
                        Vector3 prevScale = archRoot.transform.localScale;
                        Vector3 prevPos = archRoot.transform.position;

                        // Calculates the new position and scale for the architecture node.
                        Transform root = archRoot.ContainingCity().gameObject.transform;
                        float depth = root.lossyScale.z;
                        Vector3 referencePoint = new(root.position.x, archRoot.transform.position.y, root.position.z);
                        referencePoint.z += depth / 2;
                        float length = depth * slider.value;
                        Vector3 position = referencePoint;
                        position.z -= length / 2;
                        archRoot.transform.localScale = new Vector3(1, archRoot.transform.localScale.y, slider.value);
                        archRoot.transform.position = position;
                        // Check whether every child fits within the new architecture's bounds.
                        bool valid = BoundsChecker.ValidateChildrenInBounds(archRoot.GetNode(), () =>
                        {
                            archRoot.transform.localScale = prevScale;
                            archRoot.transform.position = prevPos;
                        });
                        archRoot.GetNode().Children().ForEach(child =>
                            child.GameObject().transform.SetParent(archRoot.transform));
                        if (valid)
                        {
                            immediateRedraw = true;
                            return true;
                        }
                    }
                }
                ShowNotification.Warn("Proportion change failed.",
                                $"The specified value {slider.value} would cause the children to exceed the new boundaries.");
                slider.value = archRoot.transform.localScale.z;
                return false;
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
                PrefabInstantiator.InstantiatePrefab(switchPrefab, parent.transform, false);
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
                PrefabInstantiator.InstantiatePrefab(stringFieldPrefab, parent.transform, false);
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

        private void CreateTypeField(GameObject parent, MultiGraphProvider provider)
        {
            string[] graphProviderKinds = GetGraphProviderKinds();

            CreateDropDown(settingName: "Type",
                setter: Setter,
                values: graphProviderKinds,
                getter: Getter,
                parent: parent);

            // all values of enum GraphProviderKind as strings
            string[] GetGraphProviderKinds()
            {
                return Enum.GetValues(typeof(MultiGraphProviderKind)).Cast<MultiGraphProviderKind>()
                    .Select(e => e.ToString())
                    .ToArray();
            }

            string Getter()
            {
                return provider.GetKind().ToString();
            }

            // index is the index of the changed enum
            void Setter(int index)
            {
                if (Enum.TryParse(graphProviderKinds[index], true, out MultiGraphProviderKind newKind))
                {
                    if (provider.GetKind() != newKind)
                    {
                        // TODO (#698): We need to replace provider in the list it is contained in
                        //              by a new instance of newKind.
                        Debug.LogError("Changing the type of a data provider is currently not supported.\n");
                    }
                }
            }
        }

        private void CreateTypeField(GameObject parent, SingleGraphProvider provider)
        {
            string[] graphProviderKinds = GetGraphProviderKinds();

            CreateDropDown(settingName: "Type",
                setter: Setter,
                values: graphProviderKinds,
                getter: Getter,
                parent: parent);

            // all values of enum GraphProviderKind as strings
            string[] GetGraphProviderKinds()
            {
                return Enum.GetValues(typeof(SingleGraphProviderKind)).Cast<SingleGraphProviderKind>().Select(e => e.ToString())
                    .ToArray();
            }

            string Getter()
            {
                return provider.GetKind().ToString();
            }

            // index is the index of the changed enum
            void Setter(int index)
            {
                if (Enum.TryParse(graphProviderKinds[index], true, out SingleGraphProviderKind newKind))
                {
                    if (provider.GetKind() != newKind)
                    {
                        // TODO (#698): We need to replace provider in the list it is contained in
                        //              by a new instance of newKind.
                        Debug.LogError("Changing the type of a data provider is currently not supported.\n");
                    }
                }
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
                PrefabInstantiator.InstantiatePrefab(dropDownPrefab, parent.transform, false);
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
                PrefabInstantiator.InstantiatePrefab(colorPickerPrefab, parent.transform, false);
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
            FilePicker.DataPathPicker filePicker = parent.AddComponent<FilePicker.DataPathPicker>();
            filePicker.DataPathInstance = dataPath;
            filePicker.Label = settingName;
            filePicker.PickingMode = FileBrowser.PickMode.FilesAndFolders;

            // getter of widget name (if not provided)
            string GetWidgetName() => filePicker.gameObject.FullName() + "/" + settingName;

            // add listeners
            OnShowMenuChanged += () =>
            {
                if (!ShowMenu)
                {
                    filePicker.CloseDropdown();
                }
            };
            filePicker.OnComponentInitialized +=
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
        /// <param name="list">list to create a widget for</param>
        /// <param name="parent">parent (container game object: <see cref="CreateNestedSetting"/>)</param>
        /// <param name="newT">creates a new instance of <typeparamref name="T"/></param>
        /// <typeparam name="T">the type of elements in <paramref name="list"/></typeparam>
        private void CreateList<T>(IList<T> list, GameObject parent, Func<T> newT) where T : class
        {
            // TODO (#698): We want to add and remove elements anywhere, not just at the end.
            //              We want to change the order of elements.

            // init the add and remove buttons
            GameObject buttonContainer = new("ButtonContainer");
            buttonContainer.AddComponent<HorizontalLayoutGroup>();
            buttonContainer.transform.SetParent(parent.transform);

            GameObject addButton =
                PrefabInstantiator.InstantiatePrefab(addElementButtonPrefab, buttonContainer.transform);
            addButton.name = "AddElementButton";
            ButtonManagerWithIcon addButtonManager = addButton.GetComponent<ButtonManagerWithIcon>();

            GameObject removeButton =
                PrefabInstantiator.InstantiatePrefab(removeElementButtonPrefab, buttonContainer.transform);
            removeButton.name = "RemoveElementButton";
            ButtonManagerWithIcon removeButtonManager = removeButton.GetComponent<ButtonManagerWithIcon>();

            // list settings
            UpdateListChildren(list, parent);
            buttonContainer.transform.SetAsLastSibling();

            // AddButton listener adding a new instance at the end of the list
            addButtonManager.clickEvent.AddListener(() =>
            {
                list.Add(newT());
                UpdateListChildren(list, parent);
                buttonContainer.transform.SetAsLastSibling();
                AddListElementNetAction netAction = new()
                {
                    CityIndex = CityIndex,
                    WidgetPath = parent.FullName()
                };
                netAction.Execute();
            });

            // RemoveButton listener removing the last element in the list
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

            // listeners for net actions; broadcasts the addition of a new list
            // element to all clients
            SyncAddListElement += widgetPath =>
            {
                if (widgetPath == parent.FullName())
                {
                    list.Add(newT());
                    UpdateListChildren(list, parent);
                    buttonContainer.transform.SetAsLastSibling();
                }
            };

            // broadcasts the removal of the last list element to all clients
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
                // This case can occur if a dictionary entry has been removed.
                if (parent == null || addButton == null || removeButton == null)
                {
                    return;
                }
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
        private void UpdateListChildren<T>(IList<T> list, GameObject parent) where T : class
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
                    // Note: This iCopy is needed because the lambda expression will otherwise evaluate i
                    //       at the time of its execution, which could be in a future iteration.
                    int iCopy = i;
                    CreateSetting(
                        () => list[iCopy],
                        i.ToString(),
                        parent,
                        false,
                        changedValue => list[iCopy] = changedValue as T
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
                    Destroyer.Destroy(child.gameObject);
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
                        true,
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
        private static void AddLayoutElement(GameObject widget)
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

            ImmediateRedraw();
        }

        /// <summary>
        /// Triggers the immediate redraw <see cref="TriggerImmediateRedraw"/> of the city
        /// locally and on all connected clients (the latter via <see cref="UpdateCityMethodNetAction"/>).
        /// </summary>
        private void ImmediateRedraw()
        {
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
            city.Invoke(nameof(SEECity.ReDrawGraph), 0);
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
