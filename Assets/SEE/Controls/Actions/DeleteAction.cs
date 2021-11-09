using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.Net;
using SEE.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to delete the currently selected game object (edge or node)
    /// including all its children.
    /// </summary>
    internal class DeleteAction : AbstractPlayerAction
    {
        /// <summary>
        /// Returns a new instance of <see cref="DeleteAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new DeleteAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="DeleteAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Delete"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Delete;
        }

        /// <summary>
        /// Disables the general selection provided by <see cref="SEEInput.Select"/>.
        /// We need to avoid that the selection of graph elements to be deleted
        /// interferes with the general <see cref="SelectAction"/>.
        /// </summary>
        public override void Start()
        {
            base.Start();
            SEEInput.SelectionEnabled = false;
        }

        /// <summary>
        /// Re-enables the general selection provided by <see cref="SEEInput.Select"/>.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            SEEInput.SelectionEnabled = true;
        }

        /// <summary>
        /// Deletes all objects marked as deleted by this action (i.e., set inactive)
        /// for good if action was actually completed.
        /// </summary>
        ~DeleteAction()
        {
            if (deletedGameObjects != null && currentState == ReversibleAction.Progress.Completed)
            {
                foreach (GameObject nodeOrEdge in deletedGameObjects)
                {
                    if (!nodeOrEdge.activeInHierarchy)
                    {
                        GameObject.Destroy(nodeOrEdge);
                    }
                }
            }
        }

        /// <summary>
        /// The graph element (a game object representing a node or edge) that was
        /// hit by the user for deletion. Set in <see cref="Update"/>.
        /// </summary>
        private GameObject hitGraphElement;

        /// <summary>
        /// Contains all implicitly deleted nodes and edges as a consequence of the deletion
        /// of one particular selected game object (in <see cref="Update"/>).
        ///
        /// If an edge is deleted, this set will contain only that deleted edge.
        /// If a node is deleted, the whole node subtree rooted by the selected
        /// node including edges whose source or target is contained in the subtree
        /// are deleted and contained in this set. This set will always include the
        /// explicitly selected node to be deleted.
        ///
        /// The <see cref="hitGraphElement"/> will always be included in this set
        /// unless it is null.
        ///
        /// Note that we will not actually destroy the deleted objects for the time
        /// being to be able to revert the deletion. Instead the objects will simply be set
        /// to inactive so that they are no longer visible and findable. They will
        /// eventually be deleted for good when this action ceases to exist, that
        /// is, in the destructor.
        /// </summary>
        private ISet<GameObject> deletedGameObjects;

        /// <summary>
        /// The subgraph of the underlying graph from which <see cref="deletedGameObjects"/> were
        /// removed. This information is kept because it is needed to revive the
        /// <see cref="deletedGameObjects"/> upon an <see cref="Undo"/> request.
        /// </summary>
        private GraphElementsMemento deletedSubgraph = null;

        /// <summary>
        /// See <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            // FIXME: Needs adaptation for VR where no mouse is available.
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) != HitGraphElement.None)
            {
                // the hit object is the one to be deleted
                hitGraphElement = raycastHit.collider.gameObject;
                Assert.IsTrue(hitGraphElement.HasNodeRef() || hitGraphElement.HasEdgeRef());
                InteractableObject.UnselectAll(true);
                (deletedSubgraph, deletedGameObjects) = GameElementDeleter.Delete(hitGraphElement);
                new DeleteNetAction(hitGraphElement.name).Execute();
                currentState = ReversibleAction.Progress.Completed;
                return true; // the selected objects are deleted and this action is done now
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Undoes this DeleteAction.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            GameElementDeleter.Revive(deletedGameObjects);
            new ReviveNetAction((from go in deletedGameObjects select go.name).ToList()).Execute();
        }

        /// <summary>
        /// Redoes this DeleteAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            GameElementDeleter.Delete(hitGraphElement);
            new DeleteNetAction(hitGraphElement.name).Execute();
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (deletedGameObjects == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return new HashSet<string>(deletedGameObjects.Select(x => x.name));
            }
        }
    }
}
