using System.Collections.Generic;
using System.Linq;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using SEE.Audio;
using SEE.Game.SceneManipulation;
using SEE.Utils.History;

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
        public static IReversibleAction CreateReversibleAction()
        {
            return new DeleteAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="DeleteAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Delete"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Delete;
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
            if (deletedGameObjects != null && CurrentState == IReversibleAction.Progress.Completed)
            {
                foreach (GameObject nodeOrEdge in deletedGameObjects)
                {
                    if (!nodeOrEdge.activeInHierarchy)
                    {
                        Destroyer.Destroy(nodeOrEdge);
                    }
                }
            }
        }

        /// <summary>
        /// The graph elements (game objects, each representing a node or edge) that were
        /// chosen by the user for deletion.
        /// </summary>
        private List<GameObject> hitGraphElements = new();

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
        /// Note that we will not actually destroy the deleted objects for the time
        /// being to be able to revert the deletion. Instead the objects will simply be set
        /// to inactive so that they are no longer visible and findable. They will
        /// eventually be deleted for good when this action ceases to exist, that
        /// is, in the destructor.
        /// </summary>
        private ISet<GameObject> deletedGameObjects;

        /// <summary>
        /// See <see cref="IReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            // FIXME: Needs adaptation for VR where no mouse is available.
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) != HitGraphElement.None)
            {
                // the hit object is the one to be deleted
                hitGraphElements.Add(raycastHit.collider.gameObject);
                return Delete(); // the selected objects are deleted and this action is done now
            }
            else if (ExecuteViaContextMenu)
            {
                return Delete();
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Executes the deletion.
        /// </summary>
        /// <returns>true if the deletion can be executed.</returns>
        private bool Delete()
        {
            deletedGameObjects = new HashSet<GameObject>();
            InteractableObject.UnselectAll(true);
            foreach (GameObject go in hitGraphElements)
            {
                if (!go.HasNodeRef() && !go.HasEdgeRef()
                    || go.HasNodeRef() && go.IsRoot())
                {
                    continue;
                }
                (_, ISet<GameObject> deleted) = GameElementDeleter.Delete(go);
                deletedGameObjects.UnionWith(deleted);
            }
            CurrentState = IReversibleAction.Progress.Completed;
            AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.DropSound);
            return true;
        }

        /// <summary>
        /// Used to execute the <see cref="DeleteAction"> from the context menu.
        /// It sets the object to be deleted and ensures that the <see cref="Update"/> method
        /// performs the execution via context menu.
        /// </summary>
        /// <param name="toDelete">The object to be deleted.</param>
        public void ContextMenuExecution(GameObject toDelete)
        {
            ContextMenuExecution(new List<GameObject> { toDelete });
        }

        /// <summary>
        /// Used to execute the <see cref="DeleteAction"/> from the multiselection context menu.
        /// It sets the objects to be deleted and ensures that the <see cref="Update"/> method
        /// performs the execution via context menu.
        /// </summary>
        /// <param name="toDelete">The objects to be deleted.</param>
        public void ContextMenuExecution(IEnumerable<GameObject> toDelete)
        {
            ExecuteViaContextMenu = true;
            hitGraphElements = toDelete.ToList();
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
            foreach (GameObject go in hitGraphElements)
            {
                if (go.IsRoot())
                {
                    continue;
                }
                _ = GameElementDeleter.Delete(go);
                new DeleteNetAction(go.name).Execute();
            }
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
