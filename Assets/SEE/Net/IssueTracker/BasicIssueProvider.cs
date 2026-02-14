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
public abstract class BasicIssueProvider : IssueReceiverInterface //ScriptableObject, 
{
    [SerializeField]
    public abstract IssueReceiverInterface.IssueProvider Type { get; }

    protected SEECity City;
    protected String token;
     public String projekt;
    protected string filterQuery;
    protected string preUrl;
    protected BasicIssueProvider(SEECity city)
    {
        City = city;
    }
    //[HideReferenceObjectPicker,
    //   ListDrawerSettings(DefaultExpandedState = true, ListElementLabelName = nameof(Type))]
    // [OdinSerialize, ShowInInspector]
    //[OdinSerialize, ShowInInspector]
    //[TabGroup("Issues"), RuntimeTab("Issues")]
    //[HideReferenceObjectPicker]
    [OdinSerialize, ShowInInspector, ReadOnly]
   // public string Type = "BasicIssueProvider";
   // [OdinSerialize, ShowInInspector]
    [ListDrawerSettings(DefaultExpandedState = true)]
    public JArray issuesJ=null;
    [OdinSerialize, ShowInInspector]
    public IssueReceiverInterface.Settings settings;

    [Button]
    public void Ping()
    {
        Debug.Log("Ping");
    }
  

    Task<bool> IssueReceiverInterface.createIssue(Dictionary<string, string> attributes)
    {
        throw new System.NotImplementedException();
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
    public virtual async  Task<bool> createIssue(Dictionary<string, string> attributes)//string token, string owner
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
