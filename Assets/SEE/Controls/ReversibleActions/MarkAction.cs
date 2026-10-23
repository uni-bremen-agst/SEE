using System.Collections.Generic;
using SEE.Extensions;
using SEE.GraphElementRefs;
using SEE.Net.Actions.GraphElement;
using SEE.SceneManipulation;
using SEE.UserSettings;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.ReversibleActions
{
    /// <summary>
    /// A reversible action that marks or unmarks a game node in the scene.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// Memento capturing the data necessary to re-do this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// The mark that was added when this action was executed. It is saved so
        /// that it can be removed on <see cref="Undo"/>.
        /// </summary>
        private GameObject addedMark;

        /// <summary>
        /// The information we need to re-add a mark that was undone.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The node ID for the added node. It must be kept to re-use the
            /// original name of the node in Redo().
            /// </summary>
            public string NodeID;
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Mark"/>.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Mark;
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>All IDs of gameObjects manipulated by this action.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>
            {
                memento.NodeID
            };
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
        /// Executes this action, marking or unmarking the game node.
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
                memento.NodeID = ger.gameObject.ID();
                GameNodeMarker.Toggle(ger.gameObject);
                // TODO : MarkNetAction
                CurrentState = IReversibleAction.Progress.Completed;
                result = true;
            }

            return result;
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
        /// Undoes this action.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            GameObject node = GraphElementIDMap.Find(memento.NodeID);

            if (node != null)
            {
                GameNodeMarker.Toggle(node);
                // TODO : MarkNetAction
            }
        }

        /// <summary>
        /// Redoes this action.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            GameObject node = GraphElementIDMap.Find(memento.NodeID);

            if (node != null)
            {
                GameNodeMarker.Toggle(node);
                // TODO : MarkNetAction
            }
        }
    }
}
