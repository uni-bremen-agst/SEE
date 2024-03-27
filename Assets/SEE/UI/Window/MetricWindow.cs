using FuzzySharp;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.UI.Window;
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
        /// Searches for a specified string within an array of GameObject elements and activates or deactivates them based on the search results.
        /// </summary>
        /// <param name="searchQuery"> string to search for </param>
        /// <param name="elements"> array of GameObjects to search in </param>
        private void InputSearchField(string searchQuery, GameObject[] elements)
        {
            if (searchQuery.Length == 0)
            {
                foreach (GameObject ele in elements)
                {
                    ele.SetActive(true);
                }
            }
            else
            {
                var searchList = Search(searchQuery, elements);
                foreach (GameObject ele in elements)
                {
                    if (ele != null)
                    {
                        string eleText = ele.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text;
                        ele.SetActive(searchList.Contains(eleText));
                    }
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
        public IEnumerable<string> Search(string query, GameObject[] objectList, int cutoff = 62)
        {
            List<string> attributesList = new List<string>();
            foreach (GameObject obj in objectList)
            {
                attributesList.Add(obj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text);
            }

            IEnumerable<(int score, string attribute)> searchResults = Process.ExtractAll(query, attributesList, cutoff: cutoff).Select(x => (x.Score, x.Value));
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
            GameObject[] Element = new GameObject[totalElements];

            for (int i = 0; i < totalElements; i++)
            {
                Element[i] = scrollViewContent.transform.GetChild(i).gameObject;
            }

            inputField.onValueChanged.AddListener(str => InputSearchField(str, Element));
        }

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
