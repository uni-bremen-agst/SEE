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

namespace SEE.UI
{
    public class MetricMenu : BaseWindow
    {   
        /// <summary>
        /// Gameobject of the Metric Window
        /// </summary>
        private GameObject MetricWindow;

        /// <summary>
        /// GameObject for a row
        /// </summary>
        private GameObject itemRow;

        /// <summary>
        /// GraphElement to read its Attributes
        /// </summary>
        public GraphElement graphElement;

        /// <summary>
        /// Prefab for the <see cref="MetricWindow"/>.
        /// </summary>
        private string SettingsPrefab => UIPrefabFolder + "MetricWindow";

        /// <summary>
        /// Prefab for the <see cref="itemRow"/>
        /// </summary>
        private string itemPrefab => UIPrefabFolder + "MetricRowLine";

        // Start is called before the first frame update
        protected override void StartDesktop()
        {
            base.StartDesktop();
            CreateUIInstance();
        }

        /// <summary>
        /// Searches for a specified string within an array of GameObject elements and activates or deactivates them based on the search results.
        /// </summary>
        /// <param name="str"> string to search for </param>
        /// <param name="elements"> array of GameObjects to search in </param>
        private void InputSearchField(string str, GameObject[] elements)
        {
            if(str.Length == 0)
            {
                foreach (GameObject ele in elements)
                {
                    ele.SetActive(true);
                }
            }
            else
            {
                var searchList = Search(str, elements);
                foreach (GameObject ele in elements)
                {
                    if (ele != null)
                    {
                        string eleText = ele.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text;

                        if (searchList.Contains(eleText))
                        {
                            ele.SetActive(true);
                        }
                        else
                        {
                            ele.SetActive(false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Performs a fuzzy search for the given <paramref name="query"/> in the graph,
        /// by comparing it to the source name of the nodes.
        /// Case will be ignored, and the query may be a substring of the source name (this is a fuzzy search).
        /// </summary>
        /// <param name="query">The query to be searched for.</param>
        /// <returns>A list of nodes which match the query.</returns>
        public IEnumerable<string> Search(string query, GameObject[] ObjectList, int limit = 10, int cutoff = 62)
        {
            string[] attributesList = new string[ObjectList.Length];

            for(int i = 0; i < ObjectList.Length; i++)
            {
                attributesList[i] = ObjectList[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text;
            }

            var listTest = Process.ExtractAll(query, attributesList, cutoff: cutoff);
            IEnumerable<(int score, string attribute)> listTest2 = listTest.Select(x => (x.Score, x.Value));

            listTest2 = listTest2.OrderByDescending(x => x.score);

            return listTest2.Select(x => x.attribute);
        }

        /// <summary>
        /// Creates the Metric Window
        /// </summary>
        /// <param name="graphElement"></param>
        public void CreateUIInstance()
        {
            //Instantiate MetricWindow
            MetricWindow = PrefabInstantiator.InstantiatePrefab(SettingsPrefab, Window.transform.Find("Content"), false);
            MetricWindow.name = "Scrollable";

            //Parent Content
            Transform ScrollViewContent = MetricWindow.transform.Find("Content/Items").transform;

            //Input Field
            TMP_InputField inputField = MetricWindow.transform.Find("Search/SearchField").gameObject.MustGetComponent<TMP_InputField>();

            inputField.onSelect.AddListener(str => SEEInput.KeyboardShortcutsEnabled = false);
            inputField.onDeselect.AddListener(str => SEEInput.KeyboardShortcutsEnabled = true);

            //TODO: Do it for every numeric Attribute
            foreach (KeyValuePair<string, int> kvp in graphElement.IntAttributes)
            {
                //Create GameObject
                itemRow = PrefabInstantiator.InstantiatePrefab(itemPrefab, ScrollViewContent, false);
                //Attribute Name
                TextMeshProUGUI attributeTextClone = itemRow.transform.Find("AttributeLine").gameObject.MustGetComponent<TextMeshProUGUI>();
                attributeTextClone.text = kvp.Key;
                //Value Name
                TextMeshProUGUI valueTextClone = itemRow.transform.Find("ValueLine").gameObject.MustGetComponent<TextMeshProUGUI>();
                valueTextClone.text = kvp.Value.ToString();
            }

            foreach (KeyValuePair<string, float> kvp in graphElement.FloatAttributes)
            {
                //Create GameObject
                itemRow = PrefabInstantiator.InstantiatePrefab(itemPrefab, ScrollViewContent, false);
                //Attribute Name
                TextMeshProUGUI attributeTextClone = itemRow.transform.Find("AttributeLine").gameObject.MustGetComponent<TextMeshProUGUI>();
                attributeTextClone.text = kvp.Key;
                //Value Name
                TextMeshProUGUI valueTextClone = itemRow.transform.Find("ValueLine").gameObject.MustGetComponent<TextMeshProUGUI>();
                valueTextClone.text = kvp.Value.ToString();
            }

            //Save GameObjects in Array for SearchField
            int totalElements = ScrollViewContent.transform.childCount;
            GameObject[] Element = new GameObject[totalElements];

            for (int i = 0; i < totalElements; i++)
            {
                Element[i] = ScrollViewContent.transform.GetChild(i).gameObject;
            }

            inputField.onValueChanged.AddListener(str => InputSearchField(str, Element));
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
