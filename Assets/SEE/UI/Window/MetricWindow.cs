using FuzzySharp;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace SEE.UI.Window
{
    /// <summary>
    /// Represents a movable, scrollable window containing metrics of a <see cref="GraphElement"/>.
    /// </summary>
    public class MetricWindow : BaseWindow
    {
        /// <summary>
        /// GraphElement whose metrics are to be shown.
        /// </summary>
        public GraphElement GraphElement;

        /// <summary>
        /// Prefab for the <see cref="MetricWindow"/>.
        /// </summary>
        private static string WindowPrefab => UIPrefabFolder + "MetricWindow";

        /// <summary>
        /// Prefab for the <see cref="MetricRowLine"/>.
        /// </summary>
        private static string ItemPrefab => UIPrefabFolder + "MetricRowLine";

        protected override void StartDesktop()
        {
            base.StartDesktop();
            CreateUIInstance();
        }

        /// <summary>
        /// Searches for a specified string within <paramref name="elements"/>.
        /// Activates or deactivates them based on the search results.
        /// GameObjects are considered "active" when the search string has been found within their names.
        /// </summary>
        /// <param name="searchQuery"> string to search for </param>
        /// <param name="searchableObjects"> Dictionary of GameObjects to search in </param>
        private void InputSearchField(string searchQuery, Dictionary<string, GameObject> searchableObjects)
        {
            // Remove Whitespaces
            searchQuery = searchQuery.Trim();
            if (searchQuery == null || searchQuery.Trim().Length == 0)
            {
                foreach (var ele in searchableObjects)
                {
                    ele.Value.SetActive(true);
                }
            }
            else
            {
                // Deactivate every GameObject
                IEnumerable<string> searchList = Search(searchQuery, searchableObjects.Values.ToArray());
                foreach(var ele in searchableObjects)
                {
                    ele.Value.SetActive(false);
                }
                // Activate GameObjects that match the search results from the fuzzy search.
                foreach (string ele in searchList)
                {
                    searchableObjects.TryGetValue(ele, out GameObject activeObject);
                    activeObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Returns the attributes of all <paramref name="gameObjects"/> whose name match the
        /// <paramref name="query"/> using a fuzzy search.
        /// </summary>
        /// <param name="query"> the search query (part of an attribute name)</param>
        /// <param name="gameObjects"> the game objects to search through</param>
        /// <param name="cutoff"> representing the cutoff score for relevance </param>
        /// <returns> the attributes whose names match the <paramref name="query"/> </returns>
        public IEnumerable<string> Search(string query, GameObject[] gameObjects, int limit = 15, int cutoff = 40)
        {
            List<string> attributesList = gameObjects.Select(AttributeName).ToList();
            IEnumerable<(int score, string attribute)> searchResults
                = Process.ExtractTop(query, attributesList, limit: limit, cutoff: cutoff).Select(x => (x.Score, x.Value));
            searchResults = searchResults.OrderByDescending(x => x.score);

            return searchResults.Select(x => x.attribute);
        }

        /// <summary>
        /// Returns the name of a node attribute stored in the first child of the <paramref name="metricRow"/>.
        /// Parameter <paramref name="metricRow"/> is assumed to represent a row in the metric window providing
        /// the name and value of a node attribute (metric).
        /// </summary>
        /// <param name="metricRow">a game object representing a pair of an attribute name and an attribute value</param>
        /// <returns>name of the node attribute</returns>
        private static string AttributeName(GameObject metricRow)
        {
            return Attribute(metricRow).text;
        }

        /// <summary>
        /// Returns the TMP container of <paramref name="metricRow"/> holding the attribute name.
        /// Parameter <paramref name="metricRow"/> is assumed to represent a row in the metric window providing
        /// the name and value of a node attribute (metric).
        /// </summary>
        /// <param name="metricRow">a game object representing a pair of an attribute name and an attribute value</param>
        /// <returns>the TMP holding the attribute name</returns>
        /// <remarks>Assumes that the attribute name is stored in the first child of the metric row.</remarks>
        private static TextMeshProUGUI Attribute(GameObject metricRow)
        {
            return metricRow.transform.GetChild(0).MustGetComponent<TextMeshProUGUI>();
        }

        /// <summary>
        /// Returns the TMP container of <paramref name="metricRow"/> holding the attribute value.
        /// Parameter <paramref name="metricRow"/> is assumed to represent a row in the metric window providing
        /// the name and value of a node attribute (metric).
        /// </summary>
        /// <param name="metricRow">a game object representing a pair of an attribute name and an attribute value</param>
        /// <returns>the TMP holding the attribute value</returns>
        /// <remarks>Assumes that the attribute name is stored in the second child of the metric row.</remarks>
        private static TextMeshProUGUI Value(GameObject metricRow)
        {
            return metricRow.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        }

        /// <summary>
        /// The GameObject representing the metric window. It must be stored in this field;
        /// otherwise, it will be garbage collected.
        /// </summary>
        private GameObject metricWindowObject;

        /// <summary>
        /// Creates the Metric Window
        /// </summary>
        public void CreateUIInstance()
        {
            // Instantiate MetricWindow
            metricWindowObject = PrefabInstantiator.InstantiatePrefab(WindowPrefab, Window.transform.Find("Content"), false);
            metricWindowObject.name = "Scrollable";

            Transform scrollViewContent = metricWindowObject.transform.Find("Content/Items").transform;
            TMP_InputField inputField = metricWindowObject.transform.Find("Search/SearchField").gameObject.MustGetComponent<TMP_InputField>();

            inputField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
            inputField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);

            // Int Attributes
            DisplayAttributes(GraphElement.IntAttributes, metricWindowObject);

            // Float Attributes
            DisplayAttributes(GraphElement.FloatAttributes, metricWindowObject);

            // Save GameObjects in dictionary for SearchField
            Dictionary<string, GameObject> activeElements = new();
            foreach (Transform child in scrollViewContent)
            {
                activeElements.Add(AttributeName(child.gameObject), child.gameObject);
            }

            inputField.onValueChanged.AddListener(str => InputSearchField(str, activeElements));
        }

        /// <summary>
        /// Displays the attributes and their corresponding values.
        /// </summary>
        /// <typeparam name="T">The type of the attribute values.</typeparam>
        /// <param name="attributes">A dictionary containing attribute names (keys) and their corresponding values (values).</param>
        /// <param name="metricWindowObject">The GameObject representing the metric window.</param>
        private static void DisplayAttributes<T>(Dictionary<string, T> attributes, GameObject metricWindowObject)
        {
            Transform scrollViewContent = metricWindowObject.transform.Find("Content/Items").transform;
            foreach (KeyValuePair<string, T> kvp in attributes)
            {
                // Create GameObject
                GameObject metricRow = PrefabInstantiator.InstantiatePrefab(ItemPrefab, scrollViewContent, false);
                // Attribute Name
                Attribute(metricRow).text = kvp.Key;
                // Value Name
                Value(metricRow).text = kvp.Value.ToString();
            }
        }

        public override void RebuildLayout()
        {
            // Nothing needs to be done.
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            // TODO (#732): Should metric windows be sent over the network?
            throw new NotImplementedException();
        }

        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            // TODO (#732): Should metric windows be sent over the network?
            throw new NotImplementedException();
        }

        public override WindowValues ToValueObject()
        {
            // TODO (#732): Should metric windows be sent over the network?
            throw new NotImplementedException();
        }
    }
}
