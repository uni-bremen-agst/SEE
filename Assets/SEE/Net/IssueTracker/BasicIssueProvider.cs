using MathNet.Numerics.RootFinding;
using Newtonsoft.Json.Linq;
using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
[Serializable]
public abstract class BasicIssueProvider : IssueReceiverInterface
{
    [SerializeField]
    public abstract IssueReceiverInterface.IssueProvider Type { get; }

    protected SEECity City;
    [SerializeField]
    internal String token;
    [SerializeField]
    internal String projekt;
    [SerializeField]
    internal string filterQueryStr;
    [SerializeField]
    protected string preUrl;
    [SerializeField]
    internal string defaultAssignee;
    protected BasicIssueProvider(SEECity city)
    {
        City = city;
    }

    [OdinSerialize, ShowInInspector, ReadOnly]
    [ListDrawerSettings(DefaultExpandedState = true)]
    internal JArray issuesJ=null;
    [OdinSerialize, ShowInInspector]
    internal IssueReceiverInterface.Settings settings;

    /// <summary>
    /// Label of attribute <see cref="dataIssueProvider"/> in the configuration file.
    /// Return the right IssueProvider and create a new instance
    /// </summary>
    public BasicIssueProvider getProvider(Dictionary<string, object> attributes)
    {

     
        Dictionary<string, object> dataIssueReceiver = (Dictionary<string, object>)attributes["dataIssueProvider"];
    switch( (string)dataIssueReceiver["Type"] )
            {
            case nameof(IssueReceiverInterface.IssueProvider.GitLabIssueProvider):
                return new GitLabIssueProvider(this.City);

            case nameof(IssueReceiverInterface.IssueProvider.GitHubIssueProvider):
                return new GitHubIssueProvider(this.City);

            case nameof(IssueReceiverInterface.IssueProvider.JiraIssueProvider):
                return new JiraIssueProvider(this.City);

            default:
               return new GitHubIssueProvider(this.City);
        }
    }

     Task<bool>  IssueReceiverInterface.createIssue(Dictionary<string, string> attributes)
    {
       return createIssue(attributes);
    }
   
  public virtual Dictionary<string, string> getCreateIssueAttributes() // IssueReceiverInterface.
    {
       throw new System.NotImplementedException();
    }

    Task<JArray> IssueReceiverInterface.getIssues(IssueReceiverInterface.Settings settings)
    {
        throw new System.NotImplementedException();
    }

    void IssueReceiverInterface.RestoreAttributes(Dictionary<string, object> attributes)
    {
        this.RestoreAttributes(attributes);
    }
    public virtual void RestoreAttributes(Dictionary<string, object> attributes)
    {
    }
    public virtual void SaveInternal(ConfigWriter writer, string label)
    { 
    }
    public virtual async Task<bool> createIssue(Dictionary<string, string> attributes)//string token, string owner
    {
        throw new System.NotImplementedException();
    }


    void IssueReceiverInterface.Save(ConfigWriter writer, string label)
    {
        SaveInternal( writer, label);
    }



    void IssueReceiverInterface.SaveAttributes(ConfigWriter writer)
    {
        throw new System.NotImplementedException();
    }

    Task<bool> IssueReceiverInterface.updateIssue()
    {
        throw new System.NotImplementedException();
    }
}
