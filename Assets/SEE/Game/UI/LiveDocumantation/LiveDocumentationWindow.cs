using System;
using System.Collections.Generic;
using SEE.Game.UI.Window;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.UI.LiveDocumantation
{
    /// <summary>
    /// This class represents a LiveDocumentation window.
    ///
    /// In this window the Name of the Class, the documentation and all class methods are shown.
    /// </summary>
    public class LiveDocumentationWindow : BaseWindow
    {
        /// <summary>
        /// Path to the prefab
        /// </summary>
        private const string PREFAB_NAME = "Prefabs/UI/LiveDocumentation/LiveDocumentation";

        private const string ClassDocumentationPath = "Content/ClassDocumentation/Viewport/Content/ClassDoc";
        private const string ClassMemberListPath = "Content/ClassMembers/Scroll Area/List";

        /// <summary>
        /// Text mesh for the (shortened) class name
        /// </summary>
        private TextMeshProUGUI ClassNameField;

        private GameObject ClassMembersList;

        /// <summary>
        /// Text mesh for the class documentation
        ///
        /// The Path inside the prefab should be "LiveDocumentation/ClassDocumentation/Viewport/Content/ClassDoc"
        /// </summary>
        private TextMeshProUGUI ClassDocumentation;


        public Camera Camera;


        /// <summary>
        /// The name of the class
        /// </summary>
        public string ClassName { get; set; }


        /// <summary>
        /// The <see cref="LiveDocumentationBuffer"/> which contains the documentation of the class including links
        /// </summary>
        public LiveDocumentationBuffer DocumentationBuffer { get; set; }

        public IList<LiveDocumentationBuffer> ClassMembers { get; set; }


        /// <summary>
        /// Checks if all the necessary fields are set in the class
        /// </summary>
        /// <returns>Returns true when all fields are set. Otherwise false</returns>
        private bool CheckNecessaryFields() => ClassName != null;

        private void AddClassMember(LiveDocumentationBuffer buffer)
        {
            GameObject classMem = new GameObject();
            classMem.name = "Item";
            classMem.AddComponent<CanvasRenderer>();
            RectTransform rt = classMem.AddComponent<RectTransform>();
            ClassMember cm = classMem.AddComponent<ClassMember>();
            cm.Text = buffer.PrintBuffer();


            classMem.transform.parent = ClassMembersList.transform;
            rt.localScale = new Vector3(1, 1, 1);
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
        }

        protected override void StartDesktop()
        {
            if (!CheckNecessaryFields())
            {
                Debug.LogError("Some fields are not set; cant load LiveDocumentation");
                return;
            }

            base.StartDesktop();
            var c = Canvas.GetComponent<Camera>();
            GameObject livedoc =
                PrefabInstantiator.InstantiatePrefab(PREFAB_NAME, Window.transform.Find("Content"), false);
            livedoc.name = "LiveDocumentation";


            Window.transform.Find("Dragger/Title").gameObject.GetComponent<TextMeshProUGUI>().text =
                "LiveDocumentation";


            // Initializing Unity Components 
            ClassNameField = livedoc.transform.Find("Content/ClassName").gameObject
                .GetComponent<TextMeshProUGUI>();

            ClassDocumentation =
                livedoc.transform.Find(ClassDocumentationPath).gameObject.GetComponent<TextMeshProUGUI>();


            ClassMembersList = livedoc.transform.Find(ClassMemberListPath).gameObject;

            // Setting the classname.
            ClassNameField.text = ClassName;
            ClassDocumentation.text = DocumentationBuffer.PrintBuffer();


            //     GameObject livedoc =
            //         PrefabInstantiator.InstantiatePrefab("Prefabs/UI/LiveDocumentation/ClassMember", ClassMembers.transform, false);

            ClassDocumentation.ForceMeshUpdate();

            foreach (var item in ClassMembers)
            {
                AddClassMember(item);
            }
        }


        public override void RebuildLayout()
        {
            // Nothing to be done yet
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            throw new NotImplementedException();
        }

        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            throw new NotImplementedException();
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

        protected override void UpdateDesktop()
        {
            // When the user clicked in the LiveDocumentation window
            if (Input.GetMouseButtonDown(0))
            {
                int link = TMP_TextUtilities.FindIntersectingLink(ClassDocumentation, Input.mousePosition, null);

                if (link != -1)
                {
                    string linkId = ClassDocumentation.textInfo.linkInfo[link].GetLinkID()[0].ToString();
                }
            }
        }
    }
}