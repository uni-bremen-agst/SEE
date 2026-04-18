using SEE.UI.RuntimeConfigMenu;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;
namespace SEE.Game.City
{
    [Serializable]
    public class SEECityData
    {
      //  [SerializeField, Tooltip("Typ des Issue Providers")]
       // [TabGroup("Issues"), RuntimeTab("Issues")]
        //public IssueReceiverInterface.IssueProvider IssueProviderType = IssueReceiverInterface.IssueProvider.GitHubIssueProvider;

        [SerializeField, Tooltip("Name des Issue Providers")]
        [TabGroup("Issues"), RuntimeTab("Issues")]
        public string IssueProviderName = "";
    }
    [Serializable]
    public class IssueProvider 
    {
        [OdinSerialize, ShowInInspector]
        [TabGroup("Issues"), RuntimeTab("Issues")]
        private BasicIssueProvider provider = null;

        public BasicIssueProvider Provider
        {
            get => provider;
            set
            {
                provider = value;
             } 
        }
    
       
    }
}