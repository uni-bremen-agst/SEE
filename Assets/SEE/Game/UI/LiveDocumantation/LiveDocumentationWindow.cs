using SEE.Game.UI.Window;
using SEE.Utils;
using TMPro;
using UnityEngine;

namespace SEE.Game.UI.LiveDocumantation
{
    public class LiveDocumentationWindow : BaseWindow
    {
        /// <summary>
        /// Path to the prefab
        /// </summary>
        private const string PREFAB_NAME = "Prefabs/UI/LiveDocumentation/LiveDocumentation";

        /// <summary>
        /// Text mesh for the (shortened) class name
        /// </summary>
        private TextMeshProUGUI ClassNameField;

        
        public string ClassName { get; set; }

        protected override void StartDesktop()
        {
            base.StartDesktop();
            GameObject scrollable =
                PrefabInstantiator.InstantiatePrefab(PREFAB_NAME, Window.transform.Find("Content"), false);
        }

        public override void RebuildLayout()
        {
            //throw new System.NotImplementedException();
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            throw new System.NotImplementedException();
        }

        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            throw new System.NotImplementedException();
        }

        public override WindowValues ToValueObject()
        {
            return new LiveDocumentationValues("Live doc", gameObject.name);
        }

        public class LiveDocumentationValues : WindowValues
        {
            public LiveDocumentationValues(string title, string attachedTo = null) : base(title, attachedTo)
            {
            }
        }
    }
}