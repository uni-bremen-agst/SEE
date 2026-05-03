using Crosstales;
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

public class GitLabIssueProvider : BasicIssueProvider
{
    public GitLabIssueProvider(SEECity city) : base(city)
    {
        preUrl = $"https://yourgitlabdomain.com/api/v4/projects/{projekt}/issues";
        filterQueryStr = "";
    }
    [SerializeField]
    public override IssueReceiverInterface.IssueProvider Type => IssueReceiverInterface.IssueProvider.GitLabIssueProvider;

    public override Dictionary<string, string> getCreateIssueAttributes()
    {
        return new Dictionary<string, string> {{ "Title", "" },
                                                { "Description", "" },
                                                   { "AssigneeID", defaultAssignee  !="" ? defaultAssignee : ""},
                                                { "Labels", "" }
            };
    }
    //protected internal static GitLabIssueProvider RestoreProvider(Dictionary<string, object> values)
    //{
    //    IssueReceiverInterface.IssueProvider IssueProvider = IssueReceiverInterface.IssueProvider.GitLabIssueProvider;

    //    //  Debug.Log($"LoadeConfig Owner: {label}");

    //    GitLabIssueProvider gitLabIssueReceiver = new GitLabIssueProvider(new SEECity()); ;// new GitHubIssueReceiver();
    //                                                                                       // gitHubIssueReceiver.Type = "GitHubIssueReceiver";
    //    if (ConfigIO.RestoreEnum(values, "Type", ref IssueProvider))
    //    {
    //        // Debug.Log($"IssueRestoreProvider: {IssueProvider}" );
    //        //  Debug.Log($"IssueRestore Owner: {values["Owner"]}");
    //        gitLabIssueReceiver.RestoreAttributes(values);
    //        return gitLabIssueReceiver;
    //    }
    //    else
    //    {
    //        throw new Exception($"Specification of graph provider is malformed: label {IssueProvider} is missing.");
    //    }
    //}
    #region Config I/O
    public override void SaveInternal(ConfigWriter writer, String label)
    {
        SaveAttributes(writer);
    }
    //public static GitLabIssueProvider Restore(Dictionary<string, object> attributes, string label)
    //{
    //    if (attributes.TryGetValue(label, out object dictionary))
    //    {
    //        Dictionary<string, object> values = dictionary as Dictionary<string, object>;

    //        return RestoreProvider(values);
    //    }
    //    else
    //    {
    //        throw new Exception($"A GitHubIssue Provider could not be found under the label {label}.");
    //    }
    //}


    public void SaveAttributes(ConfigWriter writer)
    {
        writer.BeginGroup("dataIssueProvider");
        writer.Save(Type.ToString(), "Type");
        writer.Save(projekt, "Projekt");
        writer.Save(filterQueryStr, "FilterQuery");
        writer.Save(defaultAssignee, "DefaultAssigneeID");
        writer.Save(token, "Token");
        writer.EndGroup();
    }
    public override void RestoreAttributes(Dictionary<string, object> attributes)
    {
       
        Debug.Log($"RestoreAttributes Config : {attributes}");

        foreach (KeyValuePair<string, object> keyValuePair in attributes)
        {
            Debug.Log($"{keyValuePair.Key}:{keyValuePair.Value}");
        }
        Dictionary<string, object> dataIssueProvider;

        if(!attributes.TryGetValue("dataIssueProvider", out System.Object obj))
            {
            // only contine if the Key "dataIssueProvider" contains in attributes
            return;
        }
        else
        {
            dataIssueProvider = (Dictionary<string, object>)attributes["dataIssueProvider"];
        }
        projekt = (string)dataIssueProvider["Projekt"];
        filterQueryStr = (string)dataIssueProvider["FilterQuery"];
        token = (string)dataIssueProvider["Token"];
        defaultAssignee = (string)dataIssueProvider["DefaultAssigneeID"];
    }

#endregion
    public override async Task<bool> createIssue(Dictionary<string, string> attributes)//string token, string owner
    {
        string url = $"https://gitlab.com/api/v4/projects/{projekt}/issues";

    
        String keyValue;
        int assigneeId = -1;
        if (attributes["AssigneeID"] != "" && attributes.TryGetValue("AssigneeID", out keyValue) && attributes["AssigneeID"].CTIsNumeric())
        {
            assigneeId = int.Parse(attributes["AssigneeID"]);
        }

        var issueData = new
        {
            title = attributes["Title"],
            description = attributes["Description"],
            labels = attributes["Labels"],
            assignee_ids = assigneeId > 0 ? new int[] { assigneeId } : null
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
            switch (request.responseCode)
            {
                case 403:
                    ShowNotification.Error("Gitlab Issues  could't created: authentication or API Token", "Info", 10);
                    break;
                case 401:
                    ShowNotification.Error("Gitlab Issues  could't created: connection Error", "Info", 10);
                    break;

                default:
                    ShowNotification.Error($"Gitlab Issues  could't created Error {request.responseCode}", "Info", 10);
                    break;
            }
            return false;
        }
    }

    private async Task restAPI(Settings settings)
    {

        this.settings = new IssueReceiverInterface.Settings
        {
          //  preUrl = $"https://api.github.com/repos/{owner}/{repo}/issues",
          //  searchUrl = "?state=open"
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
            pageingStr = $"?per_page=100&page={currentPage.ToString()}";
          
            preUrl = $"https://gitlab.com/api/v4/projects/{projekt}/issues";
            Debug.Log($"IssueLogURL: {preUrl + pageingStr + filterQueryStr}");
            UnityWebRequest request = UnityWebRequest.Get($"{preUrl}{pageingStr}{filterQueryStr}"); //;rel=last
   
            request.SetRequestHeader("User-Agent", "UnityApp");
            Debug.Log($"token: {token}");

            if (token != null && token != "")
                request.SetRequestHeader("PRIVATE-TOKEN", token);
            request.SetRequestHeader("Accept", "application/json");
            //  request.SetRequestHeader("Authorization", $"AxToken {pke}");
            //#pragma warning disable CS4014
            //            request.SendWebRequest();
            //#pragma warning restore CS4014
             await request.SendWebRequest();

            //#pragma warning disable CS4014
                     //  request.SendWebRequest();
            //#pragma warning restore CS4014
            //            await UniTask.WaitUntil(() => request.isDone);
          //  UnityWebRequestAsyncOperation operation = request.SendWebRequest();
          //  await operation;
          ////  while (!operation.isDone)
          //      await Task.Yield();

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
                switch (request.responseCode)
                {
                    case 403:
                        ShowNotification.Error("Gitlab Issues: authentication or API Token Error", "Info", 10);
                        break;
                    case 401:
                        ShowNotification.Error("Gitlab Issues: connection Error", "Info", 10);
                        break;

                    default:
                        ShowNotification.Error($"Gitlab Issues could't get from API Error {request.responseCode}", "Info", 10);
                        break;
                }
                return;
            }    
          
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
            //issuesJ = new JArray(issuesJ.Where(x =>
            //{
            //    return x["pull_request"] == null;

            //}));

            Debug.Log("Anzahl Issues: " + issuesJ.Count);

 
            currentPage += 1;
        }
        ShowNotification.Success("Gitlab Issues: Loading", "Info", 5);

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
