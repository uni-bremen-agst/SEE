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
    /// 
    /// The following fields must be set:
    /// <ul>
    ///     <li><see cref="ClassName"/> </li>
    ///     <li> <see cref="NodeOfClass"/> </li>
    /// </ul>
    /// Otherwise an error is displayed and the window is not rendereed.
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

        private GameObject ClassMembersList;

        /// <summary>
        /// Text mesh for the class documentation
        ///
        /// The Path inside the prefab should be "LiveDocumentation/ClassDocumentation/Viewport/Content/ClassDoc"
        /// </summary>
        private TextMeshProUGUI ClassDocumentation;

        private Dictionary<String, Node> NamespaceCache = new();


        public LiveDocumentationWindowType DocumentationWindowType { get; set; }

        /// <summary>
        /// The name of the class
        /// </summary>
        public string ClassName { get; set; }
        

        public List<string> ImportedNamespaces { get; set; } = new();

        /// <summary>
        /// The graph which the node the user has clicked on belongs to.
        /// This node is the node which documentation is displayed in this window.
        ///
        /// The Graph is needed to find a corresponding node when the user clicked on a link
        /// </summary>
        public Graph Graph;

        public Node NodeOfClass;

        private WindowSpaceManager spaceManager;

        /*
        private bool SpaceManagerContainsWindow(string filePath, out BaseWindow win)
        {
            List<LiveDocumentationWindow> matchingWindows = spaceManager[WindowSpaceManager.LOCAL_PLAYER].Windows
                .OfType<LiveDocumentationWindow>()
                .Where(x => x.RelativePath == filePath).ToList();


            if (matchingWindows.Count == 0)
            {
                win = null;
                return false;
            }

            win = matchingWindows[0];
            return true;
        }*/


        /// <summary>
        /// The <see cref="LiveDocumentationBuffer"/> which contains the documentation of the class including links
        /// </summary>
        public LiveDocumentationBuffer DocumentationBuffer { get; set; }

        public List<LiveDocumentationBuffer> ClassMembers { get; set; } =
            new List<LiveDocumentationBuffer>();


        /// <summary>
        /// Checks if all the necessary fields are set in the class
        /// </summary>
        /// <returns>Returns true when all fields are set. Otherwise false</returns>
        private bool CheckNecessaryFields() =>
            ClassName != null  && NodeOfClass != null;

        /// <summary>
        /// Adds a new Class member to the ClassMember section in the LiveDocumentation Window.
        ///
        /// Currently ClassMembers are represented by a <see cref="LiveDocumentationBuffer"/>.
        /// In this buffer all information and links of the method signature is stored. 
        /// </summary>
        /// <param name="buffer"></param>
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

            if (DocumentationWindowType == LiveDocumentationWindowType.METHOD)
            {
            }

            cm.OnLinkClicked += OnLinkClicked;
            cm.OnClicked.AddListener(OnClickClassMember);
        }

        private void OnClickClassMember(ClassMember cm)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                var method = NodeOfClass.Children().Where(x => x.Type.Equals("Method"))
                    .Where(x => x.GetInt("Source.Line") == cm.LineNumber).First();

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
                    newWin.Graph = Graph;
                    newWin.NodeOfClass = method;
                    newWin.DocumentationWindowType = LiveDocumentationWindowType.METHOD;
                    newWin.ImportedNamespaces = ImportedNamespaces;
                    newWin.DocumentationBuffer = classMemberBuffer.Documentation;
                    newWin.ClassMembers = classMemberBuffer.Parameters;

                    List<LiveDocumentationBuffer> methods = new List<LiveDocumentationBuffer>();


                    //  newWin.ClassMembers = methods;
                    //  newWin.DocumentationBuffer = buffer;
                    // Add code window to our space of code window, if it isn't in there yet


                    spaceManager[WindowSpaceManager.LOCAL_PLAYER].AddWindow(newWin);
                    spaceManager[WindowSpaceManager.LOCAL_PLAYER].ActiveWindow = newWin;
                }

                if (method != null)
                {
                    var oldScale = NodeOfClass.GameObject().transform.localScale;
                    if (!cm.HighlightAnimationRunning)
                    {
                        cm.HighlightAnimationRunning = true;
                        method.GameObject().transform.DOScale(oldScale * 1.5f, 0.3f).SetEase(Ease.InOutSine)
                            .SetLoops(2, LoopType.Yoyo).OnComplete(
                                () => { cm.HighlightAnimationRunning = false; }).Restart();
                    }
                }
            }
        }

        protected override void StartDesktop()
        {
            spaceManager = WindowSpaceManager.ManagerInstance;
            if (!CheckNecessaryFields())
            {
                Debug.LogError("Some fields are not set; cant load LiveDocumentation");
                return;
            }

            base.StartDesktop();
            var c = Canvas.GetComponent<Camera>();
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
            //     GameObject livedoc =
            //         PrefabInstantiator.InstantiatePrefab("Prefabs/UI/LiveDocumentation/ClassMember", ClassMembers.transform, false);

            ClassDocumentation.ForceMeshUpdate();

            foreach (var item in ClassMembers)
            {
                AddClassMember(item);
            }
        }

        private bool HighlightAnomation = false;

        /// <summary>
        /// Is called when the user clicked on a Text segment in the class name field.
        /// If the user technically clicked on the field, but hasn't hit any text the function isn't called.
        /// </summary>
        private void OnClickClassName()
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                var newScale = new Vector3(1.5f, 1.5f, 1.5f);
                var oldScale = NodeOfClass.GameObject().transform.localScale;
                if (!HighlightAnomation)
                {
                    HighlightAnomation = true;
                    NodeOfClass.GameObject().transform.DOScale(oldScale * 1.5f, 0.3f).SetEase(Ease.InOutSine)
                        .SetLoops(2, LoopType.Yoyo).OnComplete(
                            () => { HighlightAnomation = false; }).Restart();
                }
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

        //      private Node FindMatchingNode(string className)
        //     {
        //          var namspaceName = className.Split(".")[..(className.Split(".").Length - 1)]
        //           .Aggregate("", (((s, s1) => s1 + s)));
        //     }

        /// <summary>
        /// Finds a node given from a clicked filename.
        /// </summary>
        /// <param name="filename">The filename to find a corresponding node for.</param>
        /// <returns>The node or null if none could be found.</returns>
        [CanBeNull]
        private Node FindNodeWithPath(string @namespace)
        {
            //See if an item was found in the cache.
            if (NamespaceCache.ContainsKey(@namespace))
            {
                return NamespaceCache[@namespace];
            }

            // Iterate through each node in the code city.
            foreach (var item in Graph.Nodes())
            {
                // Only check for leaf nodes and classes.
                if (!(item.IsLeaf() || item.Type.Equals(NodeClassType)))
                    continue;
                //Collect all namespaces
                //TODO replace using with namespaces.
                var filePath = item.AbsolutePlatformPath();
                var input = File.ReadAllText(filePath);
                var lexer = new CSharpFullLexer(new AntlrInputStream(input));
                var tokens = new CommonTokenStream(lexer);
                tokens.Fill();

                var parser = new CSharpParser(tokens);
                var namespaces = parser.compilation_unit().namespace_member_declarations()
                    .namespace_member_declaration();

                foreach (var parsedNamespace in namespaces)
                {
                    if (parsedNamespace.namespace_declaration().qualified_identifier().GetText().Equals(@namespace))
                    {
                        NamespaceCache[@namespace] = item;
                        return item;
                    }
                }
            }

            return null;
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

                string path = nodeOfLink.Path() + nodeOfLink.Filename();
                var filenames = linkPath.Split("\\");
                newWin.Title = nodeOfLink.SourceName;
                string selectedFile = nodeOfLink.Filename();
                if (!newWin.Title.Replace(".", "").Equals(selectedFile.Split('.').Reverse().Skip(1)
                        .Aggregate("", (acc, s) => s + acc)))
                {
                    newWin.Title += $" ({selectedFile})";
                }

                newWin.ClassName = newWin.Title;
                newWin.Graph = Graph;
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
                //  newWin.DocumentationBuffer = buffer;
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
                int clickedWordInClassName =
                    TMP_TextUtilities.FindIntersectingWord(ClassNameField, Input.mousePosition, null);

                // ShowNotification.Warn("sdsd", a.ToString());

                // If the point the user has clicked really is a link
                if (classDoclink != -1)
                {
                    string linkId = ClassDocumentation.textInfo.linkInfo[classDoclink].GetLinkID().ToString();
                    OnLinkClicked(linkId);
                }

                if (clickedWordInClassName != -1)
                {
                    OnClickClassName();
                }
            }
        }
    }
}