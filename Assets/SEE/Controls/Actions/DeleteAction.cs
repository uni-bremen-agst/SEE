using Cysharp.Threading.Tasks;
using MoreLinq;
using SEE.Audio;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.SceneManipulation;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI.Notification;
using SEE.UI.RuntimeConfigMenu;
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
        /// The <see cref="VisualNodeAttributes"/> for the node types that are deleted, to allow
        /// them to be restored.
        /// This is needed only in the case of deleting an implementation or architecture root.
        /// </summary>
        private readonly Dictionary<string, VisualNodeAttributes> deletedNodeTypes = new();

        /// <summary>
        /// See <see cref="IReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            // FIXME: Needs adaptation for VR where no mouse is available.
            if (SceneSettings.InputType == PlayerInputType.DesktopPlayer && Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) != HitGraphElement.None)
            {
                // the hit object is the one to be deleted
                hitGraphElements.Add(raycastHit.collider.gameObject);
                return Delete(); // the selected objects are deleted and this action is done now
            }
            else if (SceneSettings.InputType == PlayerInputType.VRPlayer && XRSEEActions.Selected)
            {
                // the hit object is the one to be deleted
                hitGraphElements.Add(InteractableObject.HoveredObjectWithWorldFlag.gameObject);
                XRSEEActions.Selected = false;
                return Delete(); // the selected objects are deleted and this action is done now
            }
            else if (ExecuteViaContextMenu)
            {
                ExecuteViaContextMenu = false;
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

                // Architecture and implementation root nodes should only be cleared (all children deleted)
                // instead of deleting the entire node, as it is not possible to add new architecture or
                // implementation root nodes.
                if (go.IsArchitectureOrImplementationRoot())
                {
                    Node root = go.GetNode();
                    IEnumerable<Node> children = root.IsInArchitecture() ?
                        root.ItsGraph.Nodes().Where(node => node.IsInArchitecture()
                                                            && !node.HasRootToogle()
                                                            && node.Parent != null
                                                            && (!node.Parent.IsInArchitecture()
                                                                || node.Parent.IsArchitectureOrImplementationRoot())) :
                        root.ItsGraph.Nodes().Where(node => node.IsInImplementation()
                                                            && !node.HasRootToogle()
                                                            && node.Parent != null
                                                            && (!node.Parent.IsInImplementation()
                                                                || node.Parent.IsArchitectureOrImplementationRoot()));
                    if (root.Children().Count() != children.Count()
                        || go.transform.GetComponentsInChildren<NodeRef>().Count() != root.PostOrderDescendants().Count)
                    {
                        ShowNotification.Warn("Can't clear.", "Because the mapping process has already started.");
                        continue;
                    }
                    CaptureNodeTypesToRemove(root);
                    foreach (Node child in root.Children().ToList())
                    {
                        new DeleteNetAction(child.GameObject().name).Execute();
                        (_, ISet<GameObject> deleted) = GameElementDeleter.Delete(child.GameObject());
                        deletedGameObjects.UnionWith(deleted);
                    }
                    /// Notify <see cref="RuntimeConfigMenu"/> about changes.
                    if (LocalPlayer.TryGetRuntimeConfigMenu(out RuntimeConfigMenu runtimeConfigMenu))
                    {
                        runtimeConfigMenu.PerformRebuildOnNextOpening();
                    }
                }
                else
                {
                    new DeleteNetAction(go.name).Execute();
                    (_, ISet<GameObject> deleted) = GameElementDeleter.Delete(go);
                    deletedGameObjects.UnionWith(deleted);
                }
            }
            CurrentState = IReversibleAction.Progress.Completed;
            AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.DropSound);
            return true;
        }

        /// <summary>
        /// Identifies the node types belonging to this subgraph and removes them from the graph.
        /// </summary>
        /// <param name="root">The root of this subgraph.</param>
        /// <param name="rememberRemovedNodeTypes">Whether the deleted node types need to be remembered.
        /// Should be switchable for the redo case.</param>
        private void CaptureNodeTypesToRemove(Node root, bool rememberRemovedNodeTypes = true)
        {
            IEnumerable<string> typesToRemove = GetNodeTypesFromSubgraph(root);
            SEEReflexionCity city = root.GameObject().ContainingCity<SEEReflexionCity>();
            IEnumerable<string> remainingTypes = GetRemainingGraphNodeTypes(root, city);
            IEnumerable<string> typesDifference = typesToRemove.Except(remainingTypes);
            typesDifference.ForEach(type =>
            {
                if (rememberRemovedNodeTypes && city.NodeTypes.TryGetValue(type, out VisualNodeAttributes visualNodeAttribute))
                {
                    deletedNodeTypes.Add(type, visualNodeAttribute);
                }
            });
            if (root.Children().Count > 0)
            {
                RemoveTypesAfterDeletion().Forget();
            }
            return;

            async UniTask RemoveTypesAfterDeletion()
            {
                GameObject firstChild = root.Children().First()?.GameObject();
                await UniTask.WaitUntil(() => firstChild.activeInHierarchy == false);
                typesDifference.ForEach(type =>
                {
                    city.NodeTypes.Remove(type);
                    new RemoveNodeTypeNetAction(city.transform.parent.name, type).Execute();
                });
            }

            IEnumerable<string> GetNodeTypesFromSubgraph(Node subgraph)
            {
                return subgraph.PostOrderDescendantsWithoutItself().Select(node => node.Type).Distinct();
            }

            IEnumerable<string> GetRemainingGraphNodeTypes(Node subgraph, SEEReflexionCity city)
            {
                /// Attention: At this point, the root nodes must come from the graph's nodes list <see cref="Graph.Nodes()"/>.
                /// If the <see cref="ReflexionGraph.ArchitectureRoot"/> or <see cref="ReflexionGraph.ImplementationRoot"/> is used,
                /// it doesn't work because the children are not added to these root nodes reference.
                return subgraph.Type == ReflexionGraph.ArchitectureType ?
                    GetNodeTypesFromSubgraph(city.ReflexionGraph.GetNode(city.ReflexionGraph.ImplementationRoot.ID))
                    : GetNodeTypesFromSubgraph(city.ReflexionGraph.GetNode(city.ReflexionGraph.ArchitectureRoot.ID));
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
        }

        /// <summary>
        /// Undoes this DeleteAction.
        /// </summary>
        public override void Undo()
        {
            base.Undo();

            /// Notify <see cref="RuntimeConfigMenu"/> about changes.
            if (hitGraphElements.Any(obj => obj.IsArchitectureOrImplementationRoot())
                && LocalPlayer.TryGetRuntimeConfigMenu(out RuntimeConfigMenu runtimeConfigMenu))
            {
                runtimeConfigMenu.PerformRebuildOnNextOpening();
            }
            deletedGameObjects.ForEach(go =>
            {
                if (go.HasNodeRef() && go.ContainingCity<SEEReflexionCity>() != null)
                {
                    SEEReflexionCity city = go.ContainingCity<SEEReflexionCity>();
                    Node node = go.GetNode();
                    if (!city.NodeTypes.TryGetValue(node.Type, out VisualNodeAttributes _))
                    {
                        city.NodeTypes[node.Type] = deletedNodeTypes[node.Type];
                        new AddNodeTypeNetAction(city.transform.parent.name, node.Type, deletedNodeTypes[node.Type]).Execute();
                    }
                }
            });
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
#pragma warning disable VSTHRD110
                if (go.IsArchitectureOrImplementationRoot())
                {
                    Node root = go.GetNode();
                    CaptureNodeTypesToRemove(root, false);
                    foreach (Node child in root.Children().ToList())
                    {
                        new DeleteNetAction(child.GameObject().name).Execute();
                        GameElementDeleter.Delete(child.GameObject());
                    }
                    /// Notify <see cref="RuntimeConfigMenu"/> about changes.
                    if (LocalPlayer.TryGetRuntimeConfigMenu(out RuntimeConfigMenu runtimeConfigMenu))
                    {
                        runtimeConfigMenu.PerformRebuildOnNextOpening();
                    }
                }
                else
                {
                    new DeleteNetAction(go.name).Execute();
                    GameElementDeleter.Delete(go);
                }
#pragma warning restore VSTHRD110
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
