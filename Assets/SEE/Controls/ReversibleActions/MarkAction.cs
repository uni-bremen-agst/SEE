using SEE.GraphElementRefs;
using SEE.Net.Actions.GraphElement;
using SEE.SceneManipulation;
using SEE.UserSettings;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.ReversibleActions
{   
    /// <summary>
    /// Marking a node by showing a sphere above the selected node 
    /// </summary>
    
    internal class MarkAction : AbstractPlayerAction
    {
        private string markedNodeID;


        /// <summary>
        /// returns ActionStateType
        /// </summary>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.MarkNode;
        }

        /// <summary>
        /// Returns all ID's of GameObjects manipulated by this action
        /// </summary>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>
            {
                markedNodeID
            };
        }


        /// <summary>
        /// Undoes this action.
        /// </summary>
        public override void Undo()
        {
  
            base.Undo(); //security from AbstarctPlayerAction

            GameObject node = GraphElementIDMap.Find(markedNodeID);

            if (node != null) 
            {
                GameNodeMarker.ToggleMark(node);

                new MarkNetAction(markedNodeID).Execute();
            }
               

        }

        /// <summary>
        /// Redoes this action.
        /// </summary>
        public override void Redo()
        {
            base.Redo();

            GameObject node = GraphElementIDMap.Find(markedNodeID);

            if (node != null)
            {
                GameNodeMarker.ToggleMark(node);

                new MarkNetAction(markedNodeID).Execute();
            }

        }

        /// <summary>
        /// creates new Object, useful for after the action was carried out
        /// </summary>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }


        /// <summary>
        /// Creating a new MarkAction object.
        /// </summary>
        public static IReversibleAction CreateReversibleAction()
        {
            return new MarkAction();
        }

        /// <summary>
        /// Reacting if user carries out new action, GameNodeMarker creates sphere, broadcasting to all clients
        /// </summary>
        public override bool Update()
        {
            if (UserSetting.IsDesktop && Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef ger, false) == HitGraphElement.Node
                && ger.gameObject.TryGetComponent(out InteractableObject io)
                && io.IsInteractable(raycastHit.point))
            {
                GameNodeMarker.ToggleMark(ger.gameObject);
                markedNodeID = ger.gameObject.name;
                new MarkNetAction(markedNodeID).Execute();
                CurrentState = IReversibleAction.Progress.Completed;
                return true;
            }
            return false;
        }
    }
}
