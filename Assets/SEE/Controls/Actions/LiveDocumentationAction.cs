using System.Collections.Generic;
using System.IO;
using System.Linq;
using SEE.Game.UI.LiveDocumantation;
using SEE.Game.UI.Notification;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using SEE.Utils.LiveDocumentation;
using UnityEngine;
using Extractor = SEE.Game.City.LiveDocumentation.Extractor;

namespace SEE.Controls.Actions
{
    public class LiveDocumentationAction : AbstractPlayerAction
    {
        private WindowSpaceManager spaceManager;

        public static LiveDocumentationAction CreateAction() => new LiveDocumentationAction();

        public override HashSet<string> GetChangedObjects() => new HashSet<string>();
        public override ActionStateType GetActionStateType() => ActionStateType.LiveDocumentation;

        public override ReversibleAction NewInstance() => CreateAction();

        private SyncWindowSpaceAction syncAction;

        public override void Awake()
        {
            // In case we do not have an ID yet, we request one.
            if (ICRDT.GetLocalID() == 0)
            {
                new NetCRDT().RequestID();
            }

            spaceManager = WindowSpaceManager.ManagerInstance;
        }

        public override void Start()
        {
            syncAction = new SyncWindowSpaceAction();
        }

        
        
        public override bool Update()
        {
            //  SceneQueries.GetCodeCity(selectedNode.transform).gameObject;
            // Only allow local player to open new code windows
            if (Input.GetMouseButtonDown(0) &&
                Raycasting.RaycastGraphElement(out RaycastHit hit, out GraphElementRef g) == HitGraphElement.Node)
            {
                NodeRef selectedNode = hit.collider.gameObject.GetComponent<NodeRef>();

                // When the node the user has clicked on has no file attached.
                // In this case an error is displayed.
                if (selectedNode.Value.Path() == null || selectedNode.Value.Filename() == null)
                {
                    ShowNotification.Error("Node has no File", "The selected node has no source code file attached");
                    return false;
                }

                // Concat the path and the file name to get the relative path of the file in the project
                string path = selectedNode.Value.Path() + selectedNode.elem.Filename();
                
                
                // When the node the user has clicked on wasn't a leaf node.
                // In this case an error message is displayed and the LiveDocumentation windows is not going to open.
                if ( !selectedNode.Value.Type.Equals("Class") )
                {
                    ShowNotification.Error("Node not supported", "Only leaf nodes can be analysed");
                    return false;
                }


                if (!selectedNode.TryGetComponent(out LiveDocumentationWindow documentationWindow))
                {
                    string fileName = selectedNode.Value.Filename();

                    // Copied from ShowCodeAction
                    if (fileName == null)
                    {
                        ShowNotification.Warn("No file",
                            $"Selected node '{selectedNode.Value.SourceName}' has no filename.");
                        return false;
                    }

                    string absolutePlatformPath = selectedNode.Value.AbsolutePlatformPath();
                    if (!File.Exists(absolutePlatformPath))
                    {
                        ShowNotification.Warn("File does not exist",
                            $"Path {absolutePlatformPath} of selected node '{selectedNode.Value.SourceName}' does not exist.");
                        return false;
                    }
                    string selectedFile = selectedNode.Value.Filename();
                    
                  
                    documentationWindow = selectedNode.gameObject.AddComponent<LiveDocumentationWindow>();
                  
                    documentationWindow.Title = selectedNode.Value.SourceName;

                    if (!documentationWindow.Title.Replace(".", "").Equals(selectedFile.Split('.').Reverse().Skip(1)
                            .Aggregate("", (acc, s) => s + acc)))
                    {
                        documentationWindow.Title += $" ({selectedFile})";
                    }
                    documentationWindow.ClassName = documentationWindow.Title;
                    documentationWindow.BasePath = selectedNode.elem.ItsGraph.BasePath;
                    documentationWindow.RelativePath = path;
                    documentationWindow.Graph = selectedNode.Value.ItsGraph;
                    documentationWindow.NodeOfClass = selectedNode.Value;

                    LiveDocumentationBuffer buffer = new LiveDocumentationBuffer();
                    FileParser parser = new FileParser(selectedNode.Value.AbsolutePlatformPath());
                    buffer = parser.ParseClassDoc(selectedNode.Value.AbsolutePlatformPath(), selectedNode.Value.SourceName);


                    List<LiveDocumentationBuffer> classMembers = new List<LiveDocumentationBuffer>();
                   // LiveDocumentationBuffer b = new LiveDocumentationBuffer();
                   classMembers = parser.ParseClassMethods( selectedNode.Value.AbsolutePlatformPath(), selectedNode.Value.SourceName);
                   if (buffer == null || classMembers == null)
                   {
                       return false;
                   }
                  //  classMembers.Add(b);

                    documentationWindow.DocumentationBuffer = buffer;
                    documentationWindow.ClassMembers = classMembers;
                }


                // Add code window to our space of code window, if it isn't in there yet
                if (!spaceManager[WindowSpaceManager.LOCAL_PLAYER].Windows.Contains(documentationWindow))
                {
                    spaceManager[WindowSpaceManager.LOCAL_PLAYER].AddWindow(documentationWindow);
                }

                spaceManager[WindowSpaceManager.LOCAL_PLAYER].ActiveWindow = documentationWindow;
            }

            return false;
        }
    }
}