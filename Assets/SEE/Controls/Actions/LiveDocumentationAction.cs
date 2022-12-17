using System.Collections.Generic;
using SEE.Game.UI.CodeWindow;
using SEE.Game.UI.LiveDocumentation;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    public class LiveDocumentationAction : AbstractPlayerAction
    {
        public static LiveDocumentationAction CreateAction() => new LiveDocumentationAction();

        public override HashSet<string> GetChangedObjects() => new HashSet<string>();
        public override ActionStateType GetActionStateType() => ActionStateType.LiveDocumentation;

        public override ReversibleAction NewInstance() => CreateAction();

        public override bool Update()
        {
            // Only allow local player to open new code windows
            if (Input.GetMouseButtonDown(0) &&
                Raycasting.RaycastGraphElement(out RaycastHit hit, out GraphElementRef _) == HitGraphElement.Node)
            {
                NodeRef selectedNode = hit.collider.gameObject.GetComponent<NodeRef>();

                selectedNode.gameObject.AddComponent<LiveDocumentationWindow>();

                //   selectedNode.gameObject;
            }

            return false;
        }
    }
}