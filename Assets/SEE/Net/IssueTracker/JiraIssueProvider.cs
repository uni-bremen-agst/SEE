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
    public List<RootIssue> issues;
   // public JArray issuesJ=null;
    static private string label = "Data";

    [SerializeField] public string email = "";

    [SerializeField] public string domain = "";
    public override Dictionary<string, string> getCreateIssueAttributes()
    {
        return new Dictionary<string, string> {{ "Title", "" },
                                                { "Description", "" },                                    
            };
    }

    public override void SaveInternal(ConfigWriter writer, String label)
    {
        SaveAttributes(writer);
    }
    //protected internal static JiraIssueProvider RestoreProvider(Dictionary<string, object> values)
    //{
    //    IssueReceiverInterface.IssueProvider IssueProvider = IssueReceiverInterface.IssueProvider.JiraIssueProvider;
    //    if (ConfigIO.RestoreEnum(values, label, ref IssueProvider))
    //    {
    //        JiraIssueProvider jiraIssueReceiver = new JiraIssueProvider(new SEECity());
    //        jiraIssueReceiver.RestoreAttributes(values);
    //        return jiraIssueReceiver;
    //    }
    //    else
    //    {
    //        throw new Exception($"Specification of JiraIssueReceiver: label {IssueProvider} is missing.");
    //    }
    //}

    //public static JiraIssueProvider Restore(Dictionary<string, object> attributes, string label)
    //{
    //    if (attributes.TryGetValue(label, out object dictionary))
    //    {
    //        Dictionary<string, object> values = dictionary as Dictionary<string, object>;
    //        return RestoreProvider(values);
    //    }
    //    else
    //    {
    //        throw new Exception($"A JiraIssueReceiver could not be found under the label {label}.");
    //    }
    //}

    public void SaveAttributes(ConfigWriter writer)
    {
        //  writer.BeginGroup("dataIssueProviderJira");

        writer.BeginGroup("dataIssueProvider");
      
        writer.Save(Type.ToString(), "Type");
        writer.Save(domain, "Domain");
        writer.Save(email, "EMail");
        writer.Save(projekt, "ProjectName");
        writer.Save(token, "Token");
        writer.Save(filterQueryStr, "FilterQueryStr");
        writer.Save(defaultAssignee, "DefaultAssignee");;
        writer.EndGroup();
    }

    public override void RestoreAttributes(Dictionary<string, object> attributes)
    {
        Dictionary<string, object> dataIssueReceiver = (Dictionary<string, object>)attributes["dataIssueProvider"];
        domain = (string)dataIssueReceiver["Domain"];
        email = (string)dataIssueReceiver["EMail"];
        projekt = (string)dataIssueReceiver["ProjectName"];
        token = (string)dataIssueReceiver["Token"];
        filterQueryStr = (string)dataIssueReceiver["FilterQueryStr"];
        defaultAssignee = (string)dataIssueReceiver["DefaultAssignee"];
    }

    public override async Task<bool> createIssue(Dictionary<string, string> attributes)
    {
       // attributes = getCreateIssueAttributes();

        Debug.Log($"Titel{attributes["Title"]}");
        string jiraDomain = $"{domain}.atlassian.net";
      
        string url = $"https://{jiraDomain}/rest/api/3/issue"; //search?jql=project={ProjektKey}


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
         Dictionary<string, object> fields = new Dictionary<string, object>
        {
            { "project", new Dictionary<string, string> { { "key", "KAN" } } },

            { "summary",  $"{attributes["Title"]}" },
            { "issuetype", new Dictionary<string, string> { { "name", "Task" } } },
            { "description", descriptionDict }
        };
        if (!defaultAssignee.Equals(""))
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
        // Es konnte kein Issue upgedatet werden!
        return false;
    }
    public bool downloadDone = false;
    private async Task restAPI(Settings settings)
    {
        int total = 1;
        int startAT = 1;
        //preUrl = $"{domain}.atlassian.net/rest/api/3/search?jql={projekt}"
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
                Debug.Log(request.result);
                //Rückgabe
                Debug.Log(request.downloadHandler.text);
            }



       

                //DeserializeObject der Json response
                //  JsonConvert.DeserializeObject(request.downloadHandler.text);

            Dictionary<string, System.Object> dic = JsonConvert.DeserializeObject<Dictionary<string, System.Object>>(request.downloadHandler.text);

            JArray issueIDArray = (JArray)dic["issues"];
            bool isLast = (bool)dic["isLast"];
            Debug.Log($"issueIDArray:{issueIDArray.Count}");
            if (!isLast)
                total++;
            Debug.Log($"isLast:{isLast}");

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


                            //string issueJson = issueRequest.downloadHandler.text;
                            if (issuesJ == null)
                                {
                                    issuesJ = new JArray();
                                    issuesJ.Add(JObject.Parse(issueRequest.downloadHandler.text));
                                }                             
                                else
                                {
                                    // JArray an JArray anhängen
                                    JObject issueObj = JObject.Parse(issueRequest.downloadHandler.text);
                                    issuesJ.Add(issueObj);

                                }
                        }
                        else
                        {
                            //  Debug.LogWarning($"Fehler bei Issue {issueID}: {issueReq.error}");
                        }
                    }

                }
            }
            startAT++;


        }
    

    }
    public async Task<JArray> getIssues(Settings settings)
    {
        issuesJ = null;
        issues = null;
        downloadDone = false;
        await restAPI(settings);
        return issuesJ;
    }

}
