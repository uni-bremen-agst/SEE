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
    /// It consists of a search field and a list of metrics, where each metric is represented by a row
    /// holding the attribute name and its value.
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
        /// Activates all <paramref name="metricRows"/> if they match the <paramref name="searchQuery"/>.
        /// All others are deactivated. In other words, the <paramref name="searchQuery"/> is applied as
        /// a filter.
        /// </summary>
        /// <param name="searchQuery"> attribute name to search for </param>
        /// <param name="metricRows">mapping of attribute names onto gameObjects representing
        /// the corresponding metric row</param>
        private static void ActivateMatches(string searchQuery, Dictionary<string, GameObject> metricRows)
        {
            // Remove whitespace.
            searchQuery = searchQuery.Trim();

            if (string.IsNullOrEmpty(searchQuery))
            {
                // There is no search query, so activate all metric rows.
                SetActive(metricRows, true);
            }
            else
            {
                // First, deactivate all metric rows and then activate only those that match the
                // search results.
                SetActive(metricRows, false);
                foreach (string attributeName in Search(searchQuery, metricRows.Values.ToArray()))
                {
                    if (metricRows.TryGetValue(attributeName, out GameObject activeObject))
                    {
                        activeObject.SetActive(true);
                    }
                }
            }

            return;

            static void SetActive(Dictionary<string, GameObject> searchableObjects, bool activate)
            {
                foreach (GameObject go in searchableObjects.Values)
                {
                    go.SetActive(activate);
                }
            }
        }

        /// <summary>
        /// Returns the attribute names of all <paramref name="metricRows"/> whose attribute name matches the
        /// <paramref name="query"/> using a fuzzy search.
        /// </summary>
        /// <param name="query"> the search query (part of an attribute name)</param>
        /// <param name="metricRows"> the game objects representing metric rows to search through</param>
        /// <param name="limit"> the maximum number of results to return </param>
        /// <param name="cutoff"> the cutoff score for relevance (deviation from the <paramref name="query"/>
        /// of the fuzzy search)</param>
        /// <returns> the attribute names matching the <paramref name="query"/> </returns>
        private static IEnumerable<string> Search(string query, GameObject[] metricRows, int limit = 15, int cutoff = 40)
        {
            List<string> attributesList = metricRows.Select(AttributeName).ToList();
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
            return metricRow.transform.GetChild(0).gameObject.MustGetComponent<TextMeshProUGUI>();
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
            return metricRow.transform.GetChild(1).gameObject.MustGetComponent<TextMeshProUGUI>();
        }

        /// <summary>
        /// Creates the metric window.
        /// </summary>
        public void CreateUIInstance()
        {
            // Instantiate MetricWindow
            GameObject metricWindow = PrefabInstantiator.InstantiatePrefab(WindowPrefab, Window.transform.Find("Content"), false);
            metricWindow.name = "Metric Window";

            Transform scrollViewContent = metricWindow.transform.Find("Content/Items").transform;
            TMP_InputField searchField = metricWindow.transform.Find("Search/SearchField").gameObject.MustGetComponent<TMP_InputField>();

            searchField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
            searchField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);

            // Int Attributes
            DisplayAttributes(GraphElement.IntAttributes, metricWindow);

            // Float Attributes
            DisplayAttributes(GraphElement.FloatAttributes, metricWindow);

            // Create mapping of attribute names onto gameObjects representing the corresponding metric row.
            Dictionary<string, GameObject> activeElements = new();
            foreach (Transform child in scrollViewContent)
            {
                activeElements.Add(AttributeName(child.gameObject), child.gameObject);
            }

            searchField.onValueChanged.AddListener(searchQuery => ActivateMatches(searchQuery, activeElements));
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
            foreach ((string name, T value) in attributes)
            {
                // Create GameObject
                GameObject metricRow = PrefabInstantiator.InstantiatePrefab(ItemPrefab, scrollViewContent, false);
                // Attribute Name
                Attribute(metricRow).text = name;
                // Value Name
                Value(metricRow).text = value.ToString();
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
