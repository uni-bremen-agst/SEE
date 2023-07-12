using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Antlr4.Runtime;
using DG.Tweening;
using JetBrains.Annotations;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game.UI.Notification;
using SEE.Game.UI.Window;
using SEE.GO;
using SEE.Utils;
using SEE.Utils.LiveDocumentation;
using TMPro;
using UnityEngine;

namespace SEE.Game.UI.LiveDocumantation
{
    /// <summary>
    /// This class represents a LiveDocumentation window.
    /// 
    /// In this window the Name of the Class, the documentation and all class methods are shown.
    /// In addition: the <see cref="LiveDocumentationWindow"/> has two modes: 'CLASS' and 'METHOD'.
    ///
    /// 'CLASS' is used for displaying documentation of classes.
    /// 
    /// </summary>
    public class LiveDocumentationWindow : BaseWindow
    {
        #region Constants

        /// <summary>
        /// Path to the prefab
        /// </summary>
        private const string PrefabName = "Prefabs/UI/LiveDocumentation/LiveDocumentation";

        private const string ClassDocumentationPath = "ClassDocumentation/Viewport/Content";
        private const string ClassMemberListPath = "ClassMembers/Scroll Area/List";

        private const string NodeClassType = "Class";

        private const string ClassTypeClassMembersTitleText = "ClassMembers";
        private const string MethodTypeParametersTitleText = "Parameters";

        #endregion

        /// <summary>
        /// Text mesh for the (shortened) class name
        /// </summary>
        private TextMeshProUGUI ClassNameField;

        /// <summary>
        /// GameObject for the ListView containing the class methods (when <see cref="DocumentationWindowType"/> is set to <see cref="LiveDocumentationWindowType.CLASS"/>).
        /// When set to  <see cref="LiveDocumentationWindowType.METHOD"/> the parameters are displayed there.
        /// </summary>
        private GameObject ClassMembersList;

        /// <summary>
        /// Text mesh for the class documentation
        ///
        /// The Path inside the prefab should be "LiveDocumentation/ClassDocumentation/Viewport/Content/ClassDoc"
        /// </summary>
        private TextMeshProUGUI ClassDocumentation;

        /// <summary>
        /// The type of the LiveDocumentation window  
        /// </summary>
        public LiveDocumentationWindowType DocumentationWindowType { get; set; }

        /// <summary>
        /// The name of the class
        /// </su  mmary>
        public string ClassName { get; set; }

        /// <summary>
        /// A list of all imported namespaces/packages
        /// </summary>
        public List<string> ImportedNamespaces { get; set; } = new();


        /// <summary>
        /// The node of the CodeCity where the class or method belongs to.
        /// </summary>
        public Node NodeOfClass;

        /// <summary>
        /// An instance of a <see cref="WindowSpaceManager"/>
        /// </summary>
        private WindowSpaceManager spaceManager;

        /// <summary>
        /// The <see cref="LiveDocumentationBuffer"/> which contains the documentation of the class including links
        /// </summary>
        public LiveDocumentationBuffer DocumentationBuffer { get; set; }

        /// <summary>
        /// A List of buffers which are displayed in the ListView.
        ///
        /// Not that in 'Class-Mode' the type of the elements might differ (<see cref="LiveDocumentationClassMemberBuffer"/>)
        /// </summary>
        public List<LiveDocumentationBuffer> ClassMembers { get; set; } =
            new List<LiveDocumentationBuffer>();


        /// <summary>
        /// Checks if all the necessary fields are set in the class
        /// </summary>
        /// <returns>Returns true when all fields are set. Otherwise false</returns>
        private bool CheckNecessaryFields() =>
            ClassName != null && NodeOfClass != null;

        /// <summary>
        /// Adds a new class member to the ClassMember section in the LiveDocumentation Window.
        ///
        /// Currently ClassMembers are represented by a <see cref="LiveDocumentationBuffer"/>.
        /// In this buffer all information and links of the method signature is stored. 
        /// </summary>
        /// <param name="buffer">The documentation of the class member</param>
        private void AddClassMember(LiveDocumentationBuffer buffer)
        {
            // Creating a new GameObject and naming it
            GameObject classMem = new GameObject();
            classMem.name = "Item";

            // Adding some other Components
            classMem.AddComponent<CanvasRenderer>();
            RectTransform rt = classMem.AddComponent<RectTransform>();

            //Adding the ClassMember component
            ClassMember cm = classMem.AddComponent<ClassMember>();

            cm.MethodsBuffer = buffer;
            if (buffer is LiveDocumentationClassMemberBuffer classMemberBuffer)
            {
                cm.LineNumber = classMemberBuffer.LineNumber;
            }

            // Setting the correct anchor point (upper left corner) for the new game object
            classMem.transform.parent = ClassMembersList.transform;
            rt.localScale = new Vector3(1, 1, 1);
            // This will set the 
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(0, 200);

            cm.OnLinkClicked += OnLinkClicked;
            cm.OnClicked.AddListener(OnClickClassMember);
        }

        /// <summary>
        /// Is called, when the user clicked on a class member.
        /// </summary>
        /// <param name="cm">The <see cref="ClassMember"/> the user has clicked on</param>
        private void OnClickClassMember(ClassMember cm)
        {
            // Clicks should only be handled, when left shift is pressed.
            if (Input.GetKey(KeyCode.LeftShift))
            {
                var method = NodeOfClass.Children().Where(x => x.Type.Equals("Method"))
                    .Where(x => x.GetInt("Source.Line") == cm.LineNumber).First();

                // When the class member is the documentation of a method.
                // In this case a new LiveDocumentation window with the method is opened.
                if (DocumentationWindowType == LiveDocumentationWindowType.CLASS &&
                    cm.MethodsBuffer is LiveDocumentationClassMemberBuffer classMemberBuffer)
                {
                    LiveDocumentationWindow newWin = method.GameObject().AddComponent<LiveDocumentationWindow>();

                    newWin.Title = method.SourceName;
                    string selectedFile = method.Filename();
                    if (!newWin.Title.Replace(".", "").Equals(selectedFile.Split('.').Reverse().Skip(1)
                            .Aggregate("", (acc, s) => s + acc)))
                    {
                        newWin.Title += $" ({selectedFile})";
                    }

                    newWin.ClassName = newWin.Title;
                    newWin.NodeOfClass = method;
                    newWin.DocumentationWindowType = LiveDocumentationWindowType.METHOD;
                    newWin.ImportedNamespaces = ImportedNamespaces;
                    newWin.DocumentationBuffer = classMemberBuffer.Documentation;
                    newWin.ClassMembers = classMemberBuffer.Parameters;

                    List<LiveDocumentationBuffer> methods = new List<LiveDocumentationBuffer>();

                    // Add code window to our space of code window.
                    spaceManager[WindowSpaceManager.LOCAL_PLAYER].AddWindow(newWin);
                    spaceManager[WindowSpaceManager.LOCAL_PLAYER].ActiveWindow = newWin;
                }
            }
        }

        /// <summary>
        /// Is called when the window is opened on a desktop instance of SEE
        /// </summary>
        protected override void StartDesktop()
        {
            spaceManager = WindowSpaceManager.ManagerInstance;
            // Check if all required fields are set.
            if (!CheckNecessaryFields())
            {
                Debug.LogError("Some fields are not set; cant load LiveDocumentation");
                return;
            }

            base.StartDesktop();
            GameObject livedoc =
                PrefabInstantiator.InstantiatePrefab(PrefabName, Window.transform.Find("Content"), false);
            livedoc.name = "LiveDocumentation";
            Window.transform.Find("Dragger/Title").gameObject.GetComponent<TextMeshProUGUI>().text =
                "LiveDocumentation";

            // Initializing Unity Components 
            ClassNameField = livedoc.transform.Find("ClassName/Viewport/Content/ClassName").gameObject
                .GetComponent<TextMeshProUGUI>();
            ClassDocumentation =
                livedoc.transform.Find(ClassDocumentationPath).gameObject.GetComponent<TextMeshProUGUI>();
            ClassMembersList = livedoc.transform.Find(ClassMemberListPath).gameObject;

            // Set the right title 
            GameObject membersListTitle = ClassMembersList.gameObject.transform.Find("Title").gameObject;
            TextMeshProUGUI textField = membersListTitle.GetComponent<TextMeshProUGUI>();
            switch (DocumentationWindowType)
            {
                case LiveDocumentationWindowType.CLASS:
                    textField.text = ClassTypeClassMembersTitleText;
                    break;
                case LiveDocumentationWindowType.METHOD:
                    textField.text = MethodTypeParametersTitleText;
                    break;
            }


            // Setting the classname.
            ClassNameField.text = ClassName;

            // Try setting the actual documentation
            // If the class has no documentation 
            ClassDocumentation.text = DocumentationBuffer?.PrintBuffer() ?? "NO DOCS AVAILABLE";
            if (ClassDocumentation.text == "")
            {
                ClassDocumentation.text = "NO DOCS AVAILABLE";
            }

            ClassDocumentation.ForceMeshUpdate();

            // Adding the class members
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
            return new LiveDocumentationValues(Title, NodeOfClass, DocumentationWindowType, DocumentationBuffer, ClassMembers, gameObject.name);
        }

        [Serializable]
        public class LiveDocumentationValues : WindowValues
        {
            [field: SerializeField] public Node Node { get; set; }

            [field: SerializeField] public LiveDocumentationWindowType Type { get; set; }

            [field: SerializeField] public LiveDocumentationBuffer DocumentationBuffer { get; set; }

            [field: SerializeField] public List<LiveDocumentationBuffer> Members { get; set; }

            public LiveDocumentationValues(string title,  Node node, LiveDocumentationWindowType type, LiveDocumentationBuffer buffer, List<LiveDocumentationBuffer> members,string attachedTo = null) : base(title, attachedTo)
            {
         
                Node = node;
                type = type;
                buffer = buffer;
                members = members;
            }
        }

        private Node TraverseForNamespace(List<string> splitedNamespace, Node currentNode)
        {
            // Base case
            if (splitedNamespace.Count == 1 && currentNode.SourceName.Equals(splitedNamespace.First()))
            {
                return currentNode;
            }

            string nextNamespaceElement = splitedNamespace.FirstOrDefault();
            if (currentNode.SourceName.Equals(nextNamespaceElement))
            {
                foreach (var node in currentNode.Children().Where(x => x.Type.Equals("Namespace")).ToList())
                {
                    if (TraverseForNamespace(splitedNamespace.Skip(1).ToList(), node) is { } found)
                    {
                        return found;
                    }
                }
            }

            return null;
        }

        private Node TraverseForNamespace(string namespaceName) =>
            TraverseForNamespace(namespaceName.Split(".").ToList(),
                NodeOfClass.ItsGraph.Nodes().First(x => x.IsRoot()));


        /// <summary>
        /// This method looks for a Node corresponding to a specific class.
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        [CanBeNull]
        private Node FindNodeOfClass(string className)
        {
            // Iterate over each element in the same namespace
            foreach (var node in NodeOfClass.Parent.Children().Where((x) => x.Type.Equals("Class")))
            {
                if (node.SourceName.Equals(className))
                {
                    return node;
                }
            }

            foreach (var namespaceName in ImportedNamespaces)
            {
                Node traversedNamespace = TraverseForNamespace(namespaceName);
                // Skip all namespaces which don't belong to the CodeCity.
                if (traversedNamespace == null)
                {
                    continue;
                }

                foreach (var node in traversedNamespace.Children().Where((x) => x.Type.Equals("Class")).ToList())
                {
                    if (node.SourceName.Equals(className))
                    {
                        return node;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Called when a user has clicked on a link
        /// </summary>
        /// <param name="linkPath"></param>
        private void OnLinkClicked(string linkPath)
        {
            Node nodeOfLink = FindNodeOfClass(linkPath);
            if (nodeOfLink == null)
            {
                ShowNotification.Error("Cant open link", "The class can't be found");
                return;
            }
            else
            {
                ShowNotification.Info("Link found", nodeOfLink.AbsolutePlatformPath());
            }

            // If the Space manager don't contains a LiveDocumentationWindow of the same file a new one is created
            // Otherwise the old one is set as the active window
            if (!nodeOfLink.GameObject().TryGetComponent(out LiveDocumentationWindow ldocWin))
            {
                LiveDocumentationWindow newWin = nodeOfLink.GameObject().AddComponent<LiveDocumentationWindow>();

                newWin.Title = nodeOfLink.SourceName;
                string selectedFile = nodeOfLink.Filename();
                if (!newWin.Title.Replace(".", "").Equals(selectedFile.Split('.').Reverse().Skip(1)
                        .Aggregate("", (acc, s) => s + acc)))
                {
                    newWin.Title += $" ({selectedFile})";
                }

                newWin.ClassName = newWin.Title;
                newWin.NodeOfClass = nodeOfLink;
                LiveDocumentationBuffer buffer = new LiveDocumentationBuffer();
                FileParser parser = new FileParser(nodeOfLink.AbsolutePlatformPath());
                buffer = parser.ParseClassDoc(nodeOfLink.SourceName);
                newWin.DocumentationBuffer = buffer;

                List<LiveDocumentationBuffer> methods = new List<LiveDocumentationBuffer>();
                parser.ParseClassMethods(nodeOfLink.SourceName)
                    .ForEach((x) => methods.Add(x));

                newWin.ImportedNamespaces = parser.ParseNamespaceImports();
                if (buffer == null || methods == null)
                {
                    return;
                }

                newWin.ClassMembers = methods;
                // Add code window to our space of code window, if it isn't in there yet
                spaceManager[WindowSpaceManager.LOCAL_PLAYER].AddWindow(newWin);
                spaceManager[WindowSpaceManager.LOCAL_PLAYER].ActiveWindow = newWin;
            }
            else
            {
                spaceManager[WindowSpaceManager.LOCAL_PLAYER].ActiveWindow = ldocWin;
            }
        }

        protected override void UpdateDesktop()
        {
            // When the user clicked in the LiveDocumentation window
            if (Input.GetMouseButtonDown(0) && Window.activeSelf)
            {
                int classDoclink =
                    TMP_TextUtilities.FindIntersectingLink(ClassDocumentation, Input.mousePosition, null);


                // If the point the user has clicked on a valid link
                if (classDoclink != -1)
                {
                    string linkId = ClassDocumentation.textInfo.linkInfo[classDoclink].GetLinkID().ToString();
                    OnLinkClicked(linkId);
                }
            }
        }
    }
}