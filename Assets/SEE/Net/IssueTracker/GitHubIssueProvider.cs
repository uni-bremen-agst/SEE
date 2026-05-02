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
using Cysharp.Threading.Tasks;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
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
using UnityEngine;
using UnityEngine.Networking;
using static IssueReceiverInterface;
[Serializable]
public class GitHubIssueProvider : BasicIssueProvider  
{
    public GitHubIssueProvider(SEECity city) : base(city)
    {

        filterQueryStr = "";
    }

    [SerializeField]
    public override IssueReceiverInterface.IssueProvider Type => IssueReceiverInterface.IssueProvider.GitHubIssueProvider;


    [SerializeField] public string owner = "";

    static private string label = "Data";

    public override Dictionary<string, string> getCreateIssueAttributes()
    {
        return new Dictionary<string, string> {{ "Title", "" },
                                                { "Description", "" },
                                                   { "Assignee", $"{defaultAssignee}" },
                                                { "Labels", "Report" }
            };
    }
    
    #region Config I/O

    public override void SaveInternal(ConfigWriter writer,String label)
    {
        SaveAttributes(writer);
        Debug.Log($"SaveConfig Owner: {label}");

    }


    /// <summary>
    /// Saves the GitHubProvider data in the Save File with the ConfigWriter.
    /// </summary>
    public void SaveAttributes(ConfigWriter writer)
    {
        try
        {
            writer.BeginGroup("dataIssueProvider");
            writer.Save(Type.ToString(), "Type");
            writer.Save(owner, "Owner");
            writer.Save(projekt, "Repo");
            writer.Save(token, "Token");
            writer.Save(filterQueryStr, "FilterQueryStr");
            writer.Save(defaultAssignee, "DefaultAssignee");
            writer.EndGroup();
        }
        catch (Exception e)
        {
            ShowNotification.Error($"Save Failed:{e.Message}", "Error", 5);
        }
    }

    /// <summary>
    /// Restores the GitHubProvider data from the Save File ('City'.cfg).
    /// </summary>
    public override void RestoreAttributes(Dictionary<string, object> attributes)
    {
        try
        { 
            Dictionary<string, object> dataIssueReceiver = (Dictionary<string, object>)attributes["dataIssueProvider"];
            owner = (string)dataIssueReceiver["Owner"];
            projekt = (string)dataIssueReceiver["Repo"];
            token = (string)dataIssueReceiver["Token"];
            filterQueryStr = (string)dataIssueReceiver["FilterQueryStr"];
            defaultAssignee = (string)dataIssueReceiver["DefaultAssignee"];
        }
        catch (Exception e)
        {
            ShowNotification.Error($"Restore Failed:{e.Message}", "Error", 5);
        }
    }

    /// <summary>
    /// CreateIssue gets @Param attributes and use the Attributes to create a Issue with the Values in it
    /// </summary>
    public override async Task<bool> createIssue(Dictionary<string, string> attributes)//string token, string owner
    {
        string[] labelArray = attributes["Labels"]?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToArray();

        var issueData = new
        {
            title = attributes["Title"],
            body = attributes["Description"],
            assignees = new[] { attributes["Assignee"] }, // new[] { "CodeSEEBenutzer" },//self //"lkuenzel" attributes["Assignee"]
            labels = labelArray, //new[] {$"{attributes["Labels"]}" } ////   
        };
        
        using (UnityWebRequest request = new UnityWebRequest($"https://api.github.com/repos/{owner}/{projekt}/issues", "POST"))
        {

           

            string json = JsonConvert.SerializeObject(issueData); 
            byte[] content = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(content);
           request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", $"token {token}");
            request.SetRequestHeader("User-Agent", "UnityApp");
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/vnd.github+json");


            //Wait for answer from webrequest
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();


            while (!operation.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ShowNotification.Success("Github Issue was created successfully", "Info", 10);
                return true;
            }
            else
            {
                switch (request.responseCode)
                {
                    case 403:
                        ShowNotification.Error("Github create Issues: authentication or API Token", "Info", 10);
                        break;
                    case 401:
                        ShowNotification.Error("Github create Issues: connection error", "Info", 10);
                        break;

                    default:
                        ShowNotification.Error($"Github create Issues could't get from API  error {request.responseCode}", "Info", 10);
                        break;
                }
              
                return false;
            }
        }
    }
    public async Task<bool> updateIssue()
    {
        return false;
    }


    /// <summary>
    /// Get the Github Issues with rest API
    /// </summary>
    private async Task restAPI(Settings settings)
    {

        preUrl = $"https://api.github.com/repos/{owner}/{projekt}/issues?"; //https://api.github.com/ https://api.github.com/repos

        int maxPage = 1;
        int currentPage = 1;
        string pageingStr = "";
        while (maxPage >= currentPage)
        {
                  pageingStr = $"&per_page=100&page={currentPage.ToString()}";
            Debug.Log($"IssueLogURL: {preUrl + filterQueryStr}");
             UnityWebRequest request = UnityWebRequest.Get($"{preUrl}{filterQueryStr}{pageingStr}"); //;rel=last
        
            request.SetRequestHeader("User-Agent", "UnityApp");
            if (token != null && token != "TestToken" && token != "")
                request.SetRequestHeader("Authorization", $"token {token}");

            request.SetRequestHeader("X-GitHub-Api-Version", "2026-03-10");
      
            request.SetRequestHeader("Accept", "application/vnd.github+json");

            #pragma warning disable CS4014
            request.SendWebRequest();
           #pragma warning restore CS4014
              await UniTask.WaitUntil(() => request.isDone);



            if (request.result == UnityWebRequest.Result.Success)
            {

               
                string linkHeader = request.GetResponseHeader("Link");
                if (!string.IsNullOrEmpty(linkHeader) && linkHeader.Contains(">; rel=\"next\""))
                {
                    maxPage += 1;
                }
            }
            else
            {
                switch (request.responseCode)
                {
                    case 403:
                        ShowNotification.Error("Github  Issues: authentication or API Token error", "Info", 10);
                        break;
                    case 401:
                        ShowNotification.Error("Github  Issues: connection error", "Info", 10);
                        break;

                    default:
                        ShowNotification.Error($"Github  Issues could't get from API  error {request.responseCode}", "Info", 10);
                        break;
                }
                return;
            }

            if (issuesJ == null)
                issuesJ = JArray.Parse(request.downloadHandler.text);
            else
            {

                JArray jArray = JArray.Parse(request.downloadHandler.text);

                if(jArray!=null)
                {
                    foreach (JToken item in jArray)
                    {
                        issuesJ.Add(item);   // Elemente hinzufügen
                    }
                }


              
            }
            //foreach (JToken issue in issuesJ)
            //{
               
            //    JArray labels = (JArray)issue["labels"];

            //    if (labels != null)
            //    {
            //        // Umstrukturieren der Labels für das Issue
            //        JObject structuredLabels = new JObject();
                   


            //        foreach (JToken label in labels)
            //        {
            //            string labelName = label["name"].ToString();
            //            structuredLabels[labelName] = ""; 

            //          }

            //            issue["labels"] = structuredLabels;
            //    }
            //    JArray assignees = (JArray)issue["assignees"];

            //    if (assignees != null)
            //    {
            //        // Umstrukturieren der assignees für das Issue
            //        JObject structuredassignees = new JObject();

            //        foreach (JToken assignee in assignees)
            //        {
            //            string labelName = assignee["id"].ToString();
            //            structuredassignees[labelName] = "";
            //        }

            //        issue["assignees"] = structuredassignees;
            //    }
            //}






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
            // Write the string array to a new file named "WriteLines.txt".
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "IssueTestOutput.txt")))
            {

                outputFile.Write(request.downloadHandler.text);
            }


 
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


            Debug.Log($"Logstate:{state}");
            using (HttpClient client = new HttpClient())
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
  
                    System.Threading.Thread.Sleep(300); 
                   
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