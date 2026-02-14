using Newtonsoft.Json.Linq;
using SEE.GraphProviders;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;


//namespace SEE.Game

//{

    public interface IssueReceiverInterface
{
       [Serializable]
        enum IssueProvider
        {
            None,
            GitHubIssueReceiver,
            GitLabIssueReceiver,
            JiraIssueReceiver
        }
        // Interface Class 

        [Serializable]
    public class Settings
    {
        public String eMail { get; set; }
        public string preUrl { get; set; }
        public string searchUrl { get; set; }
        public string auth { get; set; }
        public string commentAttributeName { get; set; }
    };
    public class RootIssue
    {
        //public string titel { get; set; }
        //public DateTime created { get; set; }
        //public string summy { get; set; }
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
        //   public User user { get; set; }
        // public Label[] labels { get; set; }
        public string state { get; set; }
        //  public bool locked { get; set; }
        //  public Assignee assignee { get; set; }
        //  public Assignee1[] assignees { get; set; }
        public object milestone { get; set; }
        public int comments { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public DateTime? closed_at { get; set; }
        public string author_association { get; set; }
        public object active_lock_reason { get; set; }
        public string body { get; set; }
        //  public Closed_By closed_by { get; set; }
        //   public Reactions reactions { get; set; }
        public string timeline_url { get; set; }
        public object performed_via_github_app { get; set; }
        public string state_reason { get; set; }
        public bool draft { get; set; }

    }
    public void Save(ConfigWriter writer, String label);
    public void SaveAttributes(ConfigWriter writer);
    public Dictionary<string, string> getCreateIssueAttributes();
    public void RestoreAttributes(Dictionary<string, object> attributes);

    //Eigene implementation für jede Issue-Tracker
    // Diese Funktion muss Implementiert werden!
    public Task<JArray> getIssues(Settings settings);

    public Task<bool> createIssue(Dictionary<string, string> attributes);

    public Task<bool> updateIssue();






}
//}