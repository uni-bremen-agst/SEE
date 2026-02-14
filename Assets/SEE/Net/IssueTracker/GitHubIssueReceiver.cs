using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc.Server;
using SEE.Game;
using SEE.Game.City;
using SEE.GraphProviders;
using SEE.UI.Notification;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static IssueReceiverInterface;
[Serializable]
public class GitHubIssueReceiver : BasicIssueProvider  
{
    public GitHubIssueReceiver(SEECity city) : base(city)
    {
    }
    //public class GitHubIssueReceiver {
    //}
    //Type of the possible
    [SerializeField]
    public override IssueReceiverInterface.IssueProvider Type => IssueReceiverInterface.IssueProvider.GitHubIssueReceiver;


    //public List<RootIssue> issues;
    //public string token = "";
    //public string owner = "";
    //Github / giblab Issue Classes
    #region "IssueClasses Github/Gitlab" 
    public class Issue
    {
        public string url { get; set; }
        public string repository_url { get; set; }
        public string labels_url { get; set; }
        public string comments_url { get; set; }
        public string events_url { get; set; }
        public string html_url { get; set; }
        public long id { get; set; }
        public string node_id { get; set; }
        public int number { get; set; }
        public string title { get; set; }
        public User user { get; set; }
        public Label[] labels { get; set; }
        public string state { get; set; }
        public bool locked { get; set; }
        public Assignee assignee { get; set; }
        public Assignee1[] assignees { get; set; }
        public object milestone { get; set; }
        public int comments { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public DateTime? closed_at { get; set; }
        public string author_association { get; set; }
        public object active_lock_reason { get; set; }
        public string body { get; set; }
        public Closed_By closed_by { get; set; }
        public Reactions reactions { get; set; }
        public string timeline_url { get; set; }
        public object performed_via_github_app { get; set; }
        public string state_reason { get; set; }
        public bool draft { get; set; }
        public Pull_Request pull_request { get; set; }
    }

    public class User
    {
        public string login { get; set; }
        public int id { get; set; }
        public string node_id { get; set; }
        public string avatar_url { get; set; }
        public string gravatar_id { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string followers_url { get; set; }
        public string following_url { get; set; }
        public string gists_url { get; set; }
        public string starred_url { get; set; }
        public string subscriptions_url { get; set; }
        public string organizations_url { get; set; }
        public string repos_url { get; set; }
        public string events_url { get; set; }
        public string received_events_url { get; set; }
        public string type { get; set; }
        public string user_view_type { get; set; }
        public bool site_admin { get; set; }
    }

    public class Assignee
    {
        public string login { get; set; }
        public int id { get; set; }
        public string node_id { get; set; }
        public string avatar_url { get; set; }
        public string gravatar_id { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string followers_url { get; set; }
        public string following_url { get; set; }
        public string gists_url { get; set; }
        public string starred_url { get; set; }
        public string subscriptions_url { get; set; }
        public string organizations_url { get; set; }
        public string repos_url { get; set; }
        public string events_url { get; set; }
        public string received_events_url { get; set; }
        public string type { get; set; }
        public string user_view_type { get; set; }
        public bool site_admin { get; set; }
    }

    public class Closed_By
    {
        public string login { get; set; }
        public int id { get; set; }
        public string node_id { get; set; }
        public string avatar_url { get; set; }
        public string gravatar_id { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string followers_url { get; set; }
        public string following_url { get; set; }
        public string gists_url { get; set; }
        public string starred_url { get; set; }
        public string subscriptions_url { get; set; }
        public string organizations_url { get; set; }
        public string repos_url { get; set; }
        public string events_url { get; set; }
        public string received_events_url { get; set; }
        public string type { get; set; }
        public string user_view_type { get; set; }
        public bool site_admin { get; set; }
    }

    public class Reactions
    {
        public string url { get; set; }
        public int total_count { get; set; }
        public int plus1 { get; set; }
        public int minus1 { get; set; }
        public int laugh { get; set; }
        public int hooray { get; set; }
        public int confused { get; set; }
        public int heart { get; set; }
        public int rocket { get; set; }
        public int eyes { get; set; }
    }

    public class Pull_Request
    {
        public string url { get; set; }
        public string html_url { get; set; }
        public string diff_url { get; set; }
        public string patch_url { get; set; }
        public DateTime? merged_at { get; set; }
    }

    public class Label
    {
        public long id { get; set; }
        public string node_id { get; set; }
        public string url { get; set; }
        public string name { get; set; }
        public string color { get; set; }
        public bool _default { get; set; }
        public string description { get; set; }
    }

    public class Assignee1
    {
        public string login { get; set; }
        public int id { get; set; }
        public string node_id { get; set; }
        public string avatar_url { get; set; }
        public string gravatar_id { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string followers_url { get; set; }
        public string following_url { get; set; }
        public string gists_url { get; set; }
        public string starred_url { get; set; }
        public string subscriptions_url { get; set; }
        public string organizations_url { get; set; }
        public string repos_url { get; set; }
        public string events_url { get; set; }
        public string received_events_url { get; set; }
        public string type { get; set; }
        public string user_view_type { get; set; }
        public bool site_admin { get; set; }
    }
    #endregion
    //[HideReferenceObjectPicker,
    // ListDrawerSettings(DefaultExpandedState = true, ListElementLabelName = nameof(IssueProvider))]
    //[ShowInInspector]

    public string owner = "";//"";
   // [OdinSerialize, ShowInInspector]
    //public string repo = "";//"IssueTrackerRepository";
   // public string token = "";//"TestToken";
    static private string label = "Data";

    public override Dictionary<string, string> getCreateIssueAttributes()
    {
        return new Dictionary<string, string> {{ "Title", "" },
                                                { "Description", "" },
                                                   { "Assignee", "" },
                                                { "Labels", "Report" }
            };
    }
    
    #region Config I/O

    public override void SaveInternal(ConfigWriter writer,String label)
    {
        SaveAttributes(writer);
        Debug.Log($"SaveConfig Owner: {label}");

    }

    protected internal static GitHubIssueReceiver RestoreProvider(Dictionary<string, object> values)
    {
        IssueReceiverInterface.IssueProvider IssueProvider = IssueReceiverInterface.IssueProvider.GitHubIssueReceiver;

        Debug.Log($"LoadeConfig Owner: {label}");

        GitHubIssueReceiver gitHubIssueReceiver = new GitHubIssueReceiver(new SEECity()); ;// new GitHubIssueReceiver();
       // gitHubIssueReceiver.Type = "GitHubIssueReceiver";
        if (ConfigIO.RestoreEnum(values, "Type", ref IssueProvider))
        {
           // Debug.Log($"IssueRestoreProvider: {IssueProvider}" );
          //  Debug.Log($"IssueRestore Owner: {values["Owner"]}");
         
            gitHubIssueReceiver.RestoreAttributes(values);
            return gitHubIssueReceiver;
        }
        else
        {
            throw new Exception($"Specification of graph provider is malformed: label {IssueProvider} is missing.");
        }
    }

    public static GitHubIssueReceiver Restore(Dictionary<string, object> attributes, string label)
    {
        if (attributes.TryGetValue(label, out object dictionary))
        {
            Dictionary<string, object> values = dictionary as Dictionary<string, object>;

            //gitHubIssueReceiver.Type = "GitHubIssueReceiver";
            return RestoreProvider(values);
        }
        else
        {
            throw new Exception($"A GitHubIssue Provider could not be found under the label {label}.");
        }
    }

    public void SaveAttributes(ConfigWriter writer)
    {
        writer.BeginGroup("dataIssueReceiver");
       writer.Save(this.GetType().ToString(), "Type");
        writer.Save(owner, "Owner");
        writer.Save(projekt, "Repo");
        writer.Save(token, "Token");
        //SaveAttributes(writer);
        writer.EndGroup();
        //this.se
        //writer.BeginList(pipelineLabel);
        //foreach (CallConvThiscall.)
        //{
        //    provider.Save(writer, "");
        //}
        //writer.EndList();
    }

    public override void RestoreAttributes(Dictionary<string, object> attributes)
    {
        Debug.Log($"RestoreAttributes Config : {attributes}");

        foreach (KeyValuePair<string, object> keyValuePair in attributes)
        {
            Debug.Log($"{keyValuePair.Key}:{keyValuePair.Value}");
        }
        Dictionary<string, object> dataIssueReceiver = (Dictionary<string, object>)attributes["dataIssueReceiver"];
        owner = (string)dataIssueReceiver["Owner"];
        projekt = (string)dataIssueReceiver["Repo"];
        token = (string)dataIssueReceiver["Token"];

        Settings settings = new IssueReceiverInterface.Settings
        {
          //  ,
            searchUrl = "?state=open"
        };
       this.settings = settings;
    }

    #endregion
    public override async Task<bool> createIssue(Dictionary<string, string> attributes)//string token, string owner
    {
        Debug.Log($"Start multiselect Createissue");
        string[] labelArray = attributes["Labels"]?.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        // Need chage to other Typ, but its in each IssueTracker  differend so need a own class for every IssueData for Create Issue 
        var issueData = new
        {
            title = attributes["Title"],
            body = attributes["Description"],
            assignees = new[] { "CodeSEEBenutzer" },//self //"lkuenzel" attributes["Assignee"]
            labels = labelArray, //new[] {$"{attributes["Labels"]}" } ////   
        };
        
        using (HttpClient client = new HttpClient())
        {
            // GitHub API base URL
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("UnityApp/1.0");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", token);

            string json = JsonConvert.SerializeObject(issueData);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                Debug.Log($"repos/{owner}/{projekt}/issues");

                HttpResponseMessage response = await client.PostAsync($"repos/{owner}/{projekt}/issues", content);
                //      Debug.Log("Response:\n" + response);
                Console.WriteLine("Response:\n" + response);
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Issue created successfully:\n" + responseBody);
                    ShowNotification.Error("Issue was created successfully", "Info", 10);
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error:{response.StatusCode}\n{error}"); 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error:{ex}");
            }

        }
        return false;
    }
    public async Task<bool> updateIssue()
    {
        return false;
    }


    private async Task restAPI(Settings settings)
    {
        //& Notification.print("restAPI call");

        // this.issuesJ = null;

        //if (settings != null)
        //    this.settings = settings;
        //if (this.settings == null)
        //{
        //   // Notification.print({ "Issueprovider Settings is  NULL"});
        //    return;
        //}
        this.settings = new IssueReceiverInterface.Settings
        {
            preUrl = $"https://api.github.com/repos/{owner}/{projekt}/issues",
            searchUrl = "?state=open"
        };
        //this.settings = settings;

        //this.settings.preUrl = $"https://api.github.com/repos/{owner}/{repo}/issues";
        //this.settings.searchUrl = "?state=open";

        int maxPage = 1;
        int currentPage = 1;
        string pageingStr = "";
        while (maxPage >= currentPage)
        {
            // 
            // string requestUrl = "https://api.github.com/repos/uni-bremen-agst/SEE/issues?state=closed&per_page=100&Page=0;rel=last";// "https://api.github.com/repos/koschke/uni-bremen-agst/SEE/issues"; //"";
            pageingStr = $"&per_page=100&page={currentPage.ToString()}";
            Debug.Log($"IssueLogURL: {this.settings.preUrl + this.settings.searchUrl}");
              UnityWebRequest request = UnityWebRequest.Get($"{this.settings.preUrl}{this.settings.searchUrl}{pageingStr}"); //;rel=last
           // UnityWebRequest request = UnityWebRequest.Get($"https://api.github.com/repos/uni-bremen-agst/SEE/issues?state=all{pageingStr}"); //;rel=last &since=2024-01-01T00:00:00Z

            request.SetRequestHeader("User-Agent", "UnityApp");
            if (token != null && token != "TestToken" && token != "")
                request.SetRequestHeader("Authorization", $"token {token}");
            request.SetRequestHeader("Accept", "application/json");
            //  request.SetRequestHeader("Authorization", $"AxToken {pke}");
#pragma warning disable CS4014
            request.SendWebRequest();
            #pragma warning restore CS4014
            await UniTask.WaitUntil(() => request.isDone);

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("GetIssuesSuccess");
                Debug.Log($"x-ratelimit-remaining\r\n{request.GetResponseHeader("x-ratelimit-remaining")}");
                //Rückgabe
               
                string linkHeader = request.GetResponseHeader("Link");
                if (!string.IsNullOrEmpty(linkHeader) && linkHeader.Contains(">; rel=\"next\""))
                {
                    maxPage += 1;
                    Debug.Log($"setPage:{maxPage}");
                    Debug.Log(linkHeader);
                  //// Debug.Log(linkHeader);
                  //  // Suche nach rel="last"
                  //  Match match = Regex.Match(linkHeader, @"[&?]page=(\d+)>; rel=""next"""); //[&?] rel=last&
                  //  if (match.Success)
                  //  {
                 
                  //      Debug.Log(match.Groups[1].Value);
                  //      //match.NextMatch();
                  //      //int.Parse(match.Groups[1].Value);
                     
                  //  }
                }
               // Debug.Log($"Result not sucessfull {request.re}");
            }
            else
            {
                Debug.Log($"Result not sucessfull {request.result}");
                Debug.Log(request.responseCode);
                Debug.Log(request.error);
                Debug.Log(request.downloadHandler.text);
                return;
            }
            Debug.Log(request.downloadHandler.text.StartsWith("["));
            // issuesJ = JsonConvert.DeserializeObject(request.downloadHandler.text);
            if (issuesJ == null)
                issuesJ = JArray.Parse(request.downloadHandler.text);
            else
            {
                //  JArray newIssues = ;

                // JArray an JArray anhängen
                JArray jArray = JArray.Parse(request.downloadHandler.text);

                if(jArray!=null)
                {
                    foreach (JToken item in jArray)
                    {
                        issuesJ.Add(item);   // Elemente hinzufügen
                    }
                }
            }
            //String a = x["body"]?.ToString();
            // Filter pullrequests raus
            issuesJ = new JArray(issuesJ.Where(x =>
            {
              //  string body = x["body"]?.ToString() ?? "";
                return x["pull_request"] == null;
                //&&
                //       body.Length <= 229 &&
                //       body.Contains(".cs");
            }));
            
          //  x["pull_request"] == null && x["body"]?.ToString().Length<= 229 &&  .Contains(".cs") ));
            Debug.Log("Anzahl Issues: " + issuesJ.Count);

            //foreach (var issue in issuesJ)
            //{
            //    Debug.Log(" Issue: " + issue["title"]);
            //}

            // maxPage++;
            //if (maxPage == 0)
            //{
            //    // Split des Linkes
            //    string test =  request.GetResponseHeader("Link");
            //    Debug.Log($"responseheader: {request.GetResponseHeader("Link")}");
            //    string temp = null;
            //    if (request.GetResponseHeader("Link").Split().Count() >= 3)
            //        temp = request.GetResponseHeader("Link").Split()[2];

            //    //index bevor sonderzeichen (">;")
            //    int index = temp.Count() - 3;
            //    //Dekrementiert den index bis dieser auf die erste Ziffer von der Maximalen Page Anzahl steht
            //    while (char.IsNumber(temp[index - 1]))
            //    {
            //        index--;
            //    }

            //    maxPage = int.Parse(temp.Substring(index, (temp.Count() - 2) - index));
            //}
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            //// Write the string array to a new file named "WriteLines.txt".
            //using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "IssueTestOutput.txt")))
            //{

            //    outputFile.Write(request.downloadHandler.text);
            //}



            //Dictionary<string, System.Object> dic = JsonConvert.DeserializeObject<Dictionary<string, System.Object>>(request.downloadHandler.text);

            //total = Convert.ToInt32(dic["total"]);

            //UnityEngine.Debug.Log($"IssueConvert:{total}");
            // total = (int)dic["total"];

            // UnityEngine.Debug.Log($"Start at: {rootobject.startAt.ToString()}");
            // startAT = Convert.ToInt32(dic["startAt"]) + Convert.ToInt32(dic["maxResults"]);// rootobject.startAt + rootobject.maxResults;
            //total = rootobject.total;
            //// -1 da die 0 mit z?hlt
            //startAT = rootobject.startAt + rootobject.maxResults;

            //// gibt den descriptions aller Issues in der Console aus.
            ///   
            //Dictionary<string, System.Object> issuesDictionary = JsonConvert.DeserializeObject<Dictionary<string, System.Object>>( dic["issues"].ToString());


            //DeserializeObject der Json response
            //List<Issue> issueList = JsonConvert.DeserializeObject<List<Issue>>(request.downloadHandler.text);

            // gibt den Titel aller Issues in der Console aus.
            //foreach (Issue issue in issueList)
            //{
            //    UnityEngine.Debug.Log("title:" + issue.title + "/n");
            //}
            //if (currentPage == -1)
            //    currentPage = 1;
            //else
            currentPage += 1;
        }


    }

    public async Task<int> transferIssues( string owner, string repo, string token)
    {
        Debug.Log($"transferIssues");
        if (issuesJ == null)
            return 0;
        int countCreatedIssues = 0;


        foreach (JObject issue in issuesJ)
        {
            // Created Date
            string createdAtRaw = issue["created_at"]?.ToString();
            DateTime createdAt = DateTime.Parse(createdAtRaw);

            // Format für den Titel
            string createdDateFormatted = createdAt.ToString("dd.MM.yyyy");

            //   Titel
            string title = issue["title"]?.ToString()+ $" {createdDateFormatted}";

            // Body / Beschreibung
            string body = issue["body"]?.ToString() ?? "";

            // Labels
            string[] labels = issue["labels"]?.Select(l => l["name"]?.ToString()).ToArray();

            // Assignees
            string[] assignees = issue["assignees"]?.Select(a => a["login"]?.ToString()).ToArray();

            // Milestone
            //  int? milestone = issue["milestone"]?["number"]?.ToObject<int?>();
            //Issue State
            string state = issue["state"]?.ToString();



            // Objekt für POST Body erzeugen
            var newIssuePayload = new
            {
                title = title,
                body = body,
              //  labels = labels,
           //     assignees = assignees,
            //    milestone = milestone
            };

            List<String> ignoredProperties = new List<String>
            {
            "id", "node_id", "user",
            "created_at", "updated_at", "closed_at",
             "html_url",
            };

            //JObject payload = new JObject();

            //foreach (var prop in issue.Properties())
            //{
            //    if (!ignoredProperties.Contains(prop.Name))
            //    {
            //        payload[prop.Name] = prop.Value;
            //        Debug.Log($"{payload[prop.Name]}->{prop.Value}");
            //    }
            //}

            //if (issue["assignee"] != null && issue["assignee"].HasValues)
            //{
            //    payload["assignee"] = issue["assignee"]["login"]?.ToString();
            //}
            //else
            //{
            //    if (issue["assignees"] != null && issue["assignees"].Any())
            //    {
            //        JArray arr = new JArray();
            //        foreach (var a in issue["assignees"])
            //            arr.Add(a["login"]?.ToString());

            //        payload["assignees"] = arr;
            //    }
            //}

            //if (issue["labels"] != null && issue["labels"].Any())
            //{
            //    var lbl = new JArray();

            //    foreach (var l in issue["labels"])
            //        lbl.Add(l["name"]?.ToString());

            //    newIssuePayload["labels"] = lbl;
            //}

            Debug.Log($"Logstate:{state}");
            using (var client = new HttpClient())
            {
                // GitHub API base URL
                client.BaseAddress = new Uri("https://api.github.com/");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("UnityApp/1.0");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", token);
                // client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

               string json = JsonConvert.SerializeObject(newIssuePayload);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                try
                {
                    Debug.Log($"repos/{owner}/{repo}/issues");

                    HttpResponseMessage response = await client.PostAsync($"repos/{owner}/{repo}/issues", content);
                    //      Debug.Log("Response:\n" + response);
                    Debug.Log("Response:\n" + response);
                    string result = await response.Content.ReadAsStringAsync();
                    Debug.Log("Response:\n" + result);
                    
                    var createdIssue = JObject.Parse(result);
                    int newIssueNumber = createdIssue["number"].ToObject<int>();

                    //Setlablel

                    string payloadJson = JsonConvert.SerializeObject(labels); //  ["bug","high-priority"]

                     content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                    // POST Request an GitHub
                    HttpResponseMessage responseLabel = await client.PostAsync(
                        $"repos/{owner}/{repo}/issues/{newIssueNumber}/labels",
                        content
                    );

                    string resultLabel = await responseLabel.Content.ReadAsStringAsync();
                    Debug.Log("Set Labels Response: " + responseLabel.StatusCode);
                    Debug.Log("Response Content: " + resultLabel);


                    // Schließen falls Original geschlossen war
                    if (state == "closed")
                    {
        
                        //string closePayloadJson = JsonConvert.SerializeObject(new { state = "closed" });
                        //// Erstellen der HttpRequestMessage
                        //HttpRequestMessage request = new HttpRequestMessage(
                        //    HttpMethod.Patch,
                        //    $"https://api.github.com/repos/{owner}/{repo}/issues/{newIssueNumber}"
                        //);

                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"https://api.github.com/repos/{owner}/{repo}/issues/{newIssueNumber}");
                        request.Content = new StringContent(JsonConvert.SerializeObject(new { state = "closed" }), Encoding.UTF8, "application/json");
                        request.Headers.Add("X-HTTP-Method-Override", "PATCH");

                        HttpResponseMessage closeResponse = await client.SendAsync(request);


                        Console.WriteLine("Close Response: " + closeResponse.StatusCode);
                    }

                  

                    if (response.IsSuccessStatusCode)
                    {
                        countCreatedIssues++;
                        string responseBody = await response.Content.ReadAsStringAsync();
                        Debug.Log("Issue created successfully:\n" + responseBody);
                      // return true;
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Debug.Log($"Error:{response.StatusCode}\n{error}");
                    //    return true;

                    }
  
                    Thread.Sleep(300); 
                   
                }
                catch (Exception ex)
                {
                    Debug.Log($"Error:{ex}");
                }
                Debug.Log($"Created  Issues:{countCreatedIssues}");
                
            }
           
            //string jsonPayload = JsonConvert.SerializeObject(newIssuePayload);

            //string url = $"https://api.github.com/repos/{owner}/{repo}/issues";

            //UnityWebRequest req = new UnityWebRequest(url, "POST");
            //byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            //req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            //req.downloadHandler = new DownloadHandlerBuffer();

            //req.SetRequestHeader("Content-Type", "application/json");
            //req.SetRequestHeader("Accept", "application/vnd.github+json");
            //req.SetRequestHeader("Authorization", $"token {token}");
            //req.SetRequestHeader("User-Agent", "UnityImporter");

            // req.SendWebRequest();

            //if (req.result == UnityWebRequest.Result.Success)
            //{
            //    Debug.Log("Issue importiert: " + title);
            //}
            //else
            //{
            //    Debug.LogError("Fehler: " + req.error + " --- " + req.downloadHandler.text);
            //}

        }
        return countCreatedIssues;
    }


    public async Task<JArray> getIssues( Settings settings)
    {
        // bug need to be fixed need to set Setting here thats wrong
        //ResetIssues
        //  issues = null;
        //var settings = new IssueReceiverInterface.Settings
        //{
        //    preUrl = "https://api.github.com/repos/uni-bremen-agst/SEE/issues",
        //    searchUrl = "?filter=all"
        //};
       // Debug.Log($"Getissues preurl  {this.settings.preUrl}");
        issuesJ = null;

        //Settings settings = new IssueReceiverInterface.Settings
        //{
        //    preUrl = "https://api.github.com/repos/uni-bremen-agst/SEE/issues",
        //   // preUrl = $"https://api.github.com/repos/{owner}/{repo}/issues",
        //    searchUrl = "?state=open"
        //};

        await restAPI(null
            //new IssueReceiverInterface.Settings
            //{
            //    preUrl = "https://api.github.com/repos/uni-bremen-agst/SEE/issues",
            //    searchUrl = "?state=open" //
            //}
            );

        return issuesJ;

    }


}