using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI
{
    public class MetricMenu : PlatformDependentComponent
    {

        public Node testNode;

        private GameObject menu;
        /// <summary>
        /// Prefab for the <see cref="MetricMenu"/>.
        /// </summary>
        private string SettingsPrefab => UIPrefabFolder + "MetricMenu";

        private Transform ScrollViewContent;
        private string itemPrefab => UIPrefabFolder + "MetricRowPanelREAL";

        public static MetricMenu Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("Another instance of MetricMenu already exists.");
                Destroy(gameObject);
            }
        }

        // Start is called before the first frame update
        protected override void StartDesktop()
        {
            CreateNode();
            // instantiates the Metric Menu
            menu = PrefabInstantiator.InstantiatePrefab(SettingsPrefab, Canvas.transform, false);

            //Button Exit
            menu.transform.Find("MetricPanelREAL/CloseButton").gameObject.MustGetComponent<Button>().onClick.AddListener(exitWindow);

            //Attribute Name
            /*TextMeshProUGUI attributeText = menu.transform.Find("MetricPanelREAL/ScrollView/Viewport/Content/MetricRowPanelREAL/MetricRow/AttributeLine").gameObject.MustGetComponent<TextMeshProUGUI>();
            attributeText.text = "hello world";
            //Value
            TextMeshProUGUI valueText = menu.transform.Find("MetricPanelREAL/ScrollView/Viewport/Content/MetricRowPanelREAL/MetricRow/ValueLine").gameObject.MustGetComponent<TextMeshProUGUI>();
            valueText.text = "10";*/

            //Parent for itemPrefab
            ScrollViewContent = menu.transform.Find("MetricPanelREAL/ScrollView/Viewport/Content").transform;
            Debug.Log("MetricMenu: " + ScrollViewContent);

            foreach(KeyValuePair<string, int> kvp in testNode.IntAttributes)
            {
                //Create GameObject
                //GameObject newItems = Instantiate<GameObject>(itemPrefab, ScrollViewContent);
                GameObject newItems = PrefabInstantiator.InstantiatePrefab(itemPrefab, ScrollViewContent, false);
                //Attribute Name
                TextMeshProUGUI attributeTextClone = newItems.transform.Find("MetricRow/AttributeLine").gameObject.MustGetComponent<TextMeshProUGUI>();
                attributeTextClone.text = kvp.Key;
                //Value Name
                TextMeshProUGUI valueTextClone = newItems.transform.Find("MetricRow/ValueLine").gameObject.MustGetComponent<TextMeshProUGUI>();
                valueTextClone.text = kvp.Value.ToString();
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(menu.transform.Find("MetricPanelREAL/ScrollView/Viewport/Content").gameObject.GetComponent<RectTransform>());
        }

        void UpdateTable(GraphElement graphElement)
        {
            DeleteTable();
        }

        void DeleteTable()
        {

        }



        void CreateNode()
        {
            testNode = new Node();
            testNode.SetInt("A" , 10);
            testNode.SetInt("B" , 5);
            testNode.SetInt("C" , 11);
            testNode.SetInt("D" , 3);
            testNode.SetInt("E" , -1);
            testNode.SetInt("F", 1);
            testNode.SetInt("G", 2);
            testNode.SetInt("H", 7);
            testNode.SetInt("I", 13);
            testNode.SetInt("J", -12);
            testNode.SetInt("K", 104);
            testNode.SetInt("L", 56);
            testNode.SetInt("M", 131);
            testNode.SetInt("N", 31);
            testNode.SetInt("O", -1);
        }

        // Update is called once per frame
        protected override void UpdateDesktop()
        {

        }

        public void OpenWindow()
        {
            menu.SetActive(true);
        }

        private void exitWindow()
        {
            menu.SetActive(false);
        }

    }
}
