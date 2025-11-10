using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static IssueReceiverInterface;
public class GitHubIssueReceiver : IssueReceiverInterface
{

    public List<RootIssue> issues;
    public JArray issuesJ;
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


    public async Task<bool> createIssue()
    {

        string token = "YOUR-TOKEN";
        string owner = "lkuenzel";
        string repo = "IssueTrackerRepository";

        var issueData = new
        {
            title = "Found a bug",
            body = "I'm having a problem with this.",
            assignees = new[] { "User1" },
            milestone = 1,
            labels = new[] { "Report" }
        };

        using (var client = new HttpClient())
        {
            // GitHub API base URL
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("UnityApp/1.0");
            // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

            string json = JsonConvert.SerializeObject(issueData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"repos/{owner}/{repo}/issues", content);
            Debug.Log("Response:\n" + response);
            Console.WriteLine("Response:\n" + response);
            string result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Issue created successfully:\n" + responseBody);
            }
            else
            {

                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error:{response.StatusCode}\n{error}");
                return false;
            }
        }

        return true;
    }
    public async Task<bool> updateIssue()
    {
        return false;
    }
    private async Task restAPI(Settings settings)
    {
        int maxPage = 0;
        int currentPage = -1;
        string pageingStr = "";
        while (maxPage > currentPage)
        {
            // string requestUrl = "https://api.github.com/repos/uni-bremen-agst/SEE/issues?state=all&per_page=50&Page=0;rel=last";// "https://api.github.com/repos/koschke/uni-bremen-agst/SEE/issues"; //"";
            if (currentPage == -1)
            {
                pageingStr = $"&per_page=50&Page={0.ToString()}";
            }
            else
            {
                pageingStr = $"&per_page=50&Page={currentPage.ToString()}";
            }

            Debug.Log($"IssueLogURL: {settings.preUrl + settings.searchUrl}");
            UnityWebRequest request = UnityWebRequest.Get($"{settings.preUrl}{settings.searchUrl}");

            request.SetRequestHeader("Accept", "application/json");
            //  request.SetRequestHeader("Authorization", $"AxToken {pke}");



#pragma warning disable CS4014
            request.SendWebRequest();
#pragma warning restore CS4014
            await UniTask.WaitUntil(() => request.isDone);

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("IssueSuccess");
                Debug.Log(request.result);
                //Rückgabe
                Debug.Log(request.downloadHandler.text);
            }
            else
            {
                Debug.Log($"Result not sucessfull {request.result}");
                return;
            }
            Debug.Log(request.downloadHandler.text.StartsWith("["));
            // issuesJ = JsonConvert.DeserializeObject(request.downloadHandler.text);
            issuesJ = JArray.Parse(request.downloadHandler.text);

            //Debug.Log("Anzahl Issues: " + issuesJ.Count);

            //foreach (var issue in issuesJ)
            //{
            //    Debug.Log(" Issue: " + issue["title"]);
            //}
            return;

            if (maxPage == 0)
            {
                // Split des Linkes

                string temp = null;
                if (request.GetResponseHeader("Link").Split().Count() >= 3)
                    temp = request.GetResponseHeader("Link").Split()[2];

                //index bevor sonderzeichen (">;")
                int index = temp.Count() - 3;
                //Dekrementiert den index bis dieser auf die erste Ziffer von der Maximalen Page Anzahl steht
                while (char.IsNumber(temp[index - 1]))
                {
                    index--;
                }

                maxPage = int.Parse(temp.Substring(index, (temp.Count() - 2) - index));
            }
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            // Write the string array to a new file named "WriteLines.txt".
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "IssueTestOutput.txt")))
            {

                outputFile.Write(request.downloadHandler.text);
            }



            Dictionary<string, System.Object> dic = JsonConvert.DeserializeObject<Dictionary<string, System.Object>>(request.downloadHandler.text);

            //total = Convert.ToInt32(dic["total"]);

            //UnityEngine.Debug.Log($"IssueConvert:{total}");
            // total = (int)dic["total"];

            // UnityEngine.Debug.Log($"Start at: {rootobject.startAt.ToString()}");
            // startAT = Convert.ToInt32(dic["startAt"]) + Convert.ToInt32(dic["maxResults"]);// rootobject.startAt + rootobject.maxResults;
            //total = rootobject.total;
            //// -1 da die 0 mit zählt
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
            currentPage += 50;
        }


    }
    public async Task<JArray> getIssues(Settings settings)
    {
        //ResetIssues
        issues = null;

        await restAPI(settings);
        return issuesJ;
    }


}
