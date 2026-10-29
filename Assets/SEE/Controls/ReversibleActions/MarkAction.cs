using SEE.GraphElementRefs;
using SEE.Graphs;
using SEE.Net.Actions.GraphElement;
using SEE.SceneManipulation;
using SEE.UserSettings;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;


namespace SEE.Controls.ReversibleActions
{
    /// <summary>
    /// Action to mark or unmark a node with a sphere.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// The ID of the node that was marked or unmarked.
        /// </summary>
        private string markedNodeID;

        /// <summary>
        /// Returns the action state type of this action.
        /// </summary>
        /// <returns>The action state type </returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.MarkNode;
        }

        /// <summary>
        /// Returns the IDs of  all game objects changed by this action.
        /// </summary>
        /// <returns>IDs od changed game objects. </returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> { markedNodeID };
        }

        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/>.
        /// </summary>
        /// <returns>new Instance</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new MarkAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/>.
        /// </summary>
        /// <returns>new Instance</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Updates the state of the  action. Called one per frame.
        /// </summary>
        /// <returns>True if a node was successfully marked or unmarked, false otherwise</returns>
        public override bool Update()
        {
            bool clickedOnTheBuilding = false;


            if (UserSetting.IsDesktop && Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef ger, false) == HitGraphElement.Node
                && ger.gameObject.TryGetComponent(out InteractableObject io)
                && io.IsInteractable(raycastHit.point))
            {

                GameObject clickedBuilding = ger.gameObject;


                GameNodeMarker.ToggleMark(clickedBuilding);


                markedNodeID = clickedBuilding.name;


                new MarkNetAction(markedNodeID).Execute();


                CurrentState = IReversibleAction.Progress.Completed;

                clickedOnTheBuilding = true;
            }

            return clickedOnTheBuilding;
        }

        /// <summary>
        /// Undoes the action.
        /// </summary>
        public override void Undo()
        {
            base.Undo();

            GameObject node = GraphElementIDMap.Find(markedNodeID);

            if (node != null)
            {

                GameNodeMarker.ToggleMark(node);


                new MarkNetAction(markedNodeID).Execute();

            }

        }
        /// <summary>
        /// Redoes the action.
        /// </summary>
        public override void Redo()
        {
            base.Redo();

            GameObject nodeToRemark = GraphElementIDMap.Find(markedNodeID);

            if (nodeToRemark != null)
            {

                GameNodeMarker.ToggleMark(nodeToRemark);

                new MarkNetAction(markedNodeID).Execute();

            }
        }
    }
}

