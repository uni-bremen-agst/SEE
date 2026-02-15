using FuzzySharp;
using FuzzySharp.Extractor;
using Newtonsoft.Json.Linq;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.GO;
using SEE.UI.Notification;
using SEE.Utils;
using SEE.Utils.Config;
using System;
using System;
using System.Collections.Generic;
//using UnityEditor.ShaderKeywordFilter;
using System.Linq;
using System.Threading.Tasks;
using TMPro;

//using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Color = UnityEngine.Color;
using Image = UnityEngine.UI.Image;
using Task = System.Threading.Tasks.Task;
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

        private static int FuzzyScoreThreshold = 40;
        private static int maxFuzzyResult = 5;
       
        //Für das Tracking der Highlighted Blocks
        // ConfigWriter configWriter = new ConfigWriter("StudieTracker");
        public override void RebuildLayout()
        {
            //
        }
        protected override void StartDesktop()
        {
            ShowNotification.Error("Show Notification Issue StartDesktop.", "Notify", 10, true);
            // Title = "Issue Window abc";
            base.StartDesktop();
        }

        private void ApplyCustomRowLayout()
        {
          List<Transform> rows = Window.GetComponentsInChildren<Transform>(true)
      .Where(t => t.name == "AttributeLine" && t.parent.name.Equals("Foreground"))
      .ToList();

            Debug.Log($"RowCount:{rows.Count()}");
            foreach (Transform row in rows)
            {
                Transform foreground = row.Find("Foreground");
                if (foreground == null)
                    continue;
                TMP_Text attributeLine = foreground.Find("AttributeLine")?.GetComponent<TMP_Text>();
                TMP_Text valueLine = foreground.Find("ValueLine")?.GetComponent<TMP_Text>();

                if (attributeLine == null || valueLine == null)
                    continue;
          
                // "body" erkennen (case insensitive)
                if (!attributeLine.text.Trim().Equals("body", StringComparison.OrdinalIgnoreCase))
                    continue;


                valueLine.overflowMode = TextOverflowModes.Overflow;

                // Horizontal Layout entfernen
                var hLayout = foreground.GetComponent<HorizontalLayoutGroup>();
                if (hLayout != null)
                    GameObject.Destroy(hLayout);

                // Vertical Layout hinzufügen
                var vLayout = foreground.GetComponent<VerticalLayoutGroup>();
                if (vLayout == null)
                    vLayout = foreground.gameObject.AddComponent<VerticalLayoutGroup>();

                vLayout.childAlignment = TextAnchor.UpperLeft;
                vLayout.childForceExpandHeight = true;
                vLayout.childControlHeight = true;
                vLayout.spacing = 3;

                // Auto-height
                var le = row.GetComponent<LayoutElement>();
                if (le == null)
                    le = row.gameObject.AddComponent<LayoutElement>();

                le.preferredHeight = -1;
            }
        }
        private void AddHeaderButtons()
        {
            if (Window == null)
            {
                Debug.LogError("Window ist null!");
                return;
            }

            // Alle Kinder durchsuchen (inklusive inaktiver)
            var rows = Window.GetComponentsInChildren<Transform>(true)
                             .Where(t => t.name.Contains("AttributeLine") || t.name.Contains("AttributeRow"));


            foreach (Transform row in rows)
            {

                var headerLabel = row.gameObject.FindDescendant("AttributeLine").MustGetComponent<TextMeshProUGUI>();//row.Find("AttributeLine")?.GetComponent<TextMeshProUGUI>();
                if (headerLabel != null)
                {


                    string groupName = headerLabel.text;
                    if (groupName.StartsWith("Issue"))
                        AddHeaderButton(row.parent.gameObject, groupName);
                }
            }
        }

        private void AddHeaderButton(GameObject group, string groupName)
        {
            //Button-Objekt erstellen
            GameObject buttonObj = new GameObject("HeaderButton", typeof(RectTransform), typeof(Button), typeof(Image));
            buttonObj.transform.SetParent(group.transform, false);

            //Position und Größe
            var rect = buttonObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.pivot = new Vector2(1, 0.5f);
            rect.anchoredPosition = new Vector2(-25, 0);
            rect.sizeDelta = new Vector2(20, 20);

            //Optional: Farbe / Sprite
            var img = buttonObj.GetComponent<Image>();
            img.color = Color.white; // oder Sprite zuweisen

            //Click-Action
            var button = buttonObj.GetComponent<Button>();
            button.onClick.AddListener(() => OnHeaderButtonClicked(group, groupName));
        }


        //private string AttributeName(GameObject propertyRow) => Attribute(propertyRow).text;
        //private string AttributeValue(GameObject propertyRow) => Value(propertyRow).text;

        private string GetAttributeValue(String groupName, string attributeName)
        {
            if (!groupHolder.TryGetValue(groupName, out List<GameObject> groupObjects))
            {
                Debug.LogWarning($"Keine Gruppe {groupName} in groupHolder gefunden!");
                return null;
            }

            Debug.LogWarning($"Groupcount:{groupObjects.Count()}");

            foreach (var go in groupObjects)
            {
                // Überspringe die Hauptgruppe (die hat meist denselben Namen wie groupName)
                if (go.name == groupName)
                    continue;


                TextMeshProUGUI attributeText = go.FindDescendant("AttributeLine")?.GetComponent<TextMeshProUGUI>();

                // Suche in Foreground → ValueLine
                TextMeshProUGUI valueText = go.FindDescendant("ValueLine")?.GetComponent<TextMeshProUGUI>();

                if (attributeText != null && valueText != null && attributeText.text.Equals(attributeName))
                {
                    Debug.LogWarning($" {attributeText.text} = {valueText.text}");
                    return valueText.text;
                }

                //foreach (Transform child in go.transform)
                //{



                //    Debug.LogWarning($" {child.name}");
                //}
                // Suche TextMeshPro-Felder
                //var attributeText = go.FindDescendant("Attribute")?.GetComponent<TextMeshProUGUI>();
                //var valueText = go.FindDescendant("Value")?.GetComponent<TextMeshProUGUI>();

                //if (attributeText != null && valueText != null)
                //{
                //    Debug.LogWarning($"Subrow: {attributeText.text} = {valueText.text}");
                //}
                //else
                //{
                //    Debug.LogWarning($"Kein Attribute/Value in {go.name} {go.transform} gefunden");
                //}
            }
            //foreach (var row in groupObjects)
            //{

            //    Debug.LogWarning($"Name:{row.name}");
            //}
            //return "";


            //Debug.Log($"Groupcount: {groupObjects.Count}");

            //groupObjects.First().

            //foreach (var (attributeName, (value, row)) in groupDict)
            //{
            //    Debug.Log($"{attributeName}: {value}");
            //}
            //foreach (var row in groupObjects.Where(go => go.name != groupName))
            //{
            //    string currentAttrName = row.transform.Find("AttributeLine").GetComponent<TextMeshProUGUI>().text;
            //    Debug.Log($"[{groupName}] {currentAttrName} ");
            //    //if (currentAttrName.Equals(attributeName, StringComparison.OrdinalIgnoreCase))
            //    //{
            //    //    string currentValue = AttributeValue(row);
            //    //    Debug.Log($"[{groupName}] {attributeName} = {currentValue}");
            //    //    return currentValue;
            //    //}
            //}

            //Debug.LogWarning($"Attribut '{attributeName}' in Gruppe '{groupName}' nicht gefunden!");
            //Debug.LogWarning($"AttributeLine:{groupObjects.First().FindDescendant("RowLine").MustGetComponent<TextMeshProUGUI>().text}");
            ////foreach (var row in groupObjects)
            //foreach (var row in groupObjects)
            //{

            //    Debug.LogWarning($"Attribute:{Attribute(row)}");
            //    // Suche alle Text-Komponenten im Row-Objekt
            //    //var texts = row.GetComponentsInChildren<TextMeshProUGUI>(true);
            //    //foreach (var text in texts)
            //    //{
            //    //    // Falls der Name des Objekts oder der Text mit dem Attributnamen übereinstimmt
            //    //    if (text.name.Equals(attributeName, System.StringComparison.OrdinalIgnoreCase))
            //    //    {
            //    //        Debug.Log($"Gefunden: {attributeName} = {text.text}");
            //    //        return text.text;
            //    //    }
            //    //}
            //}

            //Debug.LogWarning($"Kein Attribut '{attributeName}' in Gruppe '{groupName}' gefunden!");

            //    // Suche innerhalb von Foreground nach Zeilen
            //    var attributeRowsaa = foreground.GetComponentsInChildren<Transform>(true)
            //    .Where(t => t.name.Contains("RowLine") || t.name.Contains("AttributeLine"))
            //    .ToList();

            //Debug.Log($"attributeRowsForground;  {attributeRowsaa.Count()}");
            //foreach (var t in headerRow.GetComponentsInChildren<Transform>(true))
            //{
            //    Debug.Log($"name: {t.name}");

            //    if (t.name.StartsWith("Issue"))
            //    {
            //        var attributeRowsa = t.gameObject.GetComponentsInChildren<Transform>(true)
            //                             .Where(t => t.name.Contains("RowLine") || t.name.Contains("AttributeLine")); //

            //        Debug.Log($"attributeRows; {t.name}  {attributeRowsa.Count()}");
            //    }


            //}

            //// Alle direkten oder verschachtelten Children durchsuchen

            //var attributeRows = headerRow.GetComponentsInChildren<Transform>(true)
            //                             .Where(t => t.name.Contains("RowLine") || t.name.Contains("AttributeLine")); //


            //Debug.Log($"attributeRows; {headerRow.name}  {attributeRows.Count()}");
            //foreach (var row in attributeRows)
            //{
            //    var label = row.Find("AttributeName")?.GetComponent<TextMeshProUGUI>();
            //    var value = row.Find("AttributeValue")?.GetComponent<TextMeshProUGUI>();

            //    if (label != null && value != null && label.text.Equals(attributeName, StringComparison.OrdinalIgnoreCase))
            //    {
            //        return value.text;
            //    }
            //}

            return null; // falls nicht gefunden
        }
        private async Task OnHeaderButtonClicked(GameObject headerRow, string groupName)
        {
            string bodyValue = GetAttributeValue(groupName, "body");
            Debug.Log($"BodyValue: {bodyValue}");

            if (bodyValue != null)
                await findCodeBlocksInIssue(bodyValue);
            Debug.Log($"Header button clicked: {groupName}");
            // Deine gewünschte Aktion hier (z. B. Kontextmenü, Löschen, Fokus etc.)
        }
        static int indexBlockHighligbt = 0;
        private async Task findCodeBlocksInIssue(String text)
        {
            //   string aa = "";


            GameObject[] cities = GameObject.FindGameObjectsWithTag(Tags.CodeCity);
            if (cities.Length == 0)
            {
                Debug.LogWarning("No code city found. The Issues in the Tree view  will be empty.");
            }
            //Reset der SelectedObjects
            foreach (InteractableObject interactableObject in InteractableObject.SelectedObjects) {
                interactableObject.SetSelect(false, false);
            }
         
            Debug.LogWarning("Search text in cities");
            List<String> codeBlocks = new List<String>();
            // Durchsuchen der CodeCities
            foreach (GameObject cityObject in cities)
            {
                if (cityObject.TryGetComponent(out SEECity city))
                {
                    if (city.LoadedGraph != null)
                    {
                        int index = 0;
                        foreach (Node str in city.LoadedGraph.Nodes().Where(x =>  x.Type.Equals("Class")))
                        {

                            codeBlocks.Add(str.SourceName);
                            //  break;
                            // Type 1 Clone
                            // if (text.Contains(str.SourceName))
                            // { 

                            //     //Highleigt Node
                            //// str.GameObject().Operator().Highlight(5, false);
                            //     Debug.Log($" CityBlocknames: {str.SourceName} type:{str.Type} \n {text} ");


                            // }
                            index++;
                        }

                        List<ExtractedResult<string>> results = Process.ExtractAll(text, codeBlocks, (s) => s).ToList();
                        Debug.Log($"Result >{FuzzyScoreThreshold}:{results.Where(x => x.Score > FuzzyScoreThreshold).Count()} ");
                        int indexLoop = 0;
                        List<StudyDataManager.StudyData> studyDataList = new List<StudyDataManager.StudyData>();
                        foreach (ExtractedResult<string> a in results.Where(x => x.Score > FuzzyScoreThreshold).OrderByDescending(x => x.Score)) //Descending
                        {

                            //if(a.Score > FuzzyScoreThreshold)
                            Debug.Log($"Index:{indexLoop}  CityBlocknames:  {a.Value} {a.Score} type:class \n {text}  ");
                            //     
                            InteractableObject interactable = city.LoadedGraph.Nodes().Where(x => x.Type.Equals("Class") && x.SourceName.Equals(a.Value)).First().GameObject().GetComponent<InteractableObject>();
                            if (interactable != null)
                            {
                                interactable.gameObject.Operator().Highlight(duration: 10);
                                interactable.SetSelect(true, false); // wird automatisch zu SelectedObjects hinzugefügt
                                studyDataList.Add(new StudyDataManager.StudyData() { date = DateTime.Now.ToString(), highlightedBlockName = a.Value, groupID = indexBlockHighligbt });

                            }
                            indexLoop++;
                            if (indexLoop >= maxFuzzyResult)
                                break;
                        }
                        StudyDataManager.SaveAppend(studyDataList);
                        indexBlockHighligbt++;
                        codeBlocks.Clear();
                    }
                }

            }

        }

        // Erstellen der Itemes(Issue)
        protected override async void CreateItems()
        {
            //IssueReceiver auswählen
            IssueReceiverInterface.Settings settings = new IssueReceiverInterface.Settings { preUrl = "https://api.github.com/repos/uni-bremen-agst/SEE/issues", searchUrl = "?state=all", commentAttributeName = "body" };
            //GitHubIssueReceiver gitHUbReceiver = new GitHubIssueReceiver();
            // GameObject[] cities = GameObject.FindGameObjectsWithTag(Tags.CodeCity);

           // SEECity city5 = GraphElement.par FindGameObjectsWithTag(Tags.CodeCity).First();

            //Todo aus dem Graphen die City ziehen
            SEECity city = GameObject.FindGameObjectsWithTag(Tags.CodeCity)
                     .Select(go => go.GetComponent<SEECity>())
                     .FirstOrDefault(c => c != null && c.name == "CodeCity");

            if (city == null)
            {
                Debug.LogWarning("CodeCity nicht gefunden!");
                return;
            }
            IssueReceiverInterface gitHUbReceiver = city.issueProvider.Provider;  //cities.First().GetComponent<SEECity>().issueProvider.Provider; // (())=  new GitHubIssueReceiver();

            JArray jArray;
            
          //  = await gitHUbReceiver.getIssues(settings);

            switch (gitHUbReceiver)
            {
                case GitHubIssueReceiver gitHub:

                    Debug.Log($"IssueLogURLIssueWindow: { gitHub.projekt}");

                    jArray = await gitHub.getIssues(settings);

                   // PopulateItems(gitHubIssues);
                    break;
                case GitLabIssueReceiver gitLab:

                    Debug.Log($"IssueLogURLIssueWindow: {gitLab.projekt}");

                    jArray = await gitLab.getIssues(settings);

                    // PopulateItems(gitHubIssues);
                    break;

                case JiraIssueReceiver jira:
                    jArray  = await jira.getIssues(settings);
                   //PopulateItems(jiraIssues);
                    break;

                default:
                    jArray = null;
                    Debug.LogWarning("Unbekannter IssueProvider!");
                    break;
            }

            if (jArray == null)
                return;


            Dictionary<string, string> header = new()
            {

            };
            Dictionary<string, string> Attributes = new()
            {

            };

            //Erstellen Dictionary mit Subgruppen
            Dictionary<string, object> dicIssues = new Dictionary<string, object>();


            int issueIndex = 1;
            foreach (JObject issue in jArray)
            {
                dicIssues.Add($"Issue{issueIndex}", "");


                Attributes.Clear();
                header.Add($"Issue{issueIndex}", "");

                Dictionary<string, object> dicAttributes = new() { };
                foreach (JProperty property in issue.Properties())
                {

                    if (property.Value.Type != JTokenType.Object)
                    {
                        if (property.Name.Equals(settings.commentAttributeName))
                        {
                            //Dictionary<string, object> body = new() { };
                            //Debug.Log($"Nody:{property.Value.ToString()}");
                            //// findCodeBlocksInIssue(property.Value.ToString());
                            //body.Add(property.Name, property.Value.ToString());
                            //dicAttributes.Add("->", body);
                           // dicAttributes.Add(property.Value.ToString(), "");
                            
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
                        if (dicAttributesInner.Count() > 0)
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
            AddHeaderButtons();

            ApplyCustomRowLayout();
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
                //  Dictionary<string, object> attributes = new();

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
