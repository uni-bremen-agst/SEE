using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using Cysharp.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;


using UnityEngine.Networking;
public interface IssueReceiverInterface
{
    // Interface Class 
    public class Settings
    {
        public String eMail { get; set; }
        public string preUrl { get; set; }
        public string searchUrl { get; set; }
        public string auth { get; set; }
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
   

    //Eigene implementation für jede Issue-Tracker
    // Diese Funktion muss Implementiert werden!
    public List<RootIssue> getIssues(Settings settings);

    public bool createIssue();
    public bool updateIssue();
 

}
