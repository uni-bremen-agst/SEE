
using LibGit2Sharp;
using Mediapipe;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.Drawable;
using SEE.GO;
using SEE.UI.Notification;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
//using UnityEditor.ShaderKeywordFilter;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static IssueReceiverInterface;

namespace SEE.UI.Window.PropertyWindow
{
    /// <summary>
    /// Represents a movable, scrollable window containing metrics of a <see cref="issueList"/>.

    /// </summary>
    public class IssueWindow : PropertyWindow
    {
        /// <summary>
        /// GraphElement whose properties are to be shown.
        /// </summary>
        public GraphElement GraphElement;
        //private const string issueWindowPrefab = "Prefabs/UI/IssueWindow";
        //private readonly string GroupPrefab = UIPrefabFolder + "PropertyGroupItem";
        //private List<RootIssue> issuesList;
        //private JArray jArray;
        //private static string WindowPrefab => UIPrefabFolder + "Window";
        //private static string ItemPrefabIssue => UIPrefabFolder + "IssueWindow";
        //private static string ItemPrefab => UIPrefabFolder + "IssueRowLine";
        //// private static string ItemPrefab => UIPrefabFolder + "IssueRowLine";



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

        void findCodeBlocksInIssue(String text)
        {
            GameObject[] cities = GameObject.FindGameObjectsWithTag(Tags.CodeCity);
            if (cities.Length == 0)
            {
                Debug.LogWarning("No code city found. Tree view will be empty.");

            }

            //   TreeWindow treeWindow = goa.AddComponent<TreeWindow>();
            // We will create a tree view for each code city.
            foreach (GameObject cityObject in cities)
            {
                if (cityObject.TryGetComponent(out AbstractSEECity city))
                {
                    if (city.LoadedGraph != null) //|| treeWindows.ContainsKey(city.name)
                    {
                     //   city.
                        int index = 0;
                     foreach(Node str in  city.LoadedGraph.Nodes().Where(x=> x.Type.Equals("Class")))
                        {
                            // gameObject.Operator()
                            if(text.Contains(str.SourceName))
                            { 
                           // str.GameObject().Operator().Highlight(5, false);
                                Debug.Log($" CityBlocknames: {str.SourceName} type:{str.Type} \n {text} ");
                         
                            //if (index >= 3)
                            //    break;
                            }
                            index++;
                        }
                   

                    }
                } 
            
            }
        }
        protected override async void   CreateItems()
        {
            //if (contextMenu.Filter.IncludeHeader)
            //{

          //test();
            IssueReceiverInterface.Settings settings = new IssueReceiverInterface.Settings { preUrl = "https://api.github.com/repos/uni-bremen-agst/SEE/issues", searchUrl = "?state=all",commentAttributeName="body" };
            GitHubIssueReceiver gitHUbReceiver = new GitHubIssueReceiver();
            JArray jArray = await gitHUbReceiver.getIssues(settings);
            //while(gitHUbReceiver.issuesJ ==null)
            //{

            //}

            //JArray jArrßay = gitHUbReceiver.issuesJ;
            ////if (issuesList != null)
            ////    ShowNotification.Error($"issuesList {gitHUbReceiver.issuesJ}", "Error", 5, true);
            //if (jArray.Count > 0)
            //{
            //    //  issueList = new List<RootIssue>(); // TODO: Load real data
            //    ShowNotification.Error("issuesJ Is DisplayAttributes", "Status", 5, true);
            //    // DisplayAttributes(jArray, issueWindow);
            //}
            //else
            //{
            //    ShowNotification.Error("issuesJ Is NUll", "Error", 5, true);
            //}


            Dictionary<string, string> header = new()
            {

            };
            Dictionary<string, string> Attributes = new()
         {
     
         };

            //Erstellen Dictionary mit Subgruppen
            Dictionary<string, object> dicIssues= new Dictionary<string, object>();


            int issueIndex = 1;
            foreach (JObject issue in jArray)
            {
                dicIssues.Add($"Issue{issueIndex}", "");


                Attributes.Clear();
                header.Add($"Issue{issueIndex}", "");
                //    // GameObject group = PrefabInstantiator.InstantiatePrefab(GroupPrefab, scrollViewContent, false);
                //    // group.name = "Test";
                //    // GameFinder.FindChild(group, "AttributeLine").MustGetComponent<TextMeshProUGUI>().text = "Test";
                //    // GameObject issueRowTitle = PrefabInstantiator.InstantiatePrefab(ItemPrefab, scrollViewContent, false);
                //    // TextMeshProUGUI parameterTextTitle = issueRowTitle.transform.Find("IssueLine")?.GetComponent<TextMeshProUGUI>();
                //    //// TextMeshProUGUI valueTextTitle = issueRowTitle.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
                //    // if (parameterTextTitle != null && valueTextTitle != null)
                //    // {
                //    //     parameterTextTitle.text = $"Issue1";//;issue.GetValue("title")
                //    //     valueTextTitle.text = $"Attributes";  //; {issue["title"]}
                //    // }
                Dictionary<string, object> dicAttributes = new(){};
                foreach (JProperty property in issue.Properties())
                {

                    if(property.Value.Type != JTokenType.Object)
                    {
                        if(property.Name.Equals(settings.commentAttributeName))
                        {
                           // Debug.Log($"Nody:{property.Value.ToString()}");
                            findCodeBlocksInIssue(property.Value.ToString());
                        }
                        dicAttributes.Add(property.Name, property.Value.ToString());
                    }
                    else
                    {
                        Dictionary<string, object> dicAttributesInner = new() { };
                        JToken token = JToken.Parse(property.Value.ToString());

                        // If it's an object, iterate properties
                        if (token is JObject obj)
                        {
                            foreach (JProperty propertyIn in obj.Properties())
                            {
                                
                                string name = propertyIn.Name;         // Attribute name (key)
                                JToken valueToken = propertyIn.Value;  // Raw JToken value
                                object value = valueToken.ToObject<object>(); // Convert to .NET type
                                if (value != null)
                                    dicAttributesInner.Add(name, value);
                                else
                                    dicAttributesInner.Add(name, "");
                                //Console.WriteLine($"{name}: {value}");
                            }
                        }
                        //JsonConvert.DeserializeObject<Dictionary<string, object>>($"{"{"}{property.Name}:{property.Value.ToString()}{"}"}"))
                        if(dicAttributesInner.Count()>0)
                            dicAttributes.Add($"{property.Name}{issueIndex}", dicAttributesInner);
                      //  Attributes.Add(property.Name, "Values");


                    }
                
                }
                dicIssues[$"Issue{issueIndex}"] = dicAttributes;
                //  DisplayGroup("Attribure", Attributes, 2, $"Issue{issueIndex}");
                issueIndex++;
                //DisplayGroup("Submenu", Attributes, 1, "Issues");

                // break;
            }
            // Data Attributes
           // Dictionary<string, (string, GameObject gameObject)> headerItems = DisplayAttributes(header);
            //Dictionary<string, object> subsubsds = new()
            //             {
            //              { "abc", "23423s5" },
            //                  { "abc2", "42553423" }
            //             };
            //Dictionary<string, object> subsub = new()
            //             {
            //              { "Attri1", "23423" },
            //               { "Attri2", "23423" },

            //                  { "Attri3", subsubsds },
            //     { "Attri4", "23423" },
            //      { "Attri5", "23423" }
            //             };


            SplitInAttributeGroup(dicIssues);
            // CreateNestedGroups(subsub, "Issue1");
            //  DisplayGroup("subAttri", subsub, 2, "Issue1");

            // 
            // DisplayGroup("SubmenuSub", subsubGroup, 2, "Submenu");

            //groupHolder.Add("Header", headerItems.Values.Select(x => x.gameObject).ToList());
            //// groupHolder.Add("Header5", headerItems.Values.Select(x => x.gameObject).ToList());
            //expandedItems.Add("Header");

            /// There are two ways to group the attributes: by value type or by name type.
            /// The first one creates groups like <see cref="PropertyTypes.ToggleAttributes"/>,
            /// <see cref="PropertyTypes.StringAttributes"/>, etc. according to the kind of
            /// graph element attribute kind.
            /// The second one creates groups according to the qualified name of the graph element attribute,
            /// for example "Source", "Metric", etc. The name is split at the first dot.
            //if (contextMenu.GroupByName)
            //{
            //    GroupByName();
            //}
            //else
            //{
            //    GroupByType();
            //}

            //// Sorts the properties
            //Sort();

            //// Applies the search
            //  ApplySearch();

            return;

            // Creates the items for the value type group, when attributes should be
            // grouped by their value type (i.e., boolean, string, int, float attributes).
            void GroupByType()
            {
                // Toggle Attributes
                /* Currently, there are no boolean attributes for authors.

                if (contextMenu.Filter.IncludeToggleAttributes)
                {
                    DisplayGroup(PropertyTypes.ToggleAttributes,
                                 new Dictionary<string, string>{ { "Is Admin", "yes" } });
                }
                */

                // String Attributes
                //if (contextMenu.Filter.IncludeStringAttributes)
                //{
                //    DisplayGroup(PropertyTypes.StringAttributes,
                //                 new Dictionary<string, string> { { EmailAttribute, author.Author.Email } });
                //}

                //// Int Attributes
                //if (contextMenu.Filter.IncludeIntAttributes)
                //{
                //    DisplayGroup(PropertyTypes.IntAttributes,
                //                 new Dictionary<string, string> { { NumberOfFiles, author.NumberOfFiles().ToString() } });
                //}

                // Float Attributes
                /* Currently, there are no float attributes for authors.
                if (contextMenu.Filter.IncludeFloatAttributes)
                {
                    DisplayGroup(PropertyTypes.FloatAttributes,
                                 new Dictionary<string, string> { { "Hours", "10.0" } });
                }
                */
            }

            // Creates the items for the name type group, when attributes should be
            // grouped by their name type.
            void GroupByName()
            {
                Dictionary<string, object> attributes = new();

                /* Currently, there are no boolean attributes for authors.
                if (contextMenu.Filter.IncludeToggleAttributes)
                {
                    attributes.Add("Is Admin", "yes");
                }
                */
                //if (contextMenu.Filter.IncludeStringAttributes)
                //{
                //    attributes.Add(EmailAttribute, author.Author.Email);
                //}
                //if (contextMenu.Filter.IncludeIntAttributes)
                //{
                //    attributes.Add(NumberOfFiles, author.NumberOfFiles());
                //}
                /* Currently, there are no float attributes for authors.
                if (contextMenu.Filter.IncludeFloatAttributes)
                {
                    attributes.Add("Hours", "10.0");
                }
                */
                // SplitInAttributeGroup(attributes);
                // }
            }
        }
        //public async Task CreateUIInstance()
        //{
        //    if (Window == null)
        //    {
        //        ShowNotification.Error("Window is null – cannot build issue UI.", "Error", 10, true);
        //        return;
        //    }

        //    Transform content = Window.transform.Find("Content");
        //    if (content == null)
        //    {
        //        ShowNotification.Error("Window prefab missing 'Content' child.", "Error", 10, true);
        //        return;
        //    }

        //    GameObject issueWindow = PrefabInstantiator.InstantiatePrefab(ItemPrefabIssue, content, false);
        //    issueWindow.name = "Issue Window Content";
        //    UnityEngine.UI.Button closeButton = issueWindow.transform.Find("Search/Close")?.GetComponent<UnityEngine.UI.Button>();
        //    if (closeButton != null)
        //    {
        //        closeButton.onClick.AddListener(() =>
        //        {
        //            CloseWindow();
        //        });
        //    }

        //    TMP_InputField searchField = issueWindow.transform.Find("Search/SearchField").gameObject.MustGetComponent<TMP_InputField>();

        //    searchField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
        //    searchField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);
        //    searchField.text = "";

        //    //contentIT.text = "content";
        //    // GameObject itemPrefab = .; // Dein TMP Text UI Prefab
        //    //  GameObject textRowPrefab; // Drag dein TextPrefab hier rein im Inspector
        //    //IssueReceiverInterface.Settings settings = new IssueReceiverInterface.Settings { preUrl = "https://ecosystem.atlassian.net/rest/api/3/search?jql=", searchUrl = "project=CACHE" };
        //    //JiraIssueReceiver jiraReceiver = new JiraIssueReceiver();
        //    IssueReceiverInterface.Settings settings = new IssueReceiverInterface.Settings { preUrl = "https://api.github.com/repos/uni-bremen-agst/SEE/issues", searchUrl = "?filter=all" };
        //    GitHubIssueReceiver gitHUbReceiver = new GitHubIssueReceiver();
        //    JArray jArray=  await gitHUbReceiver.getIssues(settings);

        //    //issuesList = gitHUbReceiver.issuesJ;
        //    if(issuesList!=null)
        //       ShowNotification.Error($"issuesList {gitHUbReceiver.issuesJ}", "Error", 5, true);
        //    if (jArray.Count > 0)
        //    {
        //        //  issueList = new List<RootIssue>(); // TODO: Load real data
        //        ShowNotification.Error("issuesJ Is DisplayAttributes", "Status", 5, true);
        //        DisplayAttributes(jArray, issueWindow);
        //    }
        //    else
        //    {
        //        ShowNotification.Error("issuesJ Is NUll", "Error", 5, true);
        //    }
        //    //GameObject newRow = Instantiate(contentIT.gameObject, contentIT.transform);
        //    //  TextMeshProUGUI textComponent = newRow.GetComponent<TextMeshProUGUI>();
        //    //textComponent.text = "line asas";
        //    // GameObject newItem = GameObject.Instantiate(contentIT.gameObject, contentIT.transform, false);


        //    // Test row
        //    //GameObject testRow = PrefabInstantiator.InstantiatePrefab(ItemPrefab, issueWindow.transform.Find("Content/Items"), false);
        //    //Attribute(testRow).text = "Test Row";

        //}

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




        //private  void DisplayAttributes(JArray issues, GameObject issueWindowObject)
        //{
        //    Transform scrollViewContent = issueWindowObject.transform.Find("Content/Items");
        //    if (scrollViewContent == null)
        //    {
        //        ShowNotification.Error("scrollViewContent is null.", "Error", 10, true);
        //        return;
        //    }

        //    //foreach (RootIssue issue in issues)
        //    //{
        //    foreach (JObject issue in issues)
        //    {

        //        GameObject group = PrefabInstantiator.InstantiatePrefab(GroupPrefab, scrollViewContent, false);
        //        group.name = "Test";
        //        GameFinder.FindChild(group, "AttributeLine").MustGetComponent<TextMeshProUGUI>().text = "Test";
        //        GameObject issueRowTitle = PrefabInstantiator.InstantiatePrefab(ItemPrefab, scrollViewContent, false);
        //        TextMeshProUGUI parameterTextTitle = issueRowTitle.transform.Find("IssueLine")?.GetComponent<TextMeshProUGUI>();
        //        TextMeshProUGUI valueTextTitle = issueRowTitle.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
        //        if (parameterTextTitle != null && valueTextTitle != null)
        //        {
        //            parameterTextTitle.text = $"Issue1";//;issue.GetValue("title")
        //            valueTextTitle.text = $"Attributes";  //; {issue["title"]}
        //        }

        //        foreach (JProperty property in issue.Properties())
        //        {
        //            Debug.LogWarning($"{property.HasValues} -> ");
        //            List<GameObject> list= null;
        //            //if (property.HasValues)
        //            //{
        //            //    continue;
        //            //    list = new List<GameObject>();
        //            //    foreach (JProperty value in property.Values())
        //            //    {
        //            //        GameObject issueRow = PrefabInstantiator.InstantiatePrefab(ItemPrefab, scrollViewContent, false);
        //            //        TextMeshProUGUI parameterText = issueRow.transform.Find("IssueLine")?.GetComponent<TextMeshProUGUI>();
        //            //        TextMeshProUGUI valueText = issueRow.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
        //            //        if (parameterText != null && valueText != null)
        //            //        {
        //            //            parameterText.text = $"{value.Name}";//;
        //            //            valueText.text = $"{value.Value}";  //; {issue["title"]}
        //            //        }
        //            //        list.Add(issueRow);
        //            //    }


        //            //}
        //            //else
        //            //{
        //            //
        //            //Transform scrollViewContenta = issueRowTitle.transform.Find("Content/Items");
        //            //if (scrollViewContenta == null)
        //            //{
        //            //    ShowNotification.Error("scrollViewContenta is null.", "Error", 10, true);
        //            //    return;
        //            //}
        //            //GameObject group = PrefabInstantiator.InstantiatePrefab(GroupPrefab, items, false);
        //            //group.name = name;
        //            //GameFinder.FindChild(group, "AttributeLine").MustGetComponent<TextMeshProUGUI>().text = name;

        //            GameObject issueRow = PrefabInstantiator.InstantiatePrefab(ItemPrefab, scrollViewContent, false);
        //                TextMeshProUGUI parameterText = issueRow.transform.Find("IssueLine")?.GetComponent<TextMeshProUGUI>();
        //                TextMeshProUGUI valueText = issueRow.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
        //                if (parameterText != null && valueText != null)
        //                {
        //                    parameterText.text = $"{property.Name}";//;
        //                    valueText.text = $"{property.Value}";  //; {issue["title"]}
        //                }
        //            //}
        //        }
        //     // Zeigt das erste Issue in der GUI
        //        return;
        //    }
         
        //}
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
