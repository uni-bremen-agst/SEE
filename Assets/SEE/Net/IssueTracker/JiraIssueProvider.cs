using Antlr4.Runtime;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SEE.Game.City;
using SEE.GraphProviders;
using SEE.UI.Notification;
using SEE.Utils.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static IssueReceiverInterface;


public class JiraIssueProvider : BasicIssueProvider
{
    [SerializeField]
    public override IssueReceiverInterface.IssueProvider Type => IssueReceiverInterface.IssueProvider.JiraIssueProvider;
    public JiraIssueProvider(SEECity city) :  base(city)
    {
        preUrl = $"https://yourgitlabdomain.com/api/v4/projects/{projekt}/issues";
    }
    static private string label = "Data";

    [SerializeField] public string email = "";

    [SerializeField] public string domain = "";

    /// <summary>
    /// Creates a List of attributes which will show in the CreateIssueWindow
    /// If there should be more attributes they need to be added here and
    /// need to be handled in the createIssue function also.
    /// </summary>
    public override Dictionary<string, string> getCreateIssueAttributes()
    {
        return new Dictionary<string, string> {{ "Title", "" },
                                                { "Description", "" },                                    
            };
    }

    /// <summary>
    /// Override the SaveInternal function and Calls the internal SaveAttributes function.
    /// </summary>
    public override void SaveInternal(ConfigWriter writer, String label)
    {
        SaveAttributes(writer);
    }

    /// <summary>
    /// Saves the JiraProvider data in the Save File with the ConfigWriter.
    /// </summary>
    public void SaveAttributes(ConfigWriter writer)
    {
        try
        {
        writer.BeginGroup("dataIssueProvider");
        writer.Save(Type.ToString(), "Type");   
        writer.Save(domain, "Domain");
        writer.Save(email, "EMail");
        writer.Save(projekt, "ProjectName");
        writer.Save(token, "Token");
        writer.Save(filterQueryStr, "FilterQueryStr");
        writer.Save(defaultAssignee, "DefaultAssignee");
        writer.EndGroup();
        }    
        catch(Exception e)
        {
            ShowNotification.Error($"Save Failed:{e.Message}", "Error", 5);
        }
    }
    /// <summary>
    /// Restores the JiraProvider data in the Save File with the ConfigWriter.
    /// </summary>
    public override void RestoreAttributes(Dictionary<string, object> attributes)
    {
        try
        {
            Dictionary<string, object> dataIssueReceiver = (Dictionary<string, object>)attributes["dataIssueProvider"];
            domain = (string)dataIssueReceiver["Domain"];
            email = (string)dataIssueReceiver["EMail"];
            projekt = (string)dataIssueReceiver["ProjectName"];
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
    /// Restores the JiraProvider data in the Save File with the ConfigWriter.
    /// 'attributes' contain the Data for the Issue creation from the CreateIssueWindow
    /// </summary>
    public override async Task<bool> createIssue(Dictionary<string, string> attributes)
    {
        string jiraDomain = $"{domain}.atlassian.net";
        string url = $"https://{jiraDomain}/rest/api/3/issue";
        //Dictionary to create a Doc in Jira with Content(Description)
        Dictionary<string, object> descriptionDict = new Dictionary<string, object>
        {
                { "type", "doc" },
                { "version", 1 },
                { "content", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "type", "paragraph" },
                            { "content", new List<object>
                                {
                                    new Dictionary<string, object>
                                    {
                                        { "type", "text" },
                                        { "text", attributes["Description"] }
                                    }
                                }
                            }
                        }
                    }
                }
            };

        if (projekt == "" || projekt == null )
        {
            ShowNotification.Error($"Create Failed: Projekt Name is empty or null", "Error", 5);
            return false;
        }
        //Dictionary object contains descriptionDict, titel and the Project where the Task should be created
        Dictionary<string, object> fields = new Dictionary<string, object>
        {
            { "project", new Dictionary<string, string> { { "key", projekt } } },

            { "summary",  $"{attributes["Title"]}" },
            { "issuetype", new Dictionary<string, string> { { "name", "Task" } } },
            { "description", descriptionDict }
        };
        // Include defaultAssignee when set
        if (!defaultAssignee.Equals("") && defaultAssignee != null)
        {
            fields.Add("assignee", new Dictionary<string, string> { { "accountId", defaultAssignee } });
        }

        Dictionary<string, object> payload = new Dictionary<string, object>
        {
            { "fields", fields }
        };

       
        string json = JsonConvert.SerializeObject(payload, Formatting.Indented);


        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        string auth = System.Convert.ToBase64String(Encoding.ASCII.GetBytes(email + ":" + token));
        request.SetRequestHeader("Authorization", "Basic " + auth);
        request.SetRequestHeader("Content-Type", "application/json");


        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        while (!operation.isDone)
            await Task.Yield();


        if (request.result == UnityWebRequest.Result.Success)
        {
            ShowNotification.Success("Jira Issue was created successfully", "Info", 10);
            return true;
        }
        else
        {
            ShowNotification.Error("Jira Issue could't created", "Info", 10);
            Debug.LogError(request.responseCode + " : " + request.downloadHandler.text);
        }
        return false;
    }
    public async Task<bool> updateIssue()
    {
        // Es konnte kein Issue upgedatet werden
        return false;
    }

    /// <summary>
    /// Get the Jira Issue with rest/api/3 
    /// First the Issues ID will get with a UnityWebRequest 
    /// after the function gets the Issue Data with each IssueID
    /// </summary>
    private async Task restAPI(Settings settings)
    {
        int total = 1;
        int startAT = 1;
        string jql = $"project={projekt} ORDER BY created DESC";
        preUrl = $"{domain}.atlassian.net/rest/api/3/search/jql?jql=project={projekt}";
        string pagingString = "";

        while (startAT <= total)
        {
            if (startAT != -1)
            {
                pagingString = $"&startAt={startAT.ToString()}";
            }
            //Jira Query Language(JQL)
            UnityWebRequest request = UnityWebRequest.Get(preUrl  + pagingString);
            string auth = System.Convert.ToBase64String(Encoding.ASCII.GetBytes(email + ":" + token));
            request.SetRequestHeader("Authorization", "Basic " + auth);
            request.SetRequestHeader("Content-Type", "application/json");

            #pragma warning disable CS4014
                        request.SendWebRequest();
            #pragma warning restore CS4014
                        await UniTask.WaitUntil(() => request.isDone);

            if (request.result != UnityWebRequest.Result.Success)
            {


                switch (request.responseCode)
                {
                    case 403:
                        ShowNotification.Error("Jira Issues: authentication or API Token", "Info", 10);
                        break;
                    case 401:
                        ShowNotification.Error("Jira Issues: connection error", "Info", 10);
                        break;

                    default:
                        ShowNotification.Error("Jira Issues could't get from API", "Info", 10);
                        break;
                }
                return;
               
            }
            else
            {
                ShowNotification.Success("Loading Jira Issues", "Info", 5);
            }


        //DeserializeObject of the Json response
        Dictionary<string, System.Object> dic = JsonConvert.DeserializeObject<Dictionary<string, System.Object>>(request.downloadHandler.text);

            JArray issueIDArray = (JArray)dic["issues"];
            bool isLast = (bool)dic["isLast"];
            if (!isLast)
                total++;

            foreach (JToken jToken in issueIDArray)
            {
                {
                    int IssueID = jToken.Value<int>("id");
                    Debug.Log($"ID:{IssueID}");

                    string issueUrl = $"{domain}.atlassian.net/rest/api/3/issue/{IssueID}";

                    using (UnityWebRequest issueRequest = UnityWebRequest.Get(issueUrl))
                    {

                        issueRequest.SetRequestHeader("Authorization", $"Basic {auth}");
                        issueRequest.SetRequestHeader("Accept", "application/json");

#pragma warning disable CS4014
                            issueRequest.SendWebRequest();
#pragma warning restore CS4014
                            await UniTask.WaitUntil(() => issueRequest.isDone);

                        if (issueRequest.result == UnityWebRequest.Result.Success)
                        {
                            string docPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "IssueTestOutput.txt")))
                            {

                                outputFile.Write(issueRequest.downloadHandler.text);
                            }

                            if (issuesJ == null)
                            {
                                issuesJ = new JArray();
                                issuesJ.Add(JObject.Parse(issueRequest.downloadHandler.text));
                            }
                            else
                            {
                                // new JArray from 'issueRequest' append to an issuesJ
                                JObject issueObj = JObject.Parse(issueRequest.downloadHandler.text);
                                issuesJ.Add(issueObj);

                            }
                        }
                        else
                        {
                            switch (issueRequest.responseCode)
                            {
                                case 403:
                                    ShowNotification.Error("Jira Issues: authentication or API Token", "Info", 10);
                                    break;
                                default:
                                    ShowNotification.Error($"Jira Issues: Errorcode {issueRequest.responseCode}", "Info", 10);
                                    break;
                            }
                        }
                    }

                }
            }
            startAT++;
        }
    

    }
    /// <summary>
    /// resets issueJ and calls restAPI to get 
    /// </summary>
    public async Task<JArray> getIssues(Settings settings)
    {
        issuesJ = null;
        await restAPI(settings);
        return issuesJ;
    }

}
