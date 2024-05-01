using FuzzySharp;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.UI.Window;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        /// Gameobject of the Metric Window.
        /// </summary>
        private GameObject metricWindowObject;

        /// <summary>
        /// GameObject for a row.
        /// </summary>
        private GameObject itemRow;

        /// <summary>
        /// GraphElement to read its attributes.
        /// </summary>
        public GraphElement GraphElement;

        /// <summary>
        /// Prefab for the <see cref="MetricWindow"/>.
        /// </summary>
        private string windowPrefab => UIPrefabFolder + "MetricWindow";

        /// <summary>
        /// Prefab for the <see cref="MetricRowLine"/>.
        /// </summary>
        private string itemPrefab => UIPrefabFolder + "MetricRowLine";

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
            //Remove Whitespaces
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
                IEnumerable<string> searchList = Search(searchQuery, searchableObjects.Values.ToArray());
                foreach(var ele in searchableObjects)
                {
                    ele.Value.SetActive(false);
                }
                foreach (string ele in searchList)
                {
                    searchableObjects.TryGetValue(ele, out GameObject activeObject);
                    activeObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Performs a fuzzy search operation on a collection of GameObject instances based on a provided query.
        /// </summary>
        /// <param name="query"> the search query </param>
        /// <param name="objectList"> array of GameObjects containing the objects to search through</param>
        /// <param name="cutoff"> representing the cutoff score for relevance </param>
        /// <returns> An iterable collection of strings representing the attributes of the GameObject instances, ordered by relevance to the search query </returns>
        public IEnumerable<string> Search(string query, GameObject[] objectList, int limit = 15, int cutoff = 40)
        {
            List<string> attributesList = new();
            foreach (GameObject obj in objectList)
            {
                attributesList.Add(obj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text);
            }
            IEnumerable<(int score, string attribute)> searchResults = Process.ExtractTop(query, attributesList, limit: limit, cutoff: cutoff).Select(x => (x.Score, x.Value));
            searchResults = searchResults.OrderByDescending(x => x.score);

            return searchResults.Select(x => x.attribute);
        }

        /// <summary>
        /// Creates the Metric Window
        /// </summary>
        public void CreateUIInstance()
        {
            // Instantiate MetricWindow
            metricWindowObject = PrefabInstantiator.InstantiatePrefab(windowPrefab, Window.transform.Find("Content"), false);
            metricWindowObject.name = "Scrollable";

            Transform scrollViewContent = metricWindowObject.transform.Find("Content/Items").transform;
            TMP_InputField inputField = metricWindowObject.transform.Find("Search/SearchField").gameObject.MustGetComponent<TMP_InputField>();

            inputField.onSelect.AddListener(str => SEEInput.KeyboardShortcutsEnabled = false);
            inputField.onDeselect.AddListener(str => SEEInput.KeyboardShortcutsEnabled = true);

            // Int Attributes
            DisplayAttributes(GraphElement.IntAttributes);

            // Float Attributes
            DisplayAttributes(GraphElement.FloatAttributes);

            // Save GameObjects in Array for SearchField
            int totalElements = scrollViewContent.transform.childCount;
            Dictionary<string, GameObject> activeElements = new Dictionary<string, GameObject>();

            for (int i = 1; i < totalElements; i++)
            {
                activeElements.Add(scrollViewContent.transform.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>().text,
                    scrollViewContent.transform.GetChild(i).gameObject);
            }

            inputField.onValueChanged.AddListener(str => InputSearchField(str, activeElements));
        }

        /// <summary>
        /// Displays the attributes and their corresponding values.
        /// </summary>
        /// <typeparam name="T">The type of the attribute values.</typeparam>
        /// <param name="attributes">A dictionary containing attribute names (keys) and their corresponding values (values).</param>
        private void DisplayAttributes<T>(Dictionary<string, T> attributes)
        {
            Transform scrollViewContent = metricWindowObject.transform.Find("Content/Items").transform;
            foreach (KeyValuePair<string, T> kvp in attributes)
            {
                // Create GameObject
                itemRow = PrefabInstantiator.InstantiatePrefab(itemPrefab, scrollViewContent, false);
                // Attribute Name
                TextMeshProUGUI attributeTextClone = itemRow.transform.Find("AttributeLine").gameObject.MustGetComponent<TextMeshProUGUI>();
                attributeTextClone.text = kvp.Key;
                // Value Name
                TextMeshProUGUI valueTextClone = itemRow.transform.Find("ValueLine").gameObject.MustGetComponent<TextMeshProUGUI>();
                valueTextClone.text = kvp.Value.ToString();
            }
        }

        public override void RebuildLayout()
        {
            // Nothing needs to be done.
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            // TODO: Should metric windows be sent over the network?
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
