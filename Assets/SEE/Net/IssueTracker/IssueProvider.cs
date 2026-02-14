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
        [SerializeField, Tooltip("Typ des Issue Providers")]
        [TabGroup("Issues"), RuntimeTab("Issues")]
        public IssueReceiverInterface.IssueProvider IssueProviderType = IssueReceiverInterface.IssueProvider.GitHubIssueReceiver;

        [SerializeField, Tooltip("Name des Issue Providers")]
        [TabGroup("Issues"), RuntimeTab("Issues")]
        public string IssueProviderName = "TestType";
    }
    [Serializable]
    public class IssueProvider// : MonoBehaviour
    {
        public IssueProvider(SEECity city)
        { 
        
        }
        //[SerializeField]
        //[Tooltip("Typ des Issue Providers")]
        //public IssueReceiverInterface.IssueProvider IssueProviderType = IssueReceiverInterface.IssueProvider.GitHubIssueReceiver;

        //[SerializeField]
        //[Tooltip("Name des Issue Providers")]
        //public string IssueProviderName = "TestType";
        //[Header("Issues")]

        ////[OdinSerialize, ShowInInspector]
        ////[TabGroup("Issues"), RuntimeTab("Issues")]
        ////public string Type = "TestType";

        //[SerializeField]
        //public string IssueProviderName = "TestType";
        //[OdinSerialize, ShowInInspector]
        //[TabGroup("Issues"), RuntimeTab("Issues")]
        //[SerializeField, Tooltip("Der Typ des Issue Providers")]
        //  public IssueReceiverInterface.IssueProvider IssueProviderType =
        //IssueReceiverInterface.IssueProvider.GitHubIssueReceiver;
        /// <summary>
        /// [OdinSerialize, TabGroup("Issues"), ShowInInspector] // RuntimeTab("Issues"),
        /// </summary>

        // [Tooltip("Typ des Issue Providers")]
        //[SerializeField]
        //public IssueReceiverInterface.IssueProvider IssueProviderType = IssueReceiverInterface.IssueProvider.GitHubIssueReceiver;
        //public IssueReceiverInterface.IssueProvider IssueProviderType;
        //[OdinSerialize]
        //[OdinSerialize, ShowInInspector]
        //[TabGroup("Issues"), RuntimeTab("Issues")]
        //[OdinSerialize]
        //[TabGroup("Issues")]
        //public IssueReceiverInterface.IssueProvider IssueProviderType = IssueReceiverInterface.IssueProvider.GitHubIssueReceiver; // : SerializedMonoBehaviour //: MonoBehaviour
        //                                                                                                                          // [SerializeField, OdinSerialize, ShowInInspector]
        //    //[TabGroup("Issues"), RuntimeTab("Issues")]
        //    [ShowInInspector, ReadOnly]
        //[TabGroup("Issues"), RuntimeTab("Issues")]
        //public IssueReceiverInterface.IssueProvider CurrentProvider
        //    => IssueProviderType;
        //[ShowInInspector, FilePath(AbsolutePath = true)]
        //[OdinSerialize, ShowInInspector]
        //[TabGroup("Issues"), RuntimeTab("Issues")]
        //public string Type = "TestType";
        //[OdinSerialize, ShowInInspector]
        //[TabGroup("Issues"), RuntimeTab("Issues")]
        //public IssueReceiverInterface.IssueProvider IssueProviderType = IssueReceiverInterface.IssueProvider.GitHubIssueReceiver;
        [OdinSerialize, ShowInInspector]
        [TabGroup("Issues"), RuntimeTab("Issues")]
        private BasicIssueProvider provider = null;//= new GitHubIssueReceiver();

       // public BasicIssueProvider Provider => provider;
        public BasicIssueProvider Provider
        {
            get => provider;
            set
            {
                provider = value;
             } 
        }
        
        // BasicIssueProvider provider;

                //// Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if (provider == null)
            {

               // provider = new GitHubIssueReceiver();
            }
        }
        //IssueProvider new ()
        //    {


        //    }


        //// Update is called once per frame
        //void Update()
        //{

        //}
    }
}