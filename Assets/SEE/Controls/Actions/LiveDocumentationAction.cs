using System.Collections.Generic;
using System.IO;
using System.Linq;
using SEE.Game.UI.LiveDocumentation;
using SEE.Game.UI.LiveDocumentation.Buffer;
using SEE.Game.UI.Notification;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using SEE.Utils.LiveDocumentation;
using UnityEngine;

namespace SEE.Controls.Actions
{
    public class LiveDocumentationAction : AbstractPlayerAction
    {
        /// <summary>
        /// Instance of <see cref="WindowSpaceManager"/> for managing the UI
        /// </summary>
        private WindowSpaceManager spaceManager;

        /// <summary>
        /// Creates a new instance of <see cref="LiveDocumentationAction"/>
        /// </summary>
        /// <returns>The new created instance</returns>
        public static ReversibleAction CreateAction()
        {
            return new LiveDocumentationAction();
        }

        /// <summary>
        /// Override of method from <see cref="AbstractPlayerAction"/>.
        ///
        /// Always returns an empty list. 
        /// </summary>
        /// <returns>An empty list of strings</returns>
        public override HashSet<string> GetChangedObjects() => new();

        /// <summary>
        /// Returns the ActionStateType of this action
        /// </summary>
        /// <returns>The ActionStateType (<see cref="ActionStateTypes.LiveDocumentation"/>)</returns>
        public override ActionStateType GetActionStateType() => ActionStateTypes.LiveDocumentation;

        /// <summary>
        /// Creates a new instance of the action
        /// </summary>
        /// <returns>The new instance</returns>
        public override ReversibleAction NewInstance() => CreateAction();

        /// <summary>
        /// Awake method
        ///
        /// Copied from <see cref="ShowCodeAction"/> works the same way.
        /// </summary>
        public override void Awake()
        {
            // In case we do not have an ID yet, we request one.
            if (ICRDT.GetLocalID() == 0) new NetCRDT().RequestID();

            spaceManager = WindowSpaceManager.ManagerInstance;
        }

        public override void Start()
        {
           // Do nothing because nothing has to be done here
        }

        /// <summary>
        /// Update method
        /// </summary>
        /// <returns></returns>
        public override bool Update()
        {
            // Only allow local player to open new code windows
            if (Input.GetMouseButtonDown(0) &&
                Raycasting.RaycastGraphElement(out var hit, out var g) == HitGraphElement.Node)
            {
                var selectedNode = hit.collider.gameObject.GetComponent<NodeRef>();

                // When the node the user has clicked on has no file attached.
                // In this case an error is displayed.
                if (selectedNode.Value.Path() == null || selectedNode.Value.Filename() == null)
                {
                    ShowNotification.Error("Node has no File", "The selected node has no source code file attached");
                    return false;
                }

                // Concat the path and the file name to get the relative path of the file in the project
                var path = selectedNode.Value.Path() + selectedNode.elem.Filename();


                // When the node the user has clicked on wasn't a leaf node.
                // In this case an error message is displayed and the LiveDocumentation windows is not going to open.
                if (!selectedNode.Value.Type.Equals("Class"))
                {
                    ShowNotification.Error("Node not supported", "Only leaf nodes can be analysed");
                    return false;
                }


                if (!selectedNode.TryGetComponent(out LiveDocumentationWindow documentationWindow))
                {
                    var fileName = selectedNode.Value.Filename();

                    // Copied from ShowCodeAction
                    if (fileName == null)
                    {
                        ShowNotification.Warn("No file",
                            $"Selected node '{selectedNode.Value.SourceName}' has no filename.");
                        return false;
                    }

                    var absolutePlatformPath = selectedNode.Value.AbsolutePlatformPath();
                    if (!File.Exists(absolutePlatformPath))
                    {
                        ShowNotification.Warn("File does not exist",
                            $"Path {absolutePlatformPath} of selected node '{selectedNode.Value.SourceName}' does not exist.");
                        return false;
                    }

                    var selectedFile = selectedNode.Value.Filename();


                    documentationWindow = selectedNode.gameObject.AddComponent<LiveDocumentationWindow>();

                    documentationWindow.Title = "Doc: " + selectedNode.Value.SourceName;

                    if (!documentationWindow.Title.Replace(".", "").Equals(selectedFile.Split('.').Reverse().Skip(1)
                            .Aggregate("", (acc, s) => s + acc)))
                        documentationWindow.Title += $" ({selectedFile})";

                    documentationWindow.ClassName = documentationWindow.Title;
                    documentationWindow.NodeOfClass = selectedNode.Value;

                    // Try initialise the FileParser 
                    FileParser parser;
                    try
                    {
                        parser = new FileParser(selectedNode.Value.AbsolutePlatformPath());
                    }
                    // If the source code file can't be found display an error and abort the action. 
                    catch (FileNotFoundException e)
                    {
                        ShowNotification.Error("File not found",
                            $"The file with the name {selectedNode.Value.AbsolutePlatformPath()}");
                        return false;
                    }


                    var buffer = parser.ParseClassDoc(
                        selectedNode.Value.SourceName);


                    var classMembers = new List<LiveDocumentationBuffer>();
                    parser.ParseClassMethods(selectedNode.Value.SourceName)
                        .ForEach(x => classMembers.Add(x));
                    if (buffer == null || classMembers == null) return false;

                    documentationWindow.DocumentationBuffer = buffer;
                    documentationWindow.ClassMembers = classMembers;

                    documentationWindow.ImportedNamespaces = parser.ParseNamespaceImports();
                }


                // Add code window to our space of code window, if it isn't in there yet
                if (!spaceManager[WindowSpaceManager.LOCAL_PLAYER].Windows.Contains(documentationWindow))
                    spaceManager[WindowSpaceManager.LOCAL_PLAYER].AddWindow(documentationWindow);

                spaceManager[WindowSpaceManager.LOCAL_PLAYER].ActiveWindow = documentationWindow;
            }

            return false;
        }
    }
}