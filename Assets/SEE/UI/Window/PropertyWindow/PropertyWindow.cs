using DG.Tweening;
using Michsky.UI.ModernUIPack;
using MoreLinq;
using SEE.Controls;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Window.PropertyWindow
{
    /// <summary>
    /// Base class for windows showing properties of selected elements.
    /// </summary>
    public abstract class PropertyWindow : BaseWindow
    {
        #region Attributes

        #region Prefabs
        /// <summary>
        /// Prefab for the <see cref="GraphElementPropertyWindow"/>.
        /// </summary>
        private readonly string WindowPrefab = UIPrefabFolder + "PropertyWindow";

        /// <summary>
        /// Prefab for the <see cref="PropertyRowLine"/>.
        /// </summary>
        private readonly string ItemPrefab = UIPrefabFolder + "PropertyRowLine";

        /// <summary>
        /// Prefab for the groups.
        /// </summary>
        private readonly string GroupPrefab = UIPrefabFolder + "PropertyGroupItem";
        #endregion

        /// <summary>
        /// The alpha keys for the gradient of a menu item (fully opaque).
        /// </summary>
        private readonly GradientAlphaKey[] alphaKeys = { new(1, 0), new(1, 1) };

        /// <summary>
        /// The amount by which the text of an item is indented per level.
        /// </summary>
        private const int indentShift = 22;

        /// <summary>
        /// The context menu that is displayed when the user uses the filter, group or sort buttons.
        /// </summary>
        protected PropertyWindowContextMenu contextMenu;

        /// <summary>
        /// Transform of the object containing the items of the property window.
        /// </summary>
        private RectTransform items;

        /// <summary>
        /// The input field in which the user can enter a search term.
        /// </summary>
        private TMP_InputField searchField;

        /// <summary>
        /// The current displayed items of the property window.
        /// </summary>
        private readonly List<GameObject> currentDisplayedItems = new();

        /// <summary>
        /// The dictionary that holds the items for a group.
        /// Key: Unique Group ID (Full Path), Value: List of GameObjects in that group.
        /// </summary>
        protected readonly Dictionary<string, List<GameObject>> groupHolder = new();

        /// <summary>
        /// Special key used by <see cref="AddNestedAttribute"/> to resolve conflicts
        /// where a dictionary already exists at a path that also needs to store a value.
        /// The actual value is stored under this key to preserve the subtree.
        /// </summary>
        private static readonly string valueKey = "_value";

        /// <summary>
        /// A set of all items that have been expanded.
        /// Note that this may contain items that are not currently visible due to collapsed parents.
        /// Such items will be expanded when they become visible again.
        /// </summary>
        protected readonly ISet<string> expandedItems = new HashSet<string>();
        #endregion

        /// <summary>
        /// Starts the desktop instance.
        /// </summary>
        protected override void StartDesktop()
        {
            base.StartDesktop();
            CreateUIInstance();
        }

        #region Search
        /// <summary>
        /// Activates all rows matching the <paramref name="searchQuery"/>.
        /// All others are deactivated. In other words, the <paramref name="searchQuery"/> is applied as
        /// a filter.
        /// </summary>
        /// <param name="searchQuery">Attribute name to search for.</param>
        private void ActivateMatches(string searchQuery)
        {
            Dictionary<string, (string, GameObject)> propertyRows = new();
            // Create mapping of attribute names onto gameObjects representing the corresponding property row.
            foreach (GameObject child in currentDisplayedItems)
            {
                if (!propertyRows.ContainsKey(AttributeName(child)))
                {
                    propertyRows.Add(AttributeName(child), (AttributeValue(child), child));
                }
                else
                {
                    // If the key is already in use.
                    string name = AttributeName(child) + RandomStrings.GetRandomString(10);
                    while (propertyRows.ContainsKey(name))
                    {
                        name = AttributeName(child) + RandomStrings.GetRandomString(10);
                    }
                    propertyRows.Add(name, (AttributeValue(child), child));
                }
            }

            // Remove whitespace.
            searchQuery = searchQuery.Trim();
            if (string.IsNullOrEmpty(searchQuery))
            {
                // There is no search query, so activate the main property rows.
                ActivateMainProperties();
            }
            else
            {
                // First, deactivate all property rows and then activate only those that match the
                // search results.
                SetActive(propertyRows, false);
                foreach (string attributeName in Search(searchQuery, propertyRows))
                {
                    if (propertyRows.TryGetValue(attributeName, out (string v, GameObject activeObject) t))
                    {
                        t.activeObject.SetActive(true);
                        foreach (KeyValuePair<string, List<GameObject>> pair in groupHolder)
                        {
                            if (pair.Value.Contains(t.activeObject))
                            {
                                GameObject group = pair.Value.Find(go => go.name == pair.Key);
                                if (group != null)
                                {
                                    group.SetActive(true);
                                    RotateExpandIcon(group, true);
                                    ActivateParentGroups(group);
                                }
                            }
                        }
                    }
                }
            }

            return;

            void ActivateParentGroups(GameObject group)
            {
                foreach (KeyValuePair<string, List<GameObject>> pair in groupHolder)
                {
                    if (pair.Value.Contains(group))
                    {
                        GameObject parentGroup = pair.Value.Find(go => group.name != pair.Key && GetLevel(go) < GetLevel(group));
                        if (parentGroup != null)
                        {
                            parentGroup.SetActive(true);
                            RotateExpandIcon(parentGroup, true);
                            ActivateParentGroups(parentGroup);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the hierarchy level of the <paramref name="go"/>.
        /// </summary>
        /// <param name="go">The object to be checked.</param>
        /// <returns>The level of the hierarchy.</returns>
        private int GetLevel(GameObject go)
        {
            int level = 0;
            if (go.transform.Find("Foreground") != null)
            {
                level = (int)((RectTransform)go.transform.Find("Foreground")).offsetMin.x / indentShift;
            }
            return level;
        }

        /// <summary>
        /// Activates the main property rows.
        /// </summary>
        private void ActivateMainProperties()
        {
            foreach (KeyValuePair<string, List<GameObject>> pair in groupHolder)
            {
                GameObject group = pair.Value.ToList().Find(go => go.name == pair.Key);
                List<GameObject> childenItems = pair.Value.ToList();
                if (group != null)
                {
                    childenItems.Remove(group);
                    if (IsNoSubgroupOrParentExpanded(group))
                    {
                        group.SetActive(true);
                    }
                    bool expanded = expandedItems.Contains(pair.Key) && IsNoSubgroupOrParentExpanded(group);
                    foreach (GameObject go in childenItems)
                    {
                        go.SetActive(expanded);
                    }
                    RotateExpandIcon(group, expanded);
                }
                else
                {
                    foreach (GameObject go in pair.Value)
                    {
                        go.SetActive(true);
                    }
                }
            }
            return;

            bool IsNoSubgroupOrParentExpanded(GameObject go)
            {
                bool shouldDisplayed = true;
                foreach (KeyValuePair<string, List<GameObject>> pair in groupHolder)
                {
                    if (pair.Value.Contains(go))
                    {
                        GameObject parentGroup = pair.Value.Find(obj => obj.name != go.name && GetLevel(obj) < GetLevel(go));
                        if (parentGroup != null)
                        {
                            shouldDisplayed = expandedItems.Contains(parentGroup.name) && IsNoSubgroupOrParentExpanded(parentGroup);
                        }
                    }
                }
                return shouldDisplayed;
            }
        }

        /// <summary>
        /// Returns the attribute names of all <paramref name="propertyRows"/> whose attribute name or value matches the
        /// <paramref name="query"/>.
        /// </summary>
        /// <param name="query"> The search query (part of an attribute name / value).</param>
        /// <param name="propertyRows"> The dictionary representing property rows to search through.</param>
        /// <returns> The attribute names / values matching the <paramref name="query"/>. </returns>
        private IEnumerable<string> Search(string query, Dictionary<string, (string value, GameObject gameObject)> propertyRows)
        {
            List<string> results = new();
            foreach (string key in propertyRows.Keys)
            {
                // AttributeName instead of key checking, as the key may contain a random string if two identical keys would otherwise exist.
                if (AttributeName(propertyRows[key].gameObject).ToLower().Contains(query.ToLower())
                    || propertyRows[key].value.ToLower().Contains(query.ToLower()))
                {
                    results.Add(key);
                }
            }
            return results;
        }

        /// <summary>
        /// Applies the search if <see cref="searchField.text"/> isn't empty.
        /// </summary>
        protected void ApplySearch()
        {
            if (!string.IsNullOrEmpty(searchField.text) && !string.IsNullOrWhiteSpace(searchField.text))
            {
                ActivateMatches(searchField.text);
            }
        }
        #endregion

        /// <summary>
        /// Sets the activity of the game objects of the given dictionary (<paramref name="objects"/>)
        /// </summary>
        /// <param name="objects">The objects to set their activity.</param>
        /// <param name="activate">The activity.</param>
        private void SetActive(Dictionary<string, (string, GameObject)> objects, bool activate)
        {
            foreach ((_, GameObject go) in objects.Values)
            {
                go.SetActive(activate);

                if (activate)
                {
                    AnimateIn(go);
                }
                else
                {
                    if (go.FindDescendant("Expand Icon") != null)
                    {
                        RotateExpandIcon(go, false);
                    }
                }

                if (expandedItems.Contains(go.name))
                {
                    SetActive(GetDictOfGroup(go.name), activate);
                    RotateExpandIcon(go, activate);
                }
            }
            return;

            // Expands the game object by animating its scale.
            static void AnimateIn(GameObject go)
            {
                go.transform.localScale = new Vector3(1, 0, 1);
                go.transform.DOScaleY(1, duration: 0.5f);
            }
        }

        /// <summary>
        /// Creates the property window.
        /// </summary>
        public void CreateUIInstance()
        {
            // Instantiate PropertyWindow
            GameObject propertyWindow = PrefabInstantiator.InstantiatePrefab(WindowPrefab, Window.transform.Find("Content"), false);
            propertyWindow.name = "Property Window";

            items = (RectTransform)propertyWindow.transform.Find("Content/Items");
            searchField = propertyWindow.transform.Find("Search/SearchField").gameObject.MustGetComponent<TMP_InputField>();

            searchField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
            searchField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);

            ButtonManagerBasic filterButton = propertyWindow.transform.Find("Search/Filter").gameObject.MustGetComponent<ButtonManagerBasic>();
            ButtonManagerBasic sortButton = propertyWindow.transform.Find("Search/Sort").gameObject.MustGetComponent<ButtonManagerBasic>();
            ButtonManagerBasic groupButton = propertyWindow.transform.Find("Search/Group").gameObject.MustGetComponent<ButtonManagerBasic>();
            PopupMenu.PopupMenu popupMenu = gameObject.AddComponent<PopupMenu.PopupMenu>();
            UnityEvent rebuild = new();
            rebuild.AddListener(Rebuild);
            contextMenu = new PropertyWindowContextMenu(popupMenu, rebuild, filterButton, sortButton, groupButton);

            CreateItems();
            searchField.onValueChanged.AddListener(ActivateMatches);
        }

        /// <summary>
        /// Rebuilds the window.
        /// </summary>
        private void Rebuild()
        {
            ClearItems();
            CreateItems();
        }

        /// <summary>
        /// Clears the property window of all items.
        /// </summary>
        private void ClearItems()
        {
            foreach (GameObject go in currentDisplayedItems)
            {
                Destroyer.Destroy(go);
            }
            groupHolder.Clear();
            currentDisplayedItems.Clear();
        }

        /// <summary>
        /// Creates the items (rows) for the attributes.
        /// It populates the window.
        /// </summary>
        protected abstract void CreateItems();

        #region Grouping Name Type
        /// <summary>
        /// Divides the attributes into groups and subgroups.
        /// </summary>
        /// <param name="attributes">The attributes to divide.</param>
        protected void SplitInAttributeGroup(Dictionary<string, object> attributes)
        {
            Dictionary<string, object> nestedDict = new();
            foreach (KeyValuePair<string, object> pair in attributes)
            {
                AddNestedAttribute(nestedDict, pair.Key.Split('.'), pair.Value);
            }
            CreateNestedGroups(nestedDict);
        }

        /// <summary>
        /// Inserts an attribute into the nested dictionary.
        /// Handles conflicts where a key already exists as a value (leaf) but is needed as a container (node).
        /// </summary>
        /// <param name="dict">The nested dictionary.</param>
        /// <param name="keys">The keys path to add.</param>
        /// <param name="value">The value of the attribute.</param>
        private void AddNestedAttribute(Dictionary<string, object> dict, string[] keys, object value)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];

                // Are we at the last element of the path? -> Set value.
                if (i == keys.Length - 1)
                {
                    if (dict.TryGetValue(key, out var existing) && existing is Dictionary<string, object> existingDict)
                    {
                        existingDict[valueKey] = value;
                    }
                    else
                    {
                        dict[key] = value;
                    }
                    return;
                }

                else
                {
                    // We are in the middle of the path -> We need a folder (Dictionary).

                    // 1. If nothing exists yet, create a new folder.
                    if (!dict.TryGetValue(key, out object entry))
                    {
                        dict[key] = new Dictionary<string, object>();
                        entry = dict[key];
                    }


                    // 3. Conflict Check: Is the entry NOT a Dictionary?
                    // (This happens if e.g. "Metric.Lines" was previously inserted as a number,
                    // but now we want to insert "Metric.Lines.LOC").
                    if (!(entry is Dictionary<string, object> subDict))
                    {
                        // Solution: Save the old value and convert the entry into a new folder.
                        subDict = new Dictionary<string, object>();
                        subDict[valueKey] = entry; // Save old value as "_value"
                        dict[key] = subDict;       // Overwrite entry in parent dict
                    }

                    // Descend deeper into the structure
                    dict = subDict;
                }
            }
        }

        /// <summary>
        /// Creates the nested groups and attributes recursively.
        /// Uses the full path as the group ID to ensure uniqueness.
        /// </summary>
        /// <param name="dict">The nested dictionary.</param>
        /// <param name="parentId">The unique ID of the parent group (full path).</param>
        /// <param name="level">The hierarchy level.</param>
        private void CreateNestedGroups(Dictionary<string, object> dict, string parentId = null, int level = 0)
        {
            List<KeyValuePair<string, object>> sortedList = dict.ToList();
            // Sort so that values come first, then subgroups
            sortedList = sortedList.OrderBy(kvp => kvp.Value is Dictionary<string, object> ? 1 : 0).ToList();

            foreach (KeyValuePair<string, object> pair in sortedList)
            {
                // Create a unique ID based on the path (Parent.Child)
                // If parentId is null, it's a root element.
                string uniqueId = string.IsNullOrEmpty(parentId) ? pair.Key : parentId + "." + pair.Key;

                if (pair.Value is Dictionary<string, object> nestedDict)
                {
                    // Call with (Unique ID, Display Label, ...)
                    DisplayGroup(uniqueId, pair.Key, new Dictionary<string, string>(), level, parentId);

                    // Recursion with the new uniqueId as Parent
                    CreateNestedGroups(nestedDict, uniqueId, level + 1);
                }
                else
                {
                    string groupToAddTo = parentId ?? "Header";
                    DisplayAttributes(new Dictionary<string, string>() { { pair.Key, pair.Value.ToString() } },
                        level, expandedItems.Contains(groupToAddTo), groupToAddTo);
                }
            }
        }
        #endregion

        #region Sort
        /// <summary>
        /// Sorts the properties within the group.
        /// </summary>
        protected void Sort()
        {
            if (contextMenu.Sorter.IsActive())
            {
                foreach (IEnumerable<GameObject> values in groupHolder.Values)
                {
                    List<GameObject> list = values.Where(x => x.name.Contains("RowLine")).ToList();
                    if (contextMenu.GroupByName)
                    {
                        List<GameObject> texts = new();
                        List<GameObject> numbers = new();
                        foreach (GameObject obj in list)
                        {
                            string text = AttributeValue(obj);
                            if (int.TryParse(text, out int intResult)
                                || float.TryParse(text, out float floatResult))
                            {
                                numbers.Add(obj);
                            }
                            else
                            {
                                texts.Add(obj);
                            }
                        }
                        if (numbers.Count > 0)
                        {
                            ChangeOrder(numbers);
                        }
                        if (texts.Count > 0)
                        {
                            ChangeOrder(texts);
                        }
                    }
                    else
                    {
                        ChangeOrder(list);
                    }
                }
            }
        }

        /// <summary>
        /// Applies the new order.
        /// </summary>
        /// <param name="listToOrder">The list to be sorted.</param>
        private void ChangeOrder(List<GameObject> listToOrder)
        {
            int lowestSibling = listToOrder.Min(x => x.transform.GetSiblingIndex());
            List<GameObject> sortedHeader = contextMenu.Sorter.ApplySort(listToOrder);
            for (int i = 0; i < sortedHeader.Count; i++)
            {
                sortedHeader[i].transform.SetSiblingIndex(lowestSibling);
                lowestSibling += 1;
            }
        }
        #endregion

        /// <summary>
        /// Overload for backward compatibility.
        /// Uses the same value for both the unique group ID and the display label.
        /// Use this overload only if naming collisions are not possible.
        /// </summary>
        /// <typeparam name="T">The type of the attribute values.</typeparam>
        /// <param name="name">
        /// The group name, used as both the unique internal ID and the visible label.
        /// </param>
        /// <param name="attributes">
        /// The attributes belonging to this group, mapped by attribute name.
        /// </param>
        /// <param name="level">
        /// The hierarchy level of the group, used for indentation.
        /// </param>
        /// <param name="parentGroup">
        /// The unique ID of the parent group, or <c>null</c> if this is a root group.
        /// </param>
        protected void DisplayGroup<T>(
            string name,
            Dictionary<string, T> attributes,
            int level = 0,
            string parentGroup = null)
        {
            // Forward the call and use 'name' as both the unique ID and the display label.
            DisplayGroup(name, name, attributes, level, parentGroup);
        }


        /// <summary>
        /// Displays an attribute group and its corresponding attributes with their values.
        /// </summary>
        /// <typeparam name="T">The type of the attribute values.</typeparam>
        /// <param name="id">The unique ID for the group (e.g. full path "Metric.Lines.LOC").</param>
        /// <param name="label">The display text (e.g. "LOC").</param>
        /// <param name="attributes">A dictionary containing attribute names (keys) and their corresponding values (values).</param>
        /// <param name="level">The level for the group.</param>
        /// <param name="parentGroup">The parent group of this group, if none exists, null is used.</param>
        protected void DisplayGroup<T>(string id, string label, Dictionary<string, T> attributes, int level = 0, string parentGroup = null)
        {
            GameObject group = PrefabInstantiator.InstantiatePrefab(GroupPrefab, items, false);
            group.name = id; // Internal ID (Unique)

            // Display Text (Short)
            group.FindDescendant("AttributeLine").MustGetComponent<TextMeshProUGUI>().text = label;

            Dictionary<string, (string, GameObject gameObject)> dict = DisplayAttributes(attributes, level + 1, expandedItems.Contains(group.name));
            OrderGroup();
            RotateExpandIcon(group, expandedItems.Contains(group.name), 0.01f);
            RegisterClickHandler();

            // Add to groupHolder map using unique ID
            groupHolder.Add(id, dict.Values.Select(x => x.gameObject).Append(group).ToList());
            currentDisplayedItems.Add(group);
            return;

            void RegisterClickHandler()
            {
                if (group.TryGetComponentOrLog(out PointerHelper pointerHelper))
                {
                    // expands/collapses the group item
                    pointerHelper.ClickEvent.AddListener(e =>
                    {
                        Dictionary<string, (string, GameObject gameObject)> newDict = GetDictOfGroup(group.name);
                        if (newDict.Values.Any(entry => entry.gameObject.activeInHierarchy))
                        {
                            expandedItems.Remove(group.name);
                            SetActive(newDict, false);
                            RotateExpandIcon(group, false);
                        }
                        else
                        {
                            expandedItems.Add(group.name);
                            SetActive(newDict, true);
                            ApplySearch();
                            RotateExpandIcon(group, true);
                        }
                    });
                }
            }

            void OrderGroup()
            {
                if (level > 0)
                {
                    RectTransform background = (RectTransform)group.transform.Find("Background");
                    background.offsetMin = background.offsetMin.WithXY(x: indentShift * level);
                    RectTransform foreground = (RectTransform)group.transform.Find("Foreground");
                    foreground.offsetMin = foreground.offsetMin.WithXY(x: indentShift * level);
                    if (parentGroup != null && groupHolder.TryGetValue(parentGroup, out List<GameObject> value))
                    {
                        value.Add(group);
                        group.SetActive(expandedItems.Contains(parentGroup));
                    }
                }
            }
        }

        /// <summary>
        /// Retrieve the dictionary of items from a group, excluding the group itself.
        /// </summary>
        /// <param name="groupName">The group name (ID).</param>
        /// <returns>A created dictionary of the group.</returns>
        private Dictionary<string, (string, GameObject)> GetDictOfGroup(string groupName)
        {
            return groupHolder.GetValueOrDefault(groupName)
                .Where(x => x.name != groupName)
                .ToDictionary(AttributeName, go => (AttributeValue(go), go));
        }

        /// <summary>
        /// Rotates the expand icon of a group.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="expanded">Whether the group should be expanded or not.</param>
        private void RotateExpandIcon(GameObject group, bool expanded, float duration = 0.5f)
        {
            if (group.FindDescendant("Expand Icon").TryGetComponentOrLog(out RectTransform transform))
            {
                Vector3 endValue = expanded ? new Vector3(0, 0, -180) : new Vector3(0, 0, -90);
                transform.DORotate(endValue, duration: duration);
            }
        }

        /// <summary>
        /// Displays the attributes and their corresponding values.
        /// </summary>
        /// <typeparam name="T">The type of the attribute values.</typeparam>
        /// <param name="attributes">A dictionary containing attribute names (keys) and their corresponding values (values).</param>
        /// <param name="level">The level for the property row.</param>
        /// <param name="active">Whether the attributes should be active.</param>
        /// <param name="group">The group to which this attribute is to be assigned.
        /// Used only if the attribute is to be added to the group later.</param>
        protected Dictionary<string, (string, GameObject)> DisplayAttributes<T>(Dictionary<string, T> attributes,
            int level = 0, bool active = true, string group = null)
        {
            Dictionary<string, (string, GameObject)> dict = new();
            foreach ((string name, T value) in attributes)
            {
                // Create GameObject
                GameObject propertyRow = PrefabInstantiator.InstantiatePrefab(ItemPrefab, items, false);
                // Attribute Name
                Attribute(propertyRow).text = name;
                // Value Name
                Value(propertyRow).text = value.ToString();
                // Colors and orders the item
                ColorOrderItem();
                dict.Add(name, (AttributeValue(propertyRow), propertyRow));
                currentDisplayedItems.Add(propertyRow);
                if (group != null && groupHolder.TryGetValue(group, out List<GameObject> groupList))
                {
                    groupList.Add(propertyRow);
                }
                continue;

                void ColorOrderItem()
                {
                    Color[] gradient = new[] { Color.white, Color.white.Darker() };
                    RectTransform background = (RectTransform)propertyRow.transform.Find("Background");
                    background.GetComponent<UIGradient>().EffectGradient.SetKeys(gradient.ToGradientColorKeys().ToArray(), alphaKeys);
                    background.offsetMin = background.offsetMin.WithXY(x: indentShift * level);
                    RectTransform foreground = (RectTransform)propertyRow.transform.Find("Foreground");
                    foreground.offsetMin = foreground.offsetMin.WithXY(x: indentShift * level);
                    propertyRow.SetActive(active);
                }
            }
            return dict;
        }

        #region Getter
        /// <summary>
        /// Returns the name of a node attribute stored in the first child of the <paramref name="propertyRow"/>.
        /// Parameter <paramref name="propertyRow"/> is assumed to represent a row in the property window providing
        /// the name and value of a node attribute (property).
        /// </summary>
        /// <param name="propertyRow">A game object representing a pair of an attribute name and an attribute value.</param>
        /// <returns>Name of the node attribute.</returns>
        private string AttributeName(GameObject propertyRow)
        {
            return Attribute(propertyRow).text;
        }

        /// <summary>
        /// Returns the TMP container of <paramref name="propertyRow"/> holding the attribute name.
        /// Parameter <paramref name="propertyRow"/> is assumed to represent a row in the property window providing
        /// the name and value of a node attribute (property).
        /// </summary>
        /// <param name="propertyRow">A game object representing a pair of an attribute name and an attribute value.</param>
        /// <returns>The TMP holding the attribute name.</returns>
        private TextMeshProUGUI Attribute(GameObject propertyRow)
        {
            return propertyRow.FindDescendant("AttributeLine").MustGetComponent<TextMeshProUGUI>();
        }

        /// <summary>
        /// Returns the value of a node attribute.
        /// </summary>
        /// <param name="propertyRow">A game object representing a pair of an attribute name and an attribute value.</param>
        /// <returns>Value of the node attribute or the name of the node attribute if it does not have a value (SubMenuButton). </returns>
        private string AttributeValue(GameObject propertyRow)
        {
            return Value(propertyRow) != null ? Value(propertyRow).text : AttributeName(propertyRow);
        }

        /// <summary>
        /// Returns the TMP container of <paramref name="propertyRow"/> holding the attribute value.
        /// Parameter <paramref name="propertyRow"/> is assumed to represent a row in the property window providing
        /// the name and value of a node attribute (property).
        /// </summary>
        /// <param name="propertyRow">A game object representing a pair of an attribute name and an attribute value.</param>
        /// <returns>The TMP holding the attribute value.</returns>
        private TextMeshProUGUI Value(GameObject propertyRow)
        {
            return propertyRow.FindDescendant("ValueLine")?.MustGetComponent<TextMeshProUGUI>();
        }
        #endregion

        #region BaseWindow
        public override void RebuildLayout()
        {
            // Nothing needs to be done.
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            throw new NotImplementedException();
        }

        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            throw new NotImplementedException();
        }

        public override WindowValues ToValueObject()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}