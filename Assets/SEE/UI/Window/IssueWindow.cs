
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Utils;
using SEE.GO;
using TMPro;
using UnityEngine;
using static IssueReceiverInterface;
using System.Collections.Immutable;
using System.Collections.Generic;
using System;
using SEE.UI.Notification;
using UnityEngine.UI;
using LibGit2Sharp;
using UnityEngine.UIElements;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
//using UnityEditor.ShaderKeywordFilter;
using System.Linq;
using SEE.Game.Drawable;

namespace SEE.UI.Window
{
    /// <summary>
    /// Represents a movable, scrollable window containing metrics of a <see cref="issueList"/>.

    /// </summary>
    public class IssueWindow : BaseWindow
    {
        /// <summary>
        /// GraphElement whose properties are to be shown.
        /// </summary>
        public GraphElement GraphElement;
        private const string issueWindowPrefab = "Prefabs/UI/IssueWindow";
        private readonly string GroupPrefab = UIPrefabFolder + "PropertyGroupItem";
        private List<RootIssue> issuesList;
        private JArray jArray;
        private static string WindowPrefab => UIPrefabFolder + "Window";
        private static string ItemPrefabIssue => UIPrefabFolder + "IssueWindow";
        private static string ItemPrefab => UIPrefabFolder + "IssueRowLine";
        // private static string ItemPrefab => UIPrefabFolder + "IssueRowLine";



        public override void RebuildLayout()
        {
            //
        }
        protected override void StartDesktop()
        {
            ShowNotification.Error("Show Notification Issue StartDesktop.", "Notify", 10, true);
           // Title = "Issue Window abc";
            base.StartDesktop();
            CreateUIInstance();
        }
        public async Task CreateUIInstance()
        {
            if (Window == null)
            {
                ShowNotification.Error("Window is null – cannot build issue UI.", "Error", 10, true);
                return;
            }

            Transform content = Window.transform.Find("Content");
            if (content == null)
            {
                ShowNotification.Error("Window prefab missing 'Content' child.", "Error", 10, true);
                return;
            }

            GameObject issueWindow = PrefabInstantiator.InstantiatePrefab(ItemPrefabIssue, content, false);
            issueWindow.name = "Issue Window Content";
            UnityEngine.UI.Button closeButton = issueWindow.transform.Find("Search/Close")?.GetComponent<UnityEngine.UI.Button>();
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() =>
                {
                    CloseWindow();
                });
            }

            TMP_InputField searchField = issueWindow.transform.Find("Search/SearchField").gameObject.MustGetComponent<TMP_InputField>();

            searchField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
            searchField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);
            searchField.text = "";

            //contentIT.text = "content";
            // GameObject itemPrefab = .; // Dein TMP Text UI Prefab
            //  GameObject textRowPrefab; // Drag dein TextPrefab hier rein im Inspector
            //IssueReceiverInterface.Settings settings = new IssueReceiverInterface.Settings { preUrl = "https://ecosystem.atlassian.net/rest/api/3/search?jql=", searchUrl = "project=CACHE" };
            //JiraIssueReceiver jiraReceiver = new JiraIssueReceiver();
            IssueReceiverInterface.Settings settings = new IssueReceiverInterface.Settings { preUrl = "https://api.github.com/repos/uni-bremen-agst/SEE/issues", searchUrl = "?filter=all" };
            GitHubIssueReceiver gitHUbReceiver = new GitHubIssueReceiver();
            JArray jArray=  await gitHUbReceiver.getIssues(settings);

            //issuesList = gitHUbReceiver.issuesJ;
            if(issuesList!=null)
               ShowNotification.Error($"issuesList {gitHUbReceiver.issuesJ}", "Error", 5, true);
            if (jArray.Count > 0)
            {
                //  issueList = new List<RootIssue>(); // TODO: Load real data
                ShowNotification.Error("issuesJ Is DisplayAttributes", "Status", 5, true);
                DisplayAttributes(jArray, issueWindow);
            }
            else
            {
                ShowNotification.Error("issuesJ Is NUll", "Error", 5, true);
            }
            //GameObject newRow = Instantiate(contentIT.gameObject, contentIT.transform);
            //  TextMeshProUGUI textComponent = newRow.GetComponent<TextMeshProUGUI>();
            //textComponent.text = "line asas";
            // GameObject newItem = GameObject.Instantiate(contentIT.gameObject, contentIT.transform, false);


            // Test row
            //GameObject testRow = PrefabInstantiator.InstantiatePrefab(ItemPrefab, issueWindow.transform.Find("Content/Items"), false);
            //Attribute(testRow).text = "Test Row";

        }

        private void CloseWindow()
        {
            Destroy(this.gameObject);
        }

        //public void CreateUIInstance()
        //{
        //    ShowNotification.Error("Show Notification Issue CreateUIInstance.", "Notify", 10, true);
        //    if(Window == null)
        //    {
        //        ShowNotification.Error("Show Notification Issue BaseWindow NUll.", "Notify", 10, true);
        //    }
        //    GameObject issueWindow = PrefabInstantiator.InstantiatePrefab(ItemPrefabIssue, Window.transform.Find("Content"), false);
        //    issueWindow.name = "Issue Window Show";

        //    //Transform scrollViewContent = issueWindow.transform.Find("Content/Items").transform;
        //    TMP_InputField searchField = issueWindow.transform.Find("Search/SearchField").gameObject.MustGetComponent<TMP_InputField>();

        //    searchField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
        //    searchField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);
        //    searchField.text = "testadsda";
        //    Attribute(issueWindow).text = "test";
        //    //// Int Attributes
        //    ///

        //    issueList = new List<RootIssue>();
        //    DisplayAttributes(issueList, issueWindow);

        //    //// Float Attributes
        //    //DisplayAttributes(GraphElement.FloatAttributes, metricWindow);
        //    GameObject issueRow2 = PrefabInstantiator.InstantiatePrefab(ItemPrefab, searchField.transform, false);
        //   // GameObject issueRow2 = PrefabInstantiator.InstantiatePrefab(issueWindowPrefab, searchField.transform, false);
        //    // Attribute Name
        //    Attribute(issueRow2).text = "Search feld";
        //    // Create mapping of attribute names onto gameObjects representing the corresponding metric row.
        //   // Dictionary<string, GameObject> activeElements = new();
        //    //foreach (Transform child in scrollViewContent)
        //    //{
        //    // //   activeElements.Add(AttributeName(child.gameObject), child.gameObject);
        //    //}

        // // searchField.onValueChanged.AddListener(searchQuery => ActivateMatches(searchQuery, activeElements));
        //}




        private  void DisplayAttributes(JArray issues, GameObject issueWindowObject)
        {
            Transform scrollViewContent = issueWindowObject.transform.Find("Content/Items");
            if (scrollViewContent == null)
            {
                ShowNotification.Error("scrollViewContent is null.", "Error", 10, true);
                return;
            }

            //foreach (RootIssue issue in issues)
            //{
            foreach (JObject issue in issues)
            {

                GameObject group = PrefabInstantiator.InstantiatePrefab(GroupPrefab, scrollViewContent, false);
                group.name = "Test";
                GameFinder.FindChild(group, "AttributeLine").MustGetComponent<TextMeshProUGUI>().text = "Test";
                GameObject issueRowTitle = PrefabInstantiator.InstantiatePrefab(ItemPrefab, scrollViewContent, false);
                TextMeshProUGUI parameterTextTitle = issueRowTitle.transform.Find("IssueLine")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI valueTextTitle = issueRowTitle.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
                if (parameterTextTitle != null && valueTextTitle != null)
                {
                    parameterTextTitle.text = $"Issue1";//;issue.GetValue("title")
                    valueTextTitle.text = $"Attributes";  //; {issue["title"]}
                }

                foreach (JProperty property in issue.Properties())
                {
                    Debug.LogWarning($"{property.HasValues} -> ");
                    List<GameObject> list= null;
                    //if (property.HasValues)
                    //{
                    //    continue;
                    //    list = new List<GameObject>();
                    //    foreach (JProperty value in property.Values())
                    //    {
                    //        GameObject issueRow = PrefabInstantiator.InstantiatePrefab(ItemPrefab, scrollViewContent, false);
                    //        TextMeshProUGUI parameterText = issueRow.transform.Find("IssueLine")?.GetComponent<TextMeshProUGUI>();
                    //        TextMeshProUGUI valueText = issueRow.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
                    //        if (parameterText != null && valueText != null)
                    //        {
                    //            parameterText.text = $"{value.Name}";//;
                    //            valueText.text = $"{value.Value}";  //; {issue["title"]}
                    //        }
                    //        list.Add(issueRow);
                    //    }


                    //}
                    //else
                    //{
                    //
                    //Transform scrollViewContenta = issueRowTitle.transform.Find("Content/Items");
                    //if (scrollViewContenta == null)
                    //{
                    //    ShowNotification.Error("scrollViewContenta is null.", "Error", 10, true);
                    //    return;
                    //}
                    //GameObject group = PrefabInstantiator.InstantiatePrefab(GroupPrefab, items, false);
                    //group.name = name;
                    //GameFinder.FindChild(group, "AttributeLine").MustGetComponent<TextMeshProUGUI>().text = name;

                    GameObject issueRow = PrefabInstantiator.InstantiatePrefab(ItemPrefab, scrollViewContent, false);
                        TextMeshProUGUI parameterText = issueRow.transform.Find("IssueLine")?.GetComponent<TextMeshProUGUI>();
                        TextMeshProUGUI valueText = issueRow.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
                        if (parameterText != null && valueText != null)
                        {
                            parameterText.text = $"{property.Name}";//;
                            valueText.text = $"{property.Value}";  //; {issue["title"]}
                        }
                    //}
                }
             // Zeigt das erste Issue in der GUI
                return;
            }
         
        }
        private static TextMeshProUGUI Attribute(GameObject issueRow)
        {
            return issueRow.transform.GetChild(0).gameObject.MustGetComponent<TextMeshProUGUI>();
        }
        public override WindowValues ToValueObject()
        {
            throw new NotImplementedException();
        }

        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            throw new NotImplementedException();
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            throw new NotImplementedException();
        }
   
    }
}
