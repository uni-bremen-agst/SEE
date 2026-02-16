using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SEE.Game.City;
using SEE.UI.Notification;
using SEE.Utils.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static IssueReceiverInterface;
[Serializable]

public class GitLabIssueReceiver : BasicIssueProvider
{
    public GitLabIssueReceiver(SEECity city) : base(city)
    {
        preUrl = $"https://yourgitlabdomain.com/api/v4/projects/{projekt}/issues";
        filterQueryStr = "";
    }
    [SerializeField]
    public override IssueReceiverInterface.IssueProvider Type => IssueReceiverInterface.IssueProvider.GitLabIssueReceiver;

    public override Dictionary<string, string> getCreateIssueAttributes()
    {
        return new Dictionary<string, string> {{ "Title", "" },
                                                { "Description", "" },
                                                   { "Assignee", "" },
                                                { "Labels", "" }
            };
    }
    protected internal static GitLabIssueReceiver RestoreProvider(Dictionary<string, object> values)
    {
        IssueReceiverInterface.IssueProvider IssueProvider = IssueReceiverInterface.IssueProvider.GitLabIssueReceiver;

        //  Debug.Log($"LoadeConfig Owner: {label}");

        GitLabIssueReceiver gitLabIssueReceiver = new GitLabIssueReceiver(new SEECity()); ;// new GitHubIssueReceiver();
                                                                                           // gitHubIssueReceiver.Type = "GitHubIssueReceiver";
        if (ConfigIO.RestoreEnum(values, "Type", ref IssueProvider))
        {
            // Debug.Log($"IssueRestoreProvider: {IssueProvider}" );
            //  Debug.Log($"IssueRestore Owner: {values["Owner"]}");
            gitLabIssueReceiver.RestoreAttributes(values);
            return gitLabIssueReceiver;
        }
        else
        {
            throw new Exception($"Specification of graph provider is malformed: label {IssueProvider} is missing.");
        }
    }
    public override void SaveInternal(ConfigWriter writer, String label)
    {
        SaveAttributes(writer);
        Debug.Log($"SaveConfig Owner: {label}");

    }
    public static GitLabIssueReceiver Restore(Dictionary<string, object> attributes, string label)
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
        writer.BeginGroup("dataIssueReceiverGitLab");
        writer.Save(this.GetType().ToString(), "Type");
        writer.Save(projekt, "Projekt");
        writer.Save(filterQueryStr, "FilterQuery");
        writer.Save(token, "Token");
     //   SaveAttributes(writer);
        writer.EndGroup();
    }
    public override void RestoreAttributes(Dictionary<string, object> attributes)
    {
       
        Debug.Log($"RestoreAttributes Config : {attributes}");

        foreach (KeyValuePair<string, object> keyValuePair in attributes)
        {
            Debug.Log($"{keyValuePair.Key}:{keyValuePair.Value}");
        }
        Dictionary<string, object> dataIssueReceiver = (Dictionary<string, object>)attributes["dataIssueReceiverGitLab"];



        // owner = (string)dataIssueReceiver["Type"];
        //Gitlab angepasst

        projekt = (string)dataIssueReceiver["Projekt"];
        filterQueryStr = (string)dataIssueReceiver["FilterQuery"];
        token = (string)dataIssueReceiver["Token"];
        //City.IssueProjectName = projekt;
        //City.IssueToken = token;
        //City.IssueQueryFilterText = filterQueryStr;

        // owner = attributes["dataIssueReceiver"].ToString() ; //"lkuenzel";//
        //  repo = "IssueTrackerRepository";//attributes["Repo"].ToString();
        //token27.11.2025
        //Settings settings = new IssueReceiverInterface.Settings
        //{

        //   preUrl ="",// $"https://api.github.com/repos/{owner}/{repo}/issues",
        //   searchUrl = "?state=open"
        //};
        // this.settings = settings;
    }

//#endregion
    public override async Task<bool> createIssue(Dictionary<string, string> attributes)//string token, string owner
    {
        //  assignees = new[] { "CodeSEEBenutzer" },//self //"lkuenzel" attributes["Assignee"]
        //  labels = labelArray, //new[] {$"{attributes["Labels"]}" } ////   
        string url = $"https://gitlab.com/api/v4/projects/{projekt}/issues";

        var issueData = new
        {
            title = attributes["Title"],
            description  = attributes["Description"]
        };

        string json = JsonConvert.SerializeObject(issueData);
       byte[] content = Encoding.UTF8.GetBytes(json);
       // StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
        UnityWebRequest request = new UnityWebRequest(url, "POST");


        request.uploadHandler = new UploadHandlerRaw(content);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("PRIVATE-TOKEN", token);

        //Wait for answer from webrequest
        await  request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Issue created successfully!");
            Debug.Log(request.downloadHandler.text);
            ShowNotification.Success("Gitlab Issue was created successfully", "Info", 10);
            return true;
        }
        else
        {
            Debug.LogError($"HTTP Error: {request.responseCode}");
            ShowNotification.Error("Gitlab Issue could't created", "Info", 10);
            Debug.LogError("Error creating issue: " + request.error);
            Debug.LogError(request.downloadHandler.text);
            return false;
        }
    }



     //   return false;
   // }

    //     using (var client = new HttpClient())
    //{
    //    client.DefaultRequestHeaders.Add("PRIVATE-TOKEN", privateToken);

    //    var url = $"https://gitlab.com/api/v4/projects/{projectId}/issues";

    //    var response = await client.GetAsync(url);
    //    response.EnsureSuccessStatusCode();

    //    var content = await response.Content.ReadAsStringAsync();
    //    Console.WriteLine(content);
    //}

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
          //  preUrl = $"https://api.github.com/repos/{owner}/{repo}/issues",
            searchUrl = "?state=open"
        };
        //filterQueryStr = "";// "?state=open";
       // projekt = "codeseebenutzer-group%2FCodeSEEBenutzer-project";
      
        if (!filterQueryStr.Equals(""))
        {
            filterQueryStr = $"&{filterQueryStr}";
        }
        //this.settings = settings;

        //this.settings.preUrl = $"https://api.github.com/repos/{owner}/{repo}/issues";


        int maxPage = 1;
        // UnityWebRequest request = UnityWebRequest.Get($"https://api.github.com/repos/uni-bremen-agst/SEE/issues?state=all{pageingStr}"); //;rel=last &since=2024-01-01T00:00:00Z

        //https://gitlab.com//CodeSEEBenutzer-project/-/tree/a5bf0d19fd4d01bb8fa728702fee8a1a3a6d8af4/
        // 
        // string requestUrl = "https://api.github.com/repos/uni-bremen-agst/SEE/issues?state=closed&per_page=100&Page=0;rel=last";// "https://api.github.com/repos/koschke/uni-bremen-agst/SEE/issues"; //"";
        int currentPage = 1;
        string pageingStr = "";
        while (maxPage >= currentPage)
        {
            //pageingStr = $"&per_page=100&page={currentPage.ToString()}";
            Debug.Log($"IssueLogURL: {preUrl + filterQueryStr}");
            preUrl = $"https://gitlab.com/api/v4/projects/{projekt}/issues";
            UnityWebRequest request = UnityWebRequest.Get($"{preUrl}{pageingStr}{filterQueryStr}"); //;rel=last
                                                                                               
            request.SetRequestHeader("User-Agent", "UnityApp");
            if (token != null && token != "TestToken" && token != "")
                request.SetRequestHeader("PRIVATE-TOKEN", token);
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

                if (jArray != null)
                {
                    foreach (JToken item in jArray)
                    {
                        issuesJ.Add(item);   // Elemente hinzufügen
                    }
                }
            }
            issuesJ = new JArray(issuesJ.Where(x =>
            {
                return x["pull_request"] == null;

            }));

   
            Debug.Log("Anzahl Issues: " + issuesJ.Count);

            return;
 
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
 
            currentPage += 1;
        }


    }

    public async Task<JArray> getIssues(Settings settings)
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
