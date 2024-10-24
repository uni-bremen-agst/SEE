using System.Collections.Generic;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils.History;
using UnityEngine;
using SEE.Audio;
using SEE.Game.SceneManipulation;
using SEE.Utils;
using System;
using SEE.UI.PropertyDialog;
using SEE.DataModel.DG;
using SEE.XR;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to create a new node for a selected city.
    /// </summary>
    internal class AddNodeAction : AbstractPlayerAction
    {
        /// <summary>
        /// The life cycle of this add node action.
        /// </summary>
        private enum ProgressState
        {
            /// <summary>
            /// Initial state when no parent node is selected.
            /// </summary>
            NoNodeSelected,
            /// <summary>
            /// A new node is created and selected, the dialog is opened,
            /// and we wait for input.
            /// </summary>
            WaitingForInput,
            /// <summary>
            /// When the action is finished.
            /// </summary>
            Finish
        }

        /// <summary>
        /// The current state of the add node process.
        /// </summary>
        private ProgressState progress = ProgressState.NoNodeSelected;

        /// <summary>
        /// The chosen parent for the new node.
        /// Will be used for context menu execution.
        /// </summary>
        private GameObject parent;

        /// <summary>
        /// The chosen position for the new node.
        /// Will be used for context menu execution.
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// If the user clicks with the mouse hitting a game object representing a graph node,
        /// this graph node is a parent to which a new node is created and added as a child.
        /// <see cref="IReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;

            switch (progress)
            {
                case ProgressState.NoNodeSelected:
                    if (SceneSettings.InputType == PlayerInputType.DesktopPlayer && Input.GetMouseButtonDown(0)
                        && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) == HitGraphElement.Node)
                    {
                        // the hit object is the parent in which to create the new node
                        GameObject parent = raycastHit.collider.gameObject;
                        AddNode(raycastHit.collider.gameObject, raycastHit.point);
                    }
                    else if (SceneSettings.InputType == PlayerInputType.VRPlayer && XRSEEActions.Selected && InteractableObject.HoveredObjectWithWorldFlag.gameObject != null && InteractableObject.HoveredObjectWithWorldFlag.gameObject.HasNodeRef() &&
                        XRSEEActions.RayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit res))
                    {
                        // the hit object is the parent in which to create the new node
                        GameObject parent = res.collider.gameObject;
                        XRSEEActions.Selected = false;
                        AddNode(res.collider.gameObject, res.point);
                    }
                    else if (ExecuteViaContextMenu)
                    {
                        AddNode(parent, position);
                    }
                    break;
                case ProgressState.WaitingForInput:
                    // Waiting until the dialog is closed and all input is present.
                    break;
                case ProgressState.Finish:
                    result = true;
                    CurrentState = IReversibleAction.Progress.Completed;
                    AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.NewNodeSound, parent);
                    break;
                default:
                    throw new NotImplementedException($"Unhandled case {nameof(progress)}.");
            }
            return result;
        }

        /// <summary>
        /// Adds a node on the chosen <paramref name="parent"/> at the
        /// chosen <paramref name="position"/>.
        /// </summary>
        /// <param name="parent">The parent on which to place the node.</param>
        /// <param name="position">The position where the node should be placed.</param>
        private void AddNode(GameObject parent, Vector3 position)
        {
            addedGameNode = GameNodeAdder.AddChild(parent);
            // addedGameNode has the scale and position of parent.
            // The position at which the parent was hit will be the center point of the addedGameNode.
            // The node is scaled down and placed on top of its parent.
            addedGameNode.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            addedGameNode.transform.position = GameNodeMover.GetCoordinatesOn(addedGameNode.transform.lossyScale, position, parent);
            // TODO(#786) The new node is scaled down arbitrarily and might overlap with its siblings.
            memento = new(child: addedGameNode, parent: parent)
            {
                NodeID = addedGameNode.name
            };
            new AddNodeNetAction(parentID: memento.Parent.name, newNodeID: memento.NodeID, memento.Position, memento.Scale).Execute();
            progress = ProgressState.WaitingForInput;
            OpenDialog(addedGameNode.GetNode());
        }

        /// <summary>
        /// Opens a dialog where the user can enter the node name and type.
        /// If the user presses the OK button, the SourceName and Type of
        /// <see cref="memento.node"/> will have the new values entered
        /// and <see cref="memento.Name"/> and <see cref="memento.Type"/>
        /// will be set to memorize these and <see cref="progress"/> is
        /// moved forward to <see cref="ProgressState.ValuesAreGiven"/>.
        /// If the user presses the Cancel button, the node will be created as
        /// an unnamed node with the unkown type.
        /// </summary>
        private void OpenDialog(Node node)
        {
            NodePropertyDialog dialog = new(node);
            dialog.OnConfirm.AddListener(OKButtonPressed);
            dialog.OnCancel.AddListener(CancelButtonPressed);
            dialog.Open(true);
            SEEInput.KeyboardShortcutsEnabled = false;

            return;

            void OKButtonPressed()
            {
                memento.Name = node.SourceName;
                memento.Type = node.Type;
                new EditNodeNetAction(node.ID, node.SourceName, node.Type).Execute();
                InteractableObject.UnselectAll(true);
                progress = ProgressState.Finish;
                SEEInput.KeyboardShortcutsEnabled = true;
            }

            void CancelButtonPressed()
            {
                // Case when last used is used and it has a value other
                // then 'UNKOWNTYPE', use it.
                if (node.Type != Graph.UnknownType)
                {
                    memento.Name = node.SourceName;
                    memento.Type = node.Type;
                }
                progress = ProgressState.Finish;
                SEEInput.KeyboardShortcutsEnabled = true;
            }
        }

        /// <summary>
        /// Used to execute the <see cref="AddNodeAction"/> from the context menu.
        /// Calls <see cref="AddNode"/> and ensures that the <see cref="Update"/> method
        /// performs the execution via context menu.
        /// </summary>
        /// <param name="parent">The parent node.</param>
        /// <param name="position">The position where the node should be placed.</param>
        public void ContextMenuExecution(GameObject parent, Vector3 position)
        {
            this.parent = parent;
            this.position = position;
            ExecuteViaContextMenu = true;
        }

        /// <summary>
        /// The node that was added when this action was executed. It is saved so
        /// that it can be removed on <see cref="Undo"/>.
        /// </summary>
        private GameObject addedGameNode;

        /// <summary>
        /// Memento capturing the data necessary to re-do this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// The information we need to re-add a node whose addition was undone.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The parent of the new node.
            /// </summary>
            public readonly GameObject Parent;
            /// <summary>
            /// The position of the new node in world space.
            /// </summary>
            public readonly Vector3 Position;
            /// <summary>
            /// The scale of the new node in world space.
            /// </summary>
            public readonly Vector3 Scale;
            /// <summary>
            /// The node ID for the added node. It must be kept to re-use the
            /// original name of the node in Redo().
            /// </summary>
            public string NodeID;
            /// <summary>
            /// The chosen name for the added node.
            /// </summary>
            public string Name;
            /// <summary>
            /// The chosen type for the added node.
            /// </summary>
            public string Type;

            /// <summary>
            /// Constructor setting the information necessary to re-do this action.
            /// </summary>
            /// <param name="child">child that was added</param>
            /// <param name="parent">parent of <paramref name="child"/></param>
            public Memento(GameObject child, GameObject parent)
            {
                Parent = parent;
                Position = child.transform.position;
                Scale = child.transform.lossyScale;
                NodeID = null;
                Name = string.Empty;
                Type = string.Empty;
            }
        }

        /// <summary>
        /// Undoes this action.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (addedGameNode != null)
            {
                GameElementDeleter.RemoveNodeFromGraph(addedGameNode);
                new DeleteNetAction(addedGameNode.name).Execute();
                Destroyer.Destroy(addedGameNode);
                addedGameNode = null;
            }
        }

        /// <summary>
        /// Redoes this action.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            addedGameNode = GameNodeAdder.AddChild(memento.Parent, worldSpacePosition: memento.Position,
                                                   worldSpaceScale: memento.Scale, nodeID: memento.NodeID);
            if (addedGameNode != null)
            {
                new AddNodeNetAction(parentID: memento.Parent.name,
                    newNodeID: memento.NodeID, memento.Position, memento.Scale).Execute();

                if (!string.IsNullOrEmpty(memento.Type))
                {
                    Node node = addedGameNode.GetNode();
                    GameNodeEditor.ChangeName(node, memento.Name);
                    GameNodeEditor.ChangeType(node, memento.Type);
                    new EditNodeNetAction(node.ID, node.SourceName, node.Type).Execute();
                }
            }
        }

        /// <summary>
        /// Returns a new instance of <see cref="AddNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new AddNodeAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="AddNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.NewNode"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.NewNode;
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>
            {
                memento.Parent.name,
                memento.NodeID
            };
        }
    }
}
