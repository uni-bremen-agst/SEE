using System;
using System.Collections.Generic;
using UnityEngine;

using SEE.XR;
using SEE.Extensions;
using SEE.SceneManipulation;
using SEE.GraphElementRefs;
using SEE.Controls.KeyActions;
using SEE.Net.Actions.GraphElement;
using SEE.UserSettings;
using SEE.Utils;

namespace SEE.Controls.ReversibleActions
{
    /// <summary>
    /// Action to mark a node
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {

        /// <summary>
        /// If the user clicks with the mouse hitting a node
        /// the node gets marked (or unmarked)
        /// <see cref="IReversibleAction.Update"/>.
        /// </summary>
        /// <returns>True if completed.</returns>
        public override bool Update()
        {
            bool result = false;

            if (UserSetting.IsDesktop && Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef ger, false) == HitGraphElement.Node
                && ger.gameObject.TryGetComponent(out InteractableObject io)
                && io.IsInteractable(raycastHit.point))
            {
                MarkNode(raycastHit.collider.gameObject);
                result = true;
            }
            else if (UserSetting.IsVR
                && XRSEEActions.Selected
                && InteractableObject.HoveredObjectWithWorldFlag.gameObject != null
                && InteractableObject.HoveredObjectWithWorldFlag.gameObject.HasNodeRef()
                && XRSEEActions.RayInteractor.TryGetCurrent3DRaycastHit(out raycastHit))
            {
                XRSEEActions.Selected = false;
                MarkNode(raycastHit.collider.gameObject);
                result = true;
            }

            return result;
        }

        /// <summary>
        /// marks (or removes mark from) node
        /// </summary>
        /// <param name="node">The node on which to place the sphere / mark.</param>
        private void MarkNode(GameObject node)
        {
            GameNodeMarker.AddMark(node);
            addedMarkParent = node;
            new MarkNetAction(node.name).Execute();
        }

        /// <summary>
        /// The parent that got markes when this action was executed. It is saved so
        /// that it can be marked/unmarked again <see cref="Undo"/>.
        /// </summary>
        private GameObject addedMarkParent;

        /// <summary>
        /// Undoes this action.
        /// </summary>
        public override void Undo()
        {
            base.Undo();

            MarkNode(addedMarkParent);
        }

        /// <summary>
        /// Redoes this action.
        /// </summary>
        public override void Redo()
        {
            base.Redo();

            MarkNode(addedMarkParent);
        }

        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new MarkAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns>The action state type for marking nodes.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.MarkNode;
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>All IDs of gameObjects manipulated by this action.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> {
                addedMarkParent.name
            };
        }
    }
}
