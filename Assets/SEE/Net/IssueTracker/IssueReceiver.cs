using Antlr4.Runtime.Misc;
using Cysharp.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;


using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static IssueReceiver;
using static IssueReceiverInterface;
public class IssueReceiver : MonoBehaviour 
{


    // [EnvironmentVariable("GitAPI_P_KEY")]
    // [TextArea]
    //  [Tooltip("The public key for the authority from the Rest API.")]




    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.Debug.Log("Issuetest");

        // GetIssues();

        //IssueReceiverInterface.Settings settings = new IssueReceiverInterface.Settings { preUrl = "https://api.github.com/repos/uni-bremen-agst/SEE/issues", searchUrl = "?filter=all" };
        //GitHubIssueReceiver gitHUbReceiver = new GitHubIssueReceiver();
        //gitHUbReceiver.getIssues(settings);

        //  GetIssuesJira("korusatix@gmail.com");
        IssueReceiverInterface.Settings settings = new IssueReceiverInterface.Settings { preUrl = "https://ecosystem.atlassian.net/rest/api/3/search?jql=", searchUrl = "project=CACHE" };
        JiraIssueReceiver jiraReceiver = new JiraIssueReceiver();
        jiraReceiver.getIssues(settings);

    }

    // Update is called once per frame
    void Update()
    {

    }
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
    // Jira Classes Issue   

    #region "Issue Klassen Jira"
    public class Rootobject
    {
        public string expand { get; set; }
        public int startAt { get; set; }
        public int maxResults { get; set; }
        public int total { get; set; }
        public IssueJ[] issues { get; set; }
    }

    public class IssueJ
    {
        public string expand { get; set; }
        public string id { get; set; }
        public string self { get; set; }
        public string key { get; set; }
        public Fields fields { get; set; }
    }

    public class Fields
    {

        public Resolution resolution { get; set; }

        public object lastViewed { get; set; }
        public int? aggregatetimeoriginalestimate { get; set; }
        public Issuelink[] issuelinks { get; set; }
        public AssigneeJ assignee { get; set; }

        public Subtask[] subtasks { get; set; }

        public Votes votes { get; set; }

        public Issuetype issuetype { get; set; }

        public string environment { get; set; }
        public object duedate { get; set; }
        public object[] customfield_13980 { get; set; }
        public int? timeestimate { get; set; }
        public Status status { get; set; }
        public int? aggregatetimeestimate { get; set; }
        public Creator creator { get; set; }
        public object timespent { get; set; }
        public object aggregatetimespent { get; set; }
        public int workratio { get; set; }

        public string[] labels { get; set; }
        public object[] components { get; set; }


        public Reporter reporter { get; set; }

        public Progress progress { get; set; }

        public Project project { get; set; }

        public DateTime? resolutiondate { get; set; }
        public Watches watches { get; set; }

        public DateTime updated { get; set; }
        public int? timeoriginalestimate { get; set; }
        public string description { get; set; }
        public string summary { get; set; }
        public DateTime statuscategorychangedate { get; set; }
        public Fixversion[] fixVersions { get; set; }

        public Priority priority { get; set; }
        public object[] versions { get; set; }
        public Aggregateprogress aggregateprogress { get; set; }
        public DateTime created { get; set; }
        public Security security { get; set; }
        public Parent parent { get; set; }
        public string customfield_13580 { get; set; }
    }

    public class Resolution
    {
        public string self { get; set; }
        public string id { get; set; }
        public string description { get; set; }
        public string name { get; set; }
    }

    public class Customfield_19433
    {
        public string self { get; set; }
        public string value { get; set; }
        public string id { get; set; }
    }

    public class AssigneeJ
    {
        public string self { get; set; }
        public string accountId { get; set; }
        public Avatarurls avatarUrls { get; set; }
        public string displayName { get; set; }
        public bool active { get; set; }
        public string timeZone { get; set; }
        public string accountType { get; set; }
    }

    public class Avatarurls
    {
        public string _48x48 { get; set; }
        public string _24x24 { get; set; }
        public string _16x16 { get; set; }
        public string _32x32 { get; set; }
    }

    public class Votes
    {
        public string self { get; set; }
        public int votes { get; set; }
        public bool hasVoted { get; set; }
    }

    public class Issuetype
    {
        public string self { get; set; }
        public string id { get; set; }
        public string description { get; set; }
        public string iconUrl { get; set; }
        public string name { get; set; }
        public bool subtask { get; set; }
        public int avatarId { get; set; }
        public int hierarchyLevel { get; set; }
    }

    public class Status
    {
        public string self { get; set; }
        public string description { get; set; }
        public string iconUrl { get; set; }
        public string name { get; set; }
        public string id { get; set; }
        public Statuscategory statusCategory { get; set; }
    }

    public class Statuscategory
    {
        public string self { get; set; }
        public int id { get; set; }
        public string key { get; set; }
        public string colorName { get; set; }
        public string name { get; set; }
    }

    public class Creator
    {
        public string self { get; set; }
        public string accountId { get; set; }
        public Avatarurls1 avatarUrls { get; set; }
        public string displayName { get; set; }
        public bool active { get; set; }
        public string timeZone { get; set; }
        public string accountType { get; set; }
    }

    public class Avatarurls1
    {
        public string _48x48 { get; set; }
        public string _24x24 { get; set; }
        public string _16x16 { get; set; }
        public string _32x32 { get; set; }
    }



    public class Reporter
    {
        public string self { get; set; }
        public string accountId { get; set; }
        public Avatarurls2 avatarUrls { get; set; }
        public string displayName { get; set; }
        public bool active { get; set; }
        public string timeZone { get; set; }
        public string accountType { get; set; }
    }

    public class Avatarurls2
    {
        public string _48x48 { get; set; }
        public string _24x24 { get; set; }
        public string _16x16 { get; set; }
        public string _32x32 { get; set; }
    }

    public class Progress
    {
        public int progress { get; set; }
        public int total { get; set; }
        public int percent { get; set; }
    }

    public class Project
    {
        public string self { get; set; }
        public string id { get; set; }
        public string key { get; set; }
        public string name { get; set; }
        public string projectTypeKey { get; set; }
        public bool simplified { get; set; }
        public Avatarurls3 avatarUrls { get; set; }
        public Projectcategory projectCategory { get; set; }
    }

    public class Avatarurls3
    {
        public string _48x48 { get; set; }
        public string _24x24 { get; set; }
        public string _16x16 { get; set; }
        public string _32x32 { get; set; }
    }

    public class Projectcategory
    {
        public string self { get; set; }
        public string id { get; set; }
        public string description { get; set; }
        public string name { get; set; }
    }

    public class Watches
    {
        public string self { get; set; }
        public int watchCount { get; set; }
        public bool isWatching { get; set; }
    }

    public class Priority
    {
        public string self { get; set; }
        public string iconUrl { get; set; }
        public string name { get; set; }
        public string id { get; set; }
    }

    public class Aggregateprogress
    {
        public int progress { get; set; }
        public int total { get; set; }
        public int percent { get; set; }
    }

    public class Customfield_14580
    {
        public string self { get; set; }
        public string value { get; set; }
        public string id { get; set; }
    }

    public class Security
    {
        public string self { get; set; }
        public string id { get; set; }
        public string description { get; set; }
        public string name { get; set; }
    }

    public class Parent
    {
        public string id { get; set; }
        public string key { get; set; }
        public string self { get; set; }
        public Fields1 fields { get; set; }
    }

    public class Fields1
    {
        public string summary { get; set; }
        public Status1 status { get; set; }
        public Priority1 priority { get; set; }
        public Issuetype1 issuetype { get; set; }
    }

    public class Status1
    {
        public string self { get; set; }
        public string description { get; set; }
        public string iconUrl { get; set; }
        public string name { get; set; }
        public string id { get; set; }
        public Statuscategory1 statusCategory { get; set; }
    }

    public class Statuscategory1
    {
        public string self { get; set; }
        public int id { get; set; }
        public string key { get; set; }
        public string colorName { get; set; }
        public string name { get; set; }
    }

    public class Priority1
    {
        public string self { get; set; }
        public string iconUrl { get; set; }
        public string name { get; set; }
        public string id { get; set; }
    }

    public class Issuetype1
    {
        public string self { get; set; }
        public string id { get; set; }
        public string description { get; set; }
        public string iconUrl { get; set; }
        public string name { get; set; }
        public bool subtask { get; set; }
        public int avatarId { get; set; }
        public int hierarchyLevel { get; set; }
    }

    public class Issuelink
    {
        public string id { get; set; }
        public string self { get; set; }
        public Type type { get; set; }
        public Inwardissue inwardIssue { get; set; }
        public Outwardissue outwardIssue { get; set; }
    }

    public class Type
    {
        public string id { get; set; }
        public string name { get; set; }
        public string inward { get; set; }
        public string outward { get; set; }
        public string self { get; set; }
    }

    public class Inwardissue
    {
        public string id { get; set; }
        public string key { get; set; }
        public string self { get; set; }
        public Fields2 fields { get; set; }
    }

    public class Fields2
    {
        public string summary { get; set; }
        public Status2 status { get; set; }
        public Priority2 priority { get; set; }
        public Issuetype2 issuetype { get; set; }
    }

    public class Status2
    {
        public string self { get; set; }
        public string description { get; set; }
        public string iconUrl { get; set; }
        public string name { get; set; }
        public string id { get; set; }
        public Statuscategory2 statusCategory { get; set; }
    }

    public class Statuscategory2
    {
        public string self { get; set; }
        public int id { get; set; }
        public string key { get; set; }
        public string colorName { get; set; }
        public string name { get; set; }
    }

    public class Priority2
    {
        public string self { get; set; }
        public string iconUrl { get; set; }
        public string name { get; set; }
        public string id { get; set; }
    }

    public class Issuetype2
    {
        public string self { get; set; }
        public string id { get; set; }
        public string description { get; set; }
        public string iconUrl { get; set; }
        public string name { get; set; }
        public bool subtask { get; set; }
        public int avatarId { get; set; }
        public int hierarchyLevel { get; set; }
    }

    public class Outwardissue
    {
        public string id { get; set; }
        public string key { get; set; }
        public string self { get; set; }
        public Fields3 fields { get; set; }
    }

    public class Fields3
    {
        public string summary { get; set; }
        public Status3 status { get; set; }
        public Priority3 priority { get; set; }
        public Issuetype3 issuetype { get; set; }
    }

    public class Status3
    {
        public string self { get; set; }
        public string description { get; set; }
        public string iconUrl { get; set; }
        public string name { get; set; }
        public string id { get; set; }
        public Statuscategory3 statusCategory { get; set; }
    }

    public class Statuscategory3
    {
        public string self { get; set; }
        public int id { get; set; }
        public string key { get; set; }
        public string colorName { get; set; }
        public string name { get; set; }
    }

    public class Priority3
    {
        public string self { get; set; }
        public string iconUrl { get; set; }
        public string name { get; set; }
        public string id { get; set; }
    }

    public class Issuetype3
    {
        public string self { get; set; }
        public string id { get; set; }
        public string description { get; set; }
        public string iconUrl { get; set; }
        public string name { get; set; }
        public bool subtask { get; set; }
        public int avatarId { get; set; }
        public int hierarchyLevel { get; set; }
    }

    public class Subtask
    {
        public string id { get; set; }
        public string key { get; set; }
        public string self { get; set; }
        public Fields4 fields { get; set; }
    }

    public class Fields4
    {
        public string summary { get; set; }
        public Status4 status { get; set; }
        public Priority4 priority { get; set; }
        public Issuetype4 issuetype { get; set; }
    }

    public class Status4
    {
        public string self { get; set; }
        public string description { get; set; }
        public string iconUrl { get; set; }
        public string name { get; set; }
        public string id { get; set; }
        public Statuscategory4 statusCategory { get; set; }
    }

    public class Statuscategory4
    {
        public string self { get; set; }
        public int id { get; set; }
        public string key { get; set; }
        public string colorName { get; set; }
        public string name { get; set; }
    }

    public class Priority4
    {
        public string self { get; set; }
        public string iconUrl { get; set; }
        public string name { get; set; }
        public string id { get; set; }
    }

    public class Issuetype4
    {
        public string self { get; set; }
        public string id { get; set; }
        public string description { get; set; }
        public string iconUrl { get; set; }
        public string name { get; set; }
        public bool subtask { get; set; }
        public int avatarId { get; set; }
        public int hierarchyLevel { get; set; }
    }

    public class Fixversion
    {
        public string self { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public bool archived { get; set; }
        public bool released { get; set; }
        public string description { get; set; }
        public string releaseDate { get; set; }
    }
    #endregion


        public class Settings {
        public String eMail { get; set; }
        public string preUrl { get; set; }
        public string searchUrl { get; set; }
        public string auth { get; set; }
       // public GenericClass genericClass { get; set; }



    };

    private async void getIssueTracker(Settings settings)
    {
        //JQL (Jira Query Language)
        //Gibt alle Issues zurück
        //  "https://ecosystem.atlassian.net/rest/api/2/search?jql=";

        UnityWebRequest request = UnityWebRequest.Get(settings.preUrl+settings.searchUrl);
        request.SetRequestHeader("Accept", "application/json");

    #pragma warning disable CS4014
            request.SendWebRequest();
    #pragma warning restore CS4014
            await UniTask.WaitUntil(() => request.isDone);
        
        var rootobject = JsonConvert.DeserializeObject(request.downloadHandler.text);




       // rootobject
       // var rootobject = JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
    }


    private async void GetIssuesJira(string email)
    {
        string requestUrl = "https://ecosystem.atlassian.net/jira/software/c/projects/CACHE/issues/?jql="; //"https://ecosystem.atlassian.net/rest/api/2/search?jql=";//"https://korusatix.atlassian.net/rest/api/2/search?jql=";// "https://korusatix.atlassian.net/rest/api/3/issue";// "https://api.github.com/repos/uni-bremen-agst/SEE/issues?state=all&per_page=2&Page=1;rel=last";// "https://api.github.com/repos/koschke/uni-bremen-agst/SEE/issues"; //"";

        UnityWebRequest request = UnityWebRequest.Get(requestUrl);

        // if (!string.IsNullOrWhiteSpace(PublicKey))
        // {
        // Only set certificate handler if public key is set (i.e. we're using a self-signed certificate)
        //   request.certificateHandler = new AxivionCertificateHandler(PublicKey); 
        // }
        // string accept = "application/json";

        request.SetRequestHeader("Accept", "application/json");
        // request.SetRequestHeader("Authorization", $"Basic {PublicKey}");//Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{PublicKey}")));

#pragma warning disable CS4014
        request.SendWebRequest();
#pragma warning restore CS4014
        await UniTask.WaitUntil(() => request.isDone);
        UnityEngine.Debug.Log("test1start");

        ;
        UnityEngine.Debug.Log(request.result);

        string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

   
        using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "IssueTestOutputJira.txt")))
        {
            outputFile.Write(request.downloadHandler.text);
        }
        UnityEngine.Debug.Log(request.downloadHandler.text);
        UnityEngine.Debug.Log("errorXJira");
        UnityEngine.Debug.Log(request.error);
        UnityEngine.Debug.Log("errorx");
        UnityEngine.Debug.Log("test1end");


        // DeserializeObject des Json Objects um als Klasse auf die Parameter zuzugreifen
        Rootobject rootobject = JsonConvert.DeserializeObject<Rootobject>(request.downloadHandler.text);



        int index = 0;
        // Iteriert über die Issue Liste und printet den Namen.
        foreach (IssueJ issue in rootobject.issues)
        {
            UnityEngine.Debug.Log($"IssueTitle{index.ToString()}:" + issue.key != null ? issue.key : "" + "/n");
            //UnityEngine.Debug.Log("resolution.displayName:" + issue.:"" + "\n"); // 
            index++;
        }
        //byt

    }
    //ret=last übergbit welche Pages als letztes runtergeladen wurden sind. Dies kann benutzt werden um alle Issue Pages Runterzuaden.
    private async void GetIssues()
    {
        string requestUrl = "https://api.github.com/repos/uni-bremen-agst/SEE/issues?state=all&per_page=2&Page=1;rel=last";// "https://api.github.com/repos/koschke/uni-bremen-agst/SEE/issues"; //"";

        UnityWebRequest request = UnityWebRequest.Get(requestUrl);
        // if (!string.IsNullOrWhiteSpace(PublicKey))
        // {
        // Only set certificate handler if public key is set (i.e. we're using a self-signed certificate)
        //   request.certificateHandler = new AxivionCertificateHandler(PublicKey);
        // }
        request.SetRequestHeader("Accept", "application/json");
        //  request.SetRequestHeader("Authorization", $"AxToken {PublicKey}");

#pragma warning disable CS4014
        request.SendWebRequest();
#pragma warning restore CS4014
        await UniTask.WaitUntil(() => request.isDone);
        UnityEngine.Debug.Log("test1startGithub");
        UnityEngine.Debug.Log(request.result);

        
        //foreach (KeyValuePair<string, string> pair in request.GetResponseHeaders())
        //{
        //    UnityEngine.Debug.Log($"{pair.Key}:{pair.Value}");

        //}
        string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // Write the string array to a new file named "WriteLines.txt".
        using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "IssueTestOutput.txt")))
        {

            outputFile.Write(request.downloadHandler.text);
        }
        UnityEngine.Debug.Log(request.downloadHandler.text);
        UnityEngine.Debug.Log("errorX");
        UnityEngine.Debug.Log(request.error);
        UnityEngine.Debug.Log("errorx");
        UnityEngine.Debug.Log("Wrote in response txt");

        UnityEngine.Debug.Log("test222");

        //DeserializeObject der Json response

        List<Issue> issueList = JsonConvert.DeserializeObject<List<Issue>>(request.downloadHandler.text);

        // gibt den Titel aller Issues in der Console aus.
        foreach (Issue issue in issueList)
        {
            UnityEngine.Debug.Log("title:" + issue.title + "/n");
        }

    }
}
