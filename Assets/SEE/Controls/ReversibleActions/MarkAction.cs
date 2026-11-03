using System.Collections.Generic;
using SEE.Extensions;
using SEE.GraphElementRefs;
using SEE.Net.Actions.GraphElement;
using SEE.SceneManipulation;
using SEE.UserSettings;
using SEE.Utils;
using SEE.XR;
using UnityEngine;

namespace SEE.Controls.ReversibleActions
{
    /// <summary>
    /// Reversible player action for marking and unmarking game nodes.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// ID of the node that was changed by this action.
        /// </summary>
        private string gameNodeID;

        /// <summary>
        /// Stores whether the selected node was marked
        /// before this action 
        ///
        /// Undo restores exactly this state.
        /// </summary>
        private bool previousMarkState;

        /// <summary>
        /// Handles the selection of a game node.
        /// </summary>
        /// <returns>
        /// True when the action has been completed.
        /// </returns>
        
        public override bool Update()
        {
            GameObject selectedNode = null;

            
            if (UserSetting.IsDesktop
                && Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(
                    out RaycastHit raycastHit,
                    out GraphElementRef graphElementRef,
                    false) == HitGraphElement.Node
                && graphElementRef.gameObject.TryGetComponent(
                    out InteractableObject interactableObject)
                && interactableObject.IsInteractable(raycastHit.point))
            {
                selectedNode = graphElementRef.gameObject;
            }

          
            else if (UserSetting.IsVR
                     && XRSEEActions.Selected
                     && InteractableObject.HoveredObjectWithWorldFlag.gameObject != null
                     && InteractableObject.HoveredObjectWithWorldFlag.gameObject.HasNodeRef())
            {
                selectedNode =
                    InteractableObject.HoveredObjectWithWorldFlag.gameObject;

                XRSEEActions.Selected = false;
            }

           
            if (selectedNode == null)
            {
                return false;
            }

            // Remember which node was changed.
            gameNodeID = selectedNode.name;

            // Remember the state BEFORE our change.
            previousMarkState =
                GameNodeMarker.IsMarked(selectedNode);

            // Toggle the state only here.
            //
            // Afterwards we always work with explicit states.
            bool newMarkState = !previousMarkState;

            ApplyState(selectedNode, newMarkState);

            CurrentState = IReversibleAction.Progress.Completed;

            return true;
        }

        /// <summary>
        /// Applies a marking state locally and sends the same
        /// state to the other clients.
        /// </summary>
        /// <param name="gameNode">The node to change.</param>
        /// <param name="marked">The desired marking state.</param>
        private static void ApplyState(GameObject gameNode, bool marked)
        {
            // Perform the visual change locally.
            GameNodeMarker.ApplyMark(gameNode, marked);

            // Tell the other clients to use exactly the same state.
            new MarkNetAction(gameNode.name, marked).Execute();
        }

        /// <summary>
        /// Restores the state from before this action.
        /// </summary>
        public override void Undo()
        {
            base.Undo();

            GameObject gameNode =
                GraphElementIDMap.Find(gameNodeID);

            if (gameNode != null)
            {
                ApplyState(gameNode, previousMarkState);
            }
        }

        /// <summary>
        /// Applies the action again after an undo.
        /// </summary>
        public override void Redo()
        {
            base.Redo();

            GameObject gameNode =
                GraphElementIDMap.Find(gameNodeID);

            if (gameNode != null)
            {
                ApplyState(gameNode, !previousMarkState);
            }
        }

        /// <summary>
        /// Creates a new MarkAction.
        /// </summary>
        public static IReversibleAction CreateReversibleAction()
        {
            return new MarkAction();
        }

        /// <summary>
        /// Returns a fresh instance of this action.
        /// </summary>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the action-state type belonging to this action.
        /// </summary>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Mark;
        }

        /// <summary>
        /// Returns the graph objects changed by this action.
        /// </summary>
        public override HashSet<string> GetChangedObjects()
        {
            HashSet<string> changedObjects = new();

            if (gameNodeID != null)
            {
                changedObjects.Add(gameNodeID);
            }

            return changedObjects;
        }
    }
}