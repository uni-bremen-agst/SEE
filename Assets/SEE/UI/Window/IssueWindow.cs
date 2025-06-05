
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Utils;
using SEE.GO;
using TMPro;
using UnityEngine;
using static IssueReceiverInterface;
using System.Collections.Immutable;
using System.Collections.Generic;
using System;
using SEE.UI.Notification;

namespace SEE.UI.Window
{
    /// <summary>
    /// Represents a movable, scrollable window containing metrics of a <see cref="issueList"/>.

    /// </summary>
    public class IssueWindow : BaseWindow
    {
        /// <summary>
        /// GraphElement whose properties are to be shown.
        /// </summary>
        public GraphElement GraphElement;
        private const string issueWindowPrefab = "Prefabs/UI/IssueWindow";

        private static string WindowPrefab => UIPrefabFolder + "Window";

        private static string ItemPrefab => UIPrefabFolder + "IssueRowLine";
       // private static string ItemPrefab => UIPrefabFolder + "IssueRowLine";
        public List<RootIssue> issueList;
        public override void RebuildLayout()
        {
            //
        }
        protected override void StartDesktop()
        {
            ShowNotification.Error("Show Notification Issue StartDesktop.", "Notify", 10, true);
            base.StartDesktop();
            CreateUIInstance();
        }
        public void CreateUIInstance()
        {
            ShowNotification.Error("Show Notification Issue CreateUIInstance.", "Notify", 10, true);
            GameObject issueWindow = PrefabInstantiator.InstantiatePrefab(issueWindowPrefab, Window.transform.Find("Content"), false);
            issueWindow.name = "Issue Window Show";

            //Transform scrollViewContent = issueWindow.transform.Find("Content/Items").transform;
            TMP_InputField searchField = issueWindow.transform.Find("Search/SearchField").gameObject.MustGetComponent<TMP_InputField>();
          
            searchField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
            searchField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);
            searchField.text = "testadsda";
            Attribute(issueWindow).text = "test";
            //// Int Attributes
            ///

            issueList = new List<RootIssue>();
            DisplayAttributes(issueList, issueWindow);
          
            //// Float Attributes
            //DisplayAttributes(GraphElement.FloatAttributes, metricWindow);
            GameObject issueRow2 = PrefabInstantiator.InstantiatePrefab(issueWindowPrefab, searchField.transform, false);
            // Attribute Name
            Attribute(issueRow2).text = "Search feld";
            // Create mapping of attribute names onto gameObjects representing the corresponding metric row.
           // Dictionary<string, GameObject> activeElements = new();
            //foreach (Transform child in scrollViewContent)
            //{
            // //   activeElements.Add(AttributeName(child.gameObject), child.gameObject);
            //}

         // searchField.onValueChanged.AddListener(searchQuery => ActivateMatches(searchQuery, activeElements));
        }



   
        private static void DisplayAttributes( List<RootIssue> issues, GameObject issueWindowObject)
        {
            Transform scrollViewContent = issueWindowObject.transform.Find("Content/Items").transform;
            
            foreach (RootIssue issue in issues)
            {
                // Create GameObject
                GameObject issueRow = PrefabInstantiator.InstantiatePrefab(ItemPrefab, scrollViewContent, false);
                // Attribute Name
                Attribute(issueRow).text = issue.title;
                // Value Name
            //   Value(issueRow).text = value.ToString();
            }
            GameObject issueRow2 = PrefabInstantiator.InstantiatePrefab(ItemPrefab, scrollViewContent, false);
            // Attribute Name
            Attribute(issueRow2).text = "Test 1234";
        }
        private static TextMeshProUGUI Attribute(GameObject issueRow)
        {
            return issueRow.transform.GetChild(0).gameObject.MustGetComponent<TextMeshProUGUI>();
        }
        public override WindowValues ToValueObject()
        {
            throw new NotImplementedException();
        }

        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            throw new NotImplementedException();
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            throw new NotImplementedException();
        }
   
    }
}
