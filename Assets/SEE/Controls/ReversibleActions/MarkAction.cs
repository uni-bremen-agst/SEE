using System;
using SEE.UserSetting;
using SEE.XR;
using SEE.Net.Actions.MarkNetAction;
using SEE.GraphElementRefs;
using SEE.SceneManipulation.GameModeMarker;
using SEE.Controls.KeyActions;

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
        }

        /// <summary>
        /// marks (or removes mark from) node
        /// </summary>
        /// <param name="node">The node on which to place the sphere / mark.</param>
        private void MarkNode(GameObject node)
        {
            addedMark = GameNodeMarker.AddMark(node);
            lastParent = addedMark.Parent;
            new MarkNetAction(lastParent.NodeID).Execute;
        }

        /// <summary>
        /// The mark that was added when this action was executed. It is saved so
        /// that it can be removed on <see cref="Undo"/>.
        /// </summary>
        private GameObject addedMark;

        /// <summary>
        /// The mark that was added when this action was executed. It is saved so
        /// that it can be removed on <see cref="Undo"/>.
        /// </summary>
        private GameObject lastParent;

        /// <summary>
        /// Undoes this action.
        /// </summary>
        public override void Undo()
        {
            base.Undo();

            MarkNode(lastParent);

        }

        /// <summary>
        /// Redoes this action.
        /// </summary>
        public override void Redo()
        {
            base.Redo();

            MarkNode(lastParent);
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
            return addedMark != null ?
                return lastParent != null ?
                    new HashSet<string> {
                        lastParent.NodeID,
                        addedMark.ID,
                    } :
                new HashSet<string> {
                    lastParent.NodeID,
                } :
            new HashSet<string> {};
        }
    }
}
