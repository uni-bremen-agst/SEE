using DynamicPanels;
using FuzzySharp;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.UI.Window;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI
{
    public class MetricMenu : BaseWindow
    {   
        //private List<string> windows = new List<string>();
        private Dictionary<string, GameObject> windows = new Dictionary<string, GameObject>();

        //private GameObject[] Element;

        private GameObject newUIInstance;

        private int totalElements;

        public GraphElement graphElement2;

        public Node testNode;

        private GameObject menu;

        private int i = 0;

        /// <summary>
        /// Prefab for the <see cref="MetricMenu"/>.
        /// </summary>
        private string SettingsPrefab => UIPrefabFolder + "DeleteAfterTest";

        private Transform ScrollViewContent;
        private string itemPrefab => UIPrefabFolder + "MetricRowLine";

        private GameObject InputField;

        // Start is called before the first frame update
        protected override void StartDesktop()
        {
            CreateNode();
            base.StartDesktop();
            CreateUIInstance(testNode);
        }

        protected override void UpdateDesktop()
        {

        }

        private void InputSearchField(string str, GameObject[] elements)
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

        /// <summary>
        /// Performs a fuzzy search for the given <paramref name="query"/> in the graph,
        /// by comparing it to the source name of the nodes.
        /// Case will be ignored, and the query may be a substring of the source name (this is a fuzzy search).
        /// </summary>
        /// <param name="query">The query to be searched for.</param>
        /// <returns>A list of nodes which match the query.</returns>
        public IEnumerable<string> Search(string query, GameObject[] ObjectList, int limit = 10, int cutoff = 50)
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

        public void CreateUIInstance(GraphElement graphElement)
        {
            if (windows.TryGetValue(graphElement.ID, out GameObject gameObjectValue))
            {
                gameObjectValue.SetActive(true);
            }
            else
            {
                newUIInstance = PrefabInstantiator.InstantiatePrefab(SettingsPrefab, Window.transform.Find("Content"), false);

                //newUIInstance.transform.name = graphElement.ID;
                newUIInstance.name = "Scrollable";

                //Parent Content
                ScrollViewContent = newUIInstance.transform.Find("Content/Items").transform;
                Debug.Log("ScrollViewContent: " + ScrollViewContent.gameObject);

                //Input Field
                InputField = newUIInstance.transform.Find("Search/SearchField").gameObject.MustGetComponent<TMP_InputField>().gameObject;
                TMP_InputField inputField = newUIInstance.transform.Find("Search/SearchField").gameObject.MustGetComponent<TMP_InputField>();

                inputField.onSelect.AddListener(str => SEEInput.KeyboardShortcutsEnabled = false);
                inputField.onDeselect.AddListener(str => SEEInput.KeyboardShortcutsEnabled = true);

                foreach (KeyValuePair<string, int> kvp in graphElement2.IntAttributes)
                {
                    //Create GameObject
                    //GameObject newItems = Instantiate<GameObject>(itemPrefab, ScrollViewContent);
                    GameObject newItems = PrefabInstantiator.InstantiatePrefab(itemPrefab, ScrollViewContent, false);
                    //Attribute Name
                    TextMeshProUGUI attributeTextClone = newItems.transform.Find("AttributeLine").gameObject.MustGetComponent<TextMeshProUGUI>();
                    attributeTextClone.text = kvp.Key;
                    //Value Name
                    TextMeshProUGUI valueTextClone = newItems.transform.Find("ValueLine").gameObject.MustGetComponent<TextMeshProUGUI>();
                    valueTextClone.text = kvp.Value.ToString();
                }

                //Save GameObjects in Array for SearchField
                totalElements = ScrollViewContent.transform.childCount;
                GameObject[] Element = new GameObject[totalElements];

                for (int i = 0; i < totalElements; i++)
                {
                    Element[i] = ScrollViewContent.transform.GetChild(i).gameObject;
                }


                InputField.name = i.ToString();
                i++;
                inputField.onValueChanged.AddListener(str => InputSearchField(str, Element));
            }
        }


        void CreateNode()
        {
            testNode = new Node();
            testNode.SetInt("Hello.Are" , 10);
            testNode.SetInt("IchBinAre" , 5);
            testNode.SetInt("AreBoan" , 11);
            testNode.SetInt("Metric.Cool" , 3);
            testNode.SetInt("Metric.Deleted" , -1);
            testNode.SetInt("Wooow.Deleted", 1);
            testNode.SetInt("Metric.Diff.Cool", 2);
            testNode.SetInt("H", 7);
            testNode.SetInt("I", 13);
            testNode.SetInt("J", -12);
            testNode.SetInt("K", 104);
            testNode.SetInt("L", 56);
            testNode.SetInt("N", 31);
            testNode.SetInt("O", -1);
            testNode.SetInt("Ha", 7);
            testNode.SetInt("Is", 13);
            testNode.SetInt("Jd", -12);
            testNode.SetInt("Kf", 104);
            testNode.SetInt("Lb", 56);
            testNode.SetInt("Mv", 131);
            testNode.SetInt("Nm", 31);
            testNode.SetInt("Ot", -1);
            testNode.SetInt("Hsa", 7);
            testNode.SetInt("Isa", 13);
            testNode.SetInt("Jdv", -12);
            testNode.SetInt("Kfh", 104);
            testNode.SetInt("Lbg", 56);
            testNode.SetInt("Mvj", 131);
            testNode.SetInt("Nmm", 31);
            testNode.SetInt("Otö", -1);
            testNode.SetInt("Ha1", 7);
            testNode.SetInt("Is2", 13);
            testNode.SetInt("Jd3", -12);
            testNode.SetInt("Kf4", 104);
            testNode.SetInt("Lb5", 56);
            testNode.SetInt("Mv6", 131);
            testNode.SetInt("Nm7", 31);
            testNode.SetInt("Ot8", -1);
            testNode.SetInt("Hsa1", 7);
            testNode.SetInt("Isa2", 13);
            testNode.SetInt("Jdv3", -12);
            testNode.SetInt("Kfh4", 104);
            testNode.SetInt("Lbg5", 56);
            testNode.SetInt("Mvj6", 131);
            testNode.SetInt("Nmm7", 31);
            testNode.SetInt("Otö8", -1);
        }

        private void exitWindow(Button exitButton)
        {
            //Debug.Log("exitButton: " + exitButton);
            GameObject windowToClose = exitButton.transform.parent.parent.gameObject;


            windowToClose.SetActive(false);

            TMP_InputField inputField = windowToClose.transform.Find("Content/SearchField").gameObject.MustGetComponent<TMP_InputField>();
            inputField.text = "";
        }

        public override void RebuildLayout()
        {
            // Nothing needs to be done.
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            // TODO: Should tree windows be sent over the network?
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
