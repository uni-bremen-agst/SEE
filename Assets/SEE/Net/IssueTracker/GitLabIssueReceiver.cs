using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SEE.Game.City;
using SEE.Utils.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static IssueReceiverInterface;

public class GitLabIssueReceiver : BasicIssueProvider
{
    public GitLabIssueReceiver(SEECity city) : base(city)
    {
        preUrl = $"https://yourgitlabdomain.com/api/v4/projects/{projekt}/issues";
    }
    public override IssueReceiverInterface.IssueProvider Type => IssueReceiverInterface.IssueProvider.GitLabIssueReceiver;
  

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
        //writer.Save(owner, "Owner");
        //writer.Save(repo, "Repo");
        //writer.Save(token, "Token");
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
        // owner = (string)dataIssueReceiver["Type"];
        //Gitlab angepasst
        //owner = (string)dataIssueReceiver["Owner"];
        //repo = (string)dataIssueReceiver["Repo"];
        //token = (string)dataIssueReceiver["Token"];

        // owner = attributes["dataIssueReceiver"].ToString() ; //"lkuenzel";//
        //  repo = "IssueTrackerRepository";//attributes["Repo"].ToString();
        //token27.11.2025
        Settings settings = new IssueReceiverInterface.Settings
        {

           preUrl ="",// $"https://api.github.com/repos/{owner}/{repo}/issues",
           searchUrl = "?state=open"
        };
        this.settings = settings;
    }

//#endregion
    public override async Task<bool> createIssue(Dictionary<string, string> attributes)//string token, string owner
    {
        return false;
    }

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
        filterQuery = "?state=open";

        if(!filterQuery.Equals(""))
        {
            filterQuery = $"&{filterQuery}";
        }
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
            UnityWebRequest request = UnityWebRequest.Get($"{preUrl}{pageingStr}{filterQuery}"); //;rel=last
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
