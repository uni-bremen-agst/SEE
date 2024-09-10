using DG.Tweening;
using FuzzySharp;
using Michsky.UI.ModernUIPack;
using MoreLinq;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game.Drawable;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using static RootMotion.FinalIK.RagdollUtility;

namespace SEE.UI.Window.PropertyWindow
{
    /// <summary>
    /// Represents a movable, scrollable window containing properties of a <see cref="GraphElement"/>.
    /// It consists of a search field and a list of properties, where each property is represented by a row
    /// holding the attribute name and its value.
    /// </summary>
    public class PropertyWindow : BaseWindow
    {
        /// <summary>
        /// GraphElement whose properties are to be shown.
        /// </summary>
        public GraphElement GraphElement;

        /// <summary>
        /// Prefab for the <see cref="PropertyWindow"/>.
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

        /// <summary>
        /// The alpha keys for the gradient of a menu item (fully opaque).
        /// </summary>
        private readonly GradientAlphaKey[] alphaKeys = { new(1, 0), new(1, 1) };

        /// <summary>
        /// The amount by which the text of an item is indented per level.
        /// </summary>
        private const int indentShift = 22;

        /// <summary>
        /// The context menu that is displayed when the user uses the filter, gorup or sort buttons.
        /// </summary>
        private PropertyWindowContextMenu contextMenu;

        /// <summary>
        /// Transform of the object containing the items of the property window.
        /// </summary>
        private RectTransform items;

        /// <summary>
        /// The input field in which the user can enter a search term.
        /// </summary>
        private TMP_InputField searchField;

        /// <summary>
        /// The dictionary that holds the items for a group.
        /// </summary>
        private readonly Dictionary<string, IEnumerable<GameObject>> groupHolder = new();

        /// <summary>
        /// A set of all items that have been expanded.
        /// Note that this may contain items that are not currently visible due to collapsed parents.
        /// Such items will be expanded when they become visible again.
        /// </summary>
        private readonly ISet<string> expandedItems = new HashSet<string>();

        protected override void StartDesktop()
        {
            base.StartDesktop();
            CreateUIInstance();
        }

        /// <summary>
        /// Activates all <paramref name="propertyRows"/> if they match the <paramref name="searchQuery"/>.
        /// All others are deactivated. In other words, the <paramref name="searchQuery"/> is applied as
        /// a filter.
        /// </summary>
        /// <param name="searchQuery"> attribute name to search for </param>
        /// <param name="propertyRows">mapping of attribute names onto gameObjects representing
        /// the corresponding property row</param>
        private void ActivateMatches(string searchQuery)
        {
            Dictionary<string, (string, GameObject)> propertyRows = new();
            /// Create mapping of attribute names onto gameObjects representing the corresponding property row.
            foreach (Transform child in items)
            {
                propertyRows.Add(AttributeName(child.gameObject), (AttributeValue(child.gameObject), child.gameObject));
            }

            /// Remove whitespace.
            searchQuery = searchQuery.Trim();
            if (string.IsNullOrEmpty(searchQuery))
            {
                /// There is no search query, so activate all property rows.
                SetActive(propertyRows, true);
            }
            else
            {
                /// First, deactivate all property rows and then activate only those that match the
                /// search results.
                SetActive(propertyRows, false);
                foreach (string attributeName in Search(searchQuery, propertyRows))
                {
                    if (propertyRows.TryGetValue(attributeName, out (string v, GameObject activeObject) t))
                    {
                        t.activeObject.SetActive(true);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the activity of the game objects of the given dictionary (<paramref name="objects"/>)
        /// </summary>
        /// <param name="objects">The objects to set their activity.</param>
        /// <param name="activate">The activity</param>
        private void SetActive(Dictionary<string, (string, GameObject)> objects, bool activate)
        {
            foreach ((_, GameObject go) in objects.Values)
            {
                go.SetActive(activate);
                AnimateIn(go);
            }
            return;

            /// Expands the game object by animating its scale.
            void AnimateIn(GameObject go)
            {
                go.transform.localScale = new Vector3(1, 0, 1);
                go.transform.DOScaleY(1, duration: 0.5f);
            }
        }

        /// <summary>
        /// Returns the attribute names of all <paramref name="propertyRows"/> whose attribute name or value matches the
        /// <paramref name="query"/>.
        /// </summary>
        /// <param name="query"> the search query (part of an attribute name / value)</param>
        /// <param name="propertyRows"> the dictionary representing property rows to search through</param>
        /// <returns> the attribute names / values matching the <paramref name="query"/> </returns>
        private IEnumerable<string> Search(string query, Dictionary<string, (string value, GameObject gameObject)> propertyRows)
        {
            List<string> results = new();
            foreach (string key in propertyRows.Keys)
            {
                if (key.ToLower().Contains(query.ToLower()) || propertyRows[key].value.ToLower().Contains(query.ToLower()))
                {
                    results.Add(key);
                }
            }
            return results;
        }

        /// <summary>
        /// Returns the name of a node attribute stored in the first child of the <paramref name="propertyRow"/>.
        /// Parameter <paramref name="propertyRow"/> is assumed to represent a row in the property window providing
        /// the name and value of a node attribute (property).
        /// </summary>
        /// <param name="propertyRow">a game object representing a pair of an attribute name and an attribute value</param>
        /// <returns>name of the node attribute</returns>
        private string AttributeName(GameObject propertyRow)
        {
            return Attribute(propertyRow).text;
        }

        /// <summary>
        /// Returns the TMP container of <paramref name="propertyRow"/> holding the attribute name.
        /// Parameter <paramref name="propertyRow"/> is assumed to represent a row in the property window providing
        /// the name and value of a node attribute (property).
        /// </summary>
        /// <param name="propertyRow">a game object representing a pair of an attribute name and an attribute value</param>
        /// <returns>the TMP holding the attribute name</returns>
        private TextMeshProUGUI Attribute(GameObject propertyRow)
        {
            return GameFinder.FindChild(propertyRow, "AttributeLine").MustGetComponent<TextMeshProUGUI>();
        }

        private string AttributeValue(GameObject propertyRow)
        {
            return Value(propertyRow) != null ? Value(propertyRow).text : AttributeName(propertyRow);
        }

        /// <summary>
        /// Returns the TMP container of <paramref name="propertyRow"/> holding the attribute value.
        /// Parameter <paramref name="propertyRow"/> is assumed to represent a row in the property window providing
        /// the name and value of a node attribute (property).
        /// </summary>
        /// <param name="propertyRow">a game object representing a pair of an attribute name and an attribute value</param>
        /// <returns>the TMP holding the attribute value</returns>
        private TextMeshProUGUI Value(GameObject propertyRow)
        {
            return GameFinder.FindChild(propertyRow, "ValueLine")?.MustGetComponent<TextMeshProUGUI>();
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
            rebuild.AddListener(() => Rebuild());
            contextMenu = new PropertyWindowContextMenu(popupMenu, rebuild, filterButton, sortButton, groupButton);

            CreateItems();
            searchField.onValueChanged.AddListener(searchQuery => ActivateMatches(searchQuery));
        }

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
            for (int i = 0; i < items.childCount;)
            {
                DestroyImmediate(items.GetChild(i).gameObject);
            }
            groupHolder.Clear();
        }

        /// <summary>
        /// Creates the items (rows) for the attributes.
        /// It populates the window.
        /// </summary>
        private void CreateItems()
        {
            if (contextMenu.Filter.IncludeHeader)
            {
                Dictionary<string, string> header = new()
                {
                    { "Kind", GraphElement is Node ? "Node" : "Edge" },
                };
                if (GraphElement is Edge edge)
                {
                    header.Add("ID", edge.ID);
                    header.Add("Source", edge.Source.ID);
                    header.Add("Target", edge.Target.ID);
                }
                header.Add("Type", GraphElement.Type);

                /// Data Attributes
                Dictionary<string, (string, GameObject gameObject)> headerItems = DisplayAttributes(header);
                groupHolder.Add("Header", headerItems.Values.Select(x => x.gameObject));
            }
            /// Toggle Attributes
            if (GraphElement.ToggleAttributes.Count > 0 && contextMenu.Filter.IncludeToggleAttributes)
            {
                DisplayGroup("Toggle Attributes", GraphElement.ToggleAttributes.ToDictionary(item => item, item => true));
            }

            /// String Attributes
            if (GraphElement.StringAttributes.Count > 0 && contextMenu.Filter.IncludeStringAttributes)
            {
                DisplayGroup("String Attributes", GraphElement.StringAttributes);
            }

            /// Int Attributes
            if (GraphElement.IntAttributes.Count > 0 && contextMenu.Filter.IncludeIntAttributes)
            {
                DisplayGroup("Int Attributes", GraphElement.IntAttributes);
            }

            /// Float Attributes
            if (GraphElement.FloatAttributes.Count > 0 && contextMenu.Filter.IncludeFloatAttributes)
            {
                DisplayGroup("Float Attributes", GraphElement.FloatAttributes);
            }

            /// Sorts the properties
            Sort();
        }

        /// <summary>
        /// Sorts the properties within the group.
        /// </summary>
        private void Sort()
        {
            if (contextMenu.Sorter.IsActive())
            {
                foreach (IEnumerable<GameObject> values in groupHolder.Values)
                {
                    List<GameObject> list = values.ToList();
                    list.RemoveAll(x => !x.name.Contains("RowLine"));
                    ChangeOrder(list);
                }
            }
        }

        /// <summary>
        /// Applies the new order.
        /// </summary>
        /// <param name="listToOrder">The list to be sorted.</param>
        private void ChangeOrder(List<GameObject> listToOrder)
        {
            int lowestSilbing = listToOrder.Min(x => x.transform.GetSiblingIndex());
            List<GameObject> sortedHeader = contextMenu.Sorter.ApplySort(listToOrder);
            for (int i = 0; i < sortedHeader.Count; i++)
            {
                sortedHeader[i].transform.SetSiblingIndex(lowestSilbing);
                lowestSilbing += 1;
            }
        }

        /// <summary>
        /// Displays a attribute group and their corresponding attributes with their values.
        /// </summary>
        /// <typeparam name="T">The type of the attribute values.</typeparam>
        /// <param name="name">The group name</param>
        /// <param name="attributes">A dictionary containing attribute names (keys) and their corresponding values (values).</param>
        /// <param name="level">The level for the group.</param>
        private void DisplayGroup<T>(string name, Dictionary<string, T> attributes, int level = 0)
        {
            GameObject group = PrefabInstantiator.InstantiatePrefab(GroupPrefab, items, false);
            group.name = name;
            GameFinder.FindChild(group, "AttributeLine").MustGetComponent<TextMeshProUGUI>().text = name;
            Dictionary<string, (string, GameObject gameObject)> dict = DisplayAttributes(attributes, level + 1, expandedItems.Contains(group.name));
            RegisterClickHandler();
            groupHolder.Add(name, dict.Values.Select(x => x.gameObject).Append(group));
            return;

            void RegisterClickHandler()
            {
                if (group.TryGetComponentOrLog(out PointerHelper pointerHelper))
                {
                    /// expands/collapses the group item
                    pointerHelper.ClickEvent.AddListener(e =>
                    {
                        if (dict.First().Value.gameObject.activeInHierarchy)
                        {
                            expandedItems.Remove(group.name);
                            SetActive(dict, false);
                            if (GameFinder.FindChild(group, "Expand Icon").TryGetComponentOrLog(out RectTransform transform))
                            {
                                transform.DORotate(new Vector3(0, 0, -90), duration: 0.5f);
                            }
                        }
                        else
                        {
                            expandedItems.Add(group.name);
                            SetActive(dict, true);
                            if (GameFinder.FindChild(group, "Expand Icon").TryGetComponentOrLog(out RectTransform transform))
                            {
                                transform.DORotate(new Vector3(0, 0, -180), duration: 0.5f);
                            }
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Displays the attributes and their corresponding values.
        /// </summary>
        /// <typeparam name="T">The type of the attribute values.</typeparam>
        /// <param name="attributes">A dictionary containing attribute names (keys) and their corresponding values (values).</param>
        /// <param name="level">The level for the property row.</param>
        /// <param name="active">Whether the attributes should be active.</param>
        private Dictionary<string, (string, GameObject)> DisplayAttributes<T>(Dictionary<string, T> attributes, int level = 0, bool active = true)
        {
            Dictionary<string, (string, GameObject)> dict = new();
            foreach ((string name, T value) in attributes)
            {
                /// Create GameObject
                GameObject propertyRow = PrefabInstantiator.InstantiatePrefab(ItemPrefab, items, false);
                /// Attribute Name
                Attribute(propertyRow).text = name;
                /// Value Name
                Value(propertyRow).text = value.ToString();
                /// Colors and orders the item
                ColorOrderItem();
                dict.Add(name, (AttributeValue(propertyRow), propertyRow));
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
    }
}
