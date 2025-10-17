using Cysharp.Threading.Tasks;
using MoreLinq;
using SEE.Audio;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.SceneManipulation;
using SEE.GO;
using SEE.Net.Actions;
using SEE.UI.Menu;
using SEE.Utils;
using SEE.Utils.History;
using SEE.XR;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        /// The graph element ids of <see cref="hitGraphElements"/>.
        /// </summary>
        private List<string> hitGraphElementIDs = new();

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
        /// The elements from <see cref="deletedGameObjects"/>.
        /// The value is only set when the <see cref="GraphElement"/> is a <see cref="Node"/>.
        /// </summary>
        private List<RestoreGraphElement> deletedElements;

        /// <summary>
        /// The <see cref="VisualNodeAttributes"/> for the node types that are deleted, to allow
        /// them to be restored.
        /// This is needed only in the case of deleting an implementation or architecture root.
        /// </summary>
        private Dictionary<string, VisualNodeAttributes> deletedNodeTypes = new();

        /// <summary>
        /// Indicates whether node types should be removed as part of this delete action.
        /// </summary>
        private bool removeNodeTypes = false;

        /// <summary>
        /// Represents the life cycle of a delete action.
        /// </summary>
        private enum ProgressState
        {
            Input,
            Validation,
            Deletion
        }

        /// <summary>
        /// The current state of the delete process.
        /// </summary>
        private ProgressState progress = ProgressState.Input;

        /// <summary>
        /// Indicates whether the validation phase has already been started.
        /// Prevents multiple instances of <see cref="HandleValidationAsync"/>
        /// from being triggered in consecutive Update calls.
        /// </summary>
        private bool validationStartet = false;

        /// <summary>
        /// See <see cref="IReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            switch (progress)
            {
                case ProgressState.Input:
                    HandleInputSelection();
                    break;
                case ProgressState.Validation:
                    if (!validationStartet)
                    {
                        validationStartet = true;
                        HandleValidationAsync().Forget();
                    }
                    break;
                case ProgressState.Deletion:
                    return Delete();
            }
            return false;
        }

        /// <summary>
        /// Handles the input phase of the delete action.
        /// Detects user interactions (mouse click, XR selection, or context menu)
        /// and transitions to the <see cref="ProgressState.Validation"/> phase
        /// once a valid deletion target has been selected.
        /// </summary>
        private void HandleInputSelection()
        {
            if (SceneSettings.InputType == PlayerInputType.DesktopPlayer && Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) != HitGraphElement.None)
            {
                // the hit object is the one to be deleted
                hitGraphElements.Add(raycastHit.collider.gameObject);
                hitGraphElementIDs.Add(raycastHit.collider.gameObject.name);
                progress = ProgressState.Validation;
            }
            else if (SceneSettings.InputType == PlayerInputType.VRPlayer && XRSEEActions.Selected)
            {
                // the hit object is the one to be deleted
                hitGraphElements.Add(InteractableObject.HoveredObjectWithWorldFlag.gameObject);
                hitGraphElementIDs.Add(InteractableObject.HoveredObjectWithWorldFlag.gameObject.name);
                XRSEEActions.Selected = false;
                progress = ProgressState.Validation;
            }
            else if (ExecuteViaContextMenu)
            {
                ExecuteViaContextMenu = false;
                progress = ProgressState.Validation;
            }
        }

        /// <summary>
        /// Handles the validation phase of the delete action.
        /// Checks the selected deletion targets and shows a confirmation dialog
        /// asking whether node types should be deleted,
        /// but only if one of the selected objects is a architecture or implementation root node.
        /// Transitions to the <see cref="ProgressState.Deletion"/> phase
        /// once validation is complete.
        /// </summary>
        private async UniTask HandleValidationAsync()
        {
            if (hitGraphElements.Any(ele => ele.CompareTag(Tags.Node)
                && ele.GetNode().IsArchitectureOrImplementationRoot()))
            {
                string message = "Should the unused node types also be removed?";
                removeNodeTypes = await ConfirmDialog.ConfirmAsync(ConfirmConfiguration.YesNo(message));
            }
            progress = ProgressState.Deletion;
        }

        /// <summary>
        /// Executes the deletion.
        /// </summary>
        /// <returns>true if the deletion can be executed.</returns>
        private bool Delete()
        {
            deletedGameObjects = new HashSet<GameObject>();
            deletedElements = new();
            InteractableObject.UnselectAll(true);
            foreach (GameObject go in hitGraphElements)
            {
                if (!go.HasNodeRef() && !go.HasEdgeRef()
                    || go.HasNodeRef() && go.IsRoot())
                {
                    continue;
                }

                new DeleteNetAction(go.name, removeNodeTypes).Execute();
                (GraphElementsMemento mem,
                    ISet<GameObject> deleted,
                    Dictionary<string, VisualNodeAttributes> deletedNTypes) = GameElementDeleter.Delete(go, removeNodeTypes);

                if (deleted == null)
                {
                    continue;
                }

                deletedGameObjects.UnionWith(deleted);
                deleted.ForEach(go =>
                {
                    SEECity city = go.ContainingCity<SEECity>();

                    GraphElement ele = GetGraphElement(go);
                    if (ele is Node node)
                    {
                        deletedElements.Add(new RestoreNodeElement(((SubgraphMemento)mem).Parents[node].ID,
                            node.ID, go.transform.position, go.transform.lossyScale, node.Type, node.SourceName, node.Level));
                    }
                    else if (ele is Edge edge)
                    {
                        deletedElements.Add(new RestoreEdgeElement(edge.ID, edge.Source.ID, edge.Target.ID, edge.Type));
                    }
                });
                deletedNodeTypes = deletedNodeTypes.Concat(deletedNTypes)
                                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            CurrentState = IReversibleAction.Progress.Completed;
            AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.DropSound, true);
            return true;

            static GraphElement GetGraphElement(GameObject go)
            {
                if (go.TryGetNode(out Node node))
                {
                    return node;
                }
                else if (go.TryGetEdge(out Edge edge))
                {
                    return edge;
                }
                return null;
            }
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
            hitGraphElementIDs = toDelete.Select(x => x.name).ToList();
        }

        /// <summary>
        /// Undoes this <see cref="DeleteAction">.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            /// First, try to find the corresponding <see cref="GameObject"/>s if the references in the set are <c>null</c>.
            if (deletedGameObjects.All(go => go == null))
            {
                deletedGameObjects.Clear();
                deletedGameObjects.UnionWith(deletedElements.Select(ele => GraphElementIDMap.Find(ele.ID)));
            }
            /// Revive the objects.
            if (deletedGameObjects.All(go => go != null))
            {
                GameElementDeleter.Revive(deletedGameObjects, deletedNodeTypes);
                new ReviveNetAction((from go in deletedGameObjects select go.name).ToList(), deletedNodeTypes).Execute();
            }
            else
            /// Occurs if the corresponding <see cref="GameObject"/>s cannot be found. This typically happens after a redraw.
            {
                GameElementDeleter.Restore(deletedElements, deletedNodeTypes);
                new RestoreNetAction(deletedElements, deletedNodeTypes).Execute();
            }
        }

        /// <summary>
        /// Redoes this <see cref="DeleteAction">.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (hitGraphElements.All(go => go == null))
            {
                hitGraphElements = hitGraphElementIDs.Select(go => GraphElementIDMap.Find(go)).ToList();
            }
            foreach (GameObject go in hitGraphElements)
            {
                if (go.IsRoot())
                {
                    continue;
                }
#pragma warning disable VSTHRD110
                new DeleteNetAction(go.name, removeNodeTypes).Execute();
                GameElementDeleter.Delete(go, removeNodeTypes);
#pragma warning restore VSTHRD110
            }
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (deletedElements == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return new HashSet<string>(deletedElements.Select(ele => ele.ID));
            }
        }
    }
}
