using SEE.Controls;
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
        //private List<string> windows = new List<string>();
        private Dictionary<string, GameObject> windows = new Dictionary<string, GameObject>();

        private GameObject canvasObject;

        //private GameObject[] Element;

        private GameObject newUIInstance;

        private int totalElements;

        public Node testNode;

        private GameObject menu;

        private int i = 0;

        private string CanvasPrefab => UIPrefabFolder + "WindowSpace";

        /// <summary>
        /// Prefab for the <see cref="MetricMenu"/>.
        /// </summary>
        private string SettingsPrefab => UIPrefabFolder + "BuildWindowToTest";

        private Transform ScrollViewContent;
        private string itemPrefab => UIPrefabFolder + "MetricRowLine";

        private GameObject InputField;

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
            
            canvasObject = PrefabInstantiator.InstantiatePrefab(CanvasPrefab, Canvas.transform, false);
            canvasObject.transform.name = "MetricSpace";

            // instantiates the Metric Menu
            menu = PrefabInstantiator.InstantiatePrefab(SettingsPrefab, canvasObject.transform, false);

            //Button Exit
            Transform outTrans = menu.transform.Find("Dragger/ExitButton");

            menu.transform.Find("Dragger/ExitButton").gameObject.MustGetComponent<Button>().onClick.AddListener(() => exitWindow(outTrans.gameObject.MustGetComponent<Button>()));

            //Parent Content
            ScrollViewContent = menu.transform.Find("Content/ScrollView/Viewport/Content").transform;

            //Input Field
            InputField = menu.transform.Find("Content/SearchField").gameObject.MustGetComponent<TMP_InputField>().gameObject;

            InputField.MustGetComponent<TMP_InputField>().onSelect.AddListener(str => SEEInput.KeyboardShortcutsEnabled = false) ;
            InputField.MustGetComponent<TMP_InputField>().onDeselect.AddListener(str => SEEInput.KeyboardShortcutsEnabled = true);

            foreach (KeyValuePair<string, int> kvp in testNode.IntAttributes)
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

            totalElements = ScrollViewContent.transform.childCount;
            GameObject[] Element = new GameObject[totalElements];

            for (int i = 0; i < totalElements; i++)
            {
                Element[i] = ScrollViewContent.transform.GetChild(i).gameObject;
            }

            InputField.MustGetComponent<TMP_InputField>().onValueChanged.AddListener(str => InputSearchField(str, Element, InputField));
        }

        protected override void UpdateDesktop()
        {

        }

        private void InputSearchField(string str, GameObject[] elements, GameObject gameObject)
        {
            Debug.Log(gameObject);
            Debug.Log("Elements.Length: " + elements.Length);
            foreach (GameObject ele in elements)
            {
                if (ele != null)
                {
                    string eleText = ele.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text;
                    if (eleText.ToLower().Contains(str.ToLower()))
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

        public void UpdateTable(GraphElement graphElement)
        {
            //Delete the table
            //DeleteTable();

            foreach (KeyValuePair<string, int> kvp in graphElement.IntAttributes)
            {
                //Create GameObject
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
        }

        /*void DeleteTable()
        {
            GameObject content = menu.transform.Find("Content/ScrollView/Viewport/Content").gameObject;
            foreach(Transform child in content.transform)
            {
                Destroy(child.gameObject);
            }
            Element = null;
        }*/

        public void CreateUIInstance(GraphElement graphElement)
        {
            if (windows.TryGetValue(graphElement.ID, out GameObject gameObjectValue))
            {
                gameObjectValue.SetActive(true);
            }
            else
            {
                newUIInstance = PrefabInstantiator.InstantiatePrefab(SettingsPrefab, canvasObject.transform, false);

                newUIInstance.transform.name = graphElement.ID;

                windows.Add(newUIInstance.transform.name, newUIInstance);

                //Button Exit
                Transform outTrans = newUIInstance.transform.Find("Dragger/ExitButton");

                Button exitButton = newUIInstance.transform.Find("Dragger/ExitButton").gameObject.MustGetComponent<Button>();
                exitButton.onClick.AddListener(() => exitWindow(exitButton));

                //Parent Content
                ScrollViewContent = newUIInstance.transform.Find("Content/ScrollView/Viewport/Content").transform;

                //Input Field
                InputField = newUIInstance.transform.Find("Content/SearchField").gameObject.MustGetComponent<TMP_InputField>().gameObject;
                TMP_InputField inputField = newUIInstance.transform.Find("Content/SearchField").gameObject.MustGetComponent<TMP_InputField>();

                inputField.onSelect.AddListener(str => SEEInput.KeyboardShortcutsEnabled = false);
                inputField.onDeselect.AddListener(str => SEEInput.KeyboardShortcutsEnabled = true);

                foreach (KeyValuePair<string, int> kvp in graphElement.IntAttributes)
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
                inputField.onValueChanged.AddListener(str => InputSearchField(str, Element, inputField.gameObject));
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

        public void OpenWindow()
        {
            menu.SetActive(true);
            newUIInstance.SetActive(true);
        }

        private void exitWindow(Button exitButton)
        {
            //Debug.Log("exitButton: " + exitButton);
            GameObject windowToClose = exitButton.transform.parent.parent.gameObject;

            Debug.Log("windowToClose: " + windowToClose);

            windowToClose.SetActive(false);

            TMP_InputField inputField = windowToClose.transform.Find("Content/SearchField").gameObject.MustGetComponent<TMP_InputField>();
            inputField.text = "";
        }
    }
}
