using System.Collections.Generic;
using SEE.Game.UI.LiveDocumantation;
using SEE.Game.UI.Notification;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    public class LiveDocumentationAction : AbstractPlayerAction
    {
        private WindowSpaceManager spaceManager;

        public static LiveDocumentationAction CreateAction() => new LiveDocumentationAction();

        public override HashSet<string> GetChangedObjects() => new HashSet<string>();
        public override ActionStateType GetActionStateType() => ActionStateType.LiveDocumentation;

        public override ReversibleAction NewInstance() => CreateAction();

        public override void Awake()
        {
            // In case we do not have an ID yet, we request one.
            if (ICRDT.GetLocalID() == 0)
            {
                new NetCRDT().RequestID();
            }

            spaceManager = WindowSpaceManager.ManagerInstance;
        }

        public override bool Update()
        {
            // Only allow local player to open new code windows
            if (Input.GetMouseButtonDown(0) &&
                Raycasting.RaycastGraphElement(out RaycastHit hit, out GraphElementRef _) == HitGraphElement.Node)
            {
                NodeRef selectedNode = hit.collider.gameObject.GetComponent<NodeRef>();

                if (!selectedNode.Value.IsLeaf())
                {
                    ShowNotification.Warn("Node not supported", "Only leaf nodes can be analysed");
                    return false;
                }
                
                if (!selectedNode.TryGetComponent(out LiveDocumentationWindow documentationWindow))
                {
                    
                    string fileName = selectedNode.Value.Filename();
                    
                    documentationWindow = selectedNode.gameObject.AddComponent<LiveDocumentationWindow>();
                    documentationWindow.ClassName = selectedNode.Value.SourceName;
                    documentationWindow.Title = "LiveDoc";

                    LiveDocumentationBuffer buffer = new LiveDocumentationBuffer();
                    buffer.Add(new LiveDocumentationBufferText("Dies ist ein Text erstmal"));
                    buffer.Add(new LiveDocumentationLink("Test.cs", "Name of link"));
                    buffer.Add(new LiveDocumentationBufferText("Dies ist ein Text erstmal"));

                    buffer.Add(new LiveDocumentationBufferText("Dies ist ein Text erstmal"));

                    buffer.Add(new LiveDocumentationBufferText("Dies ist ein Text erstmal"));


                    List<LiveDocumentationBuffer> classMembers = new List<LiveDocumentationBuffer>();
                    LiveDocumentationBuffer b = new LiveDocumentationBuffer();
                    b.Add(new LiveDocumentationBufferText("test member"));
                    
                    classMembers.Add(b);
                    
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