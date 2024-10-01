using System;
using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.UI.PropertyDialog;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using UnityEngine;
using SEE.Utils.History;
using SEE.Game.SceneManipulation;
using SEE.UI.Notification;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to edit an existing node's attributes.
    /// </summary>
    internal class EditNodeAction : AbstractPlayerAction
    {
        /// <summary>
        /// The life cycle of this edit action.
        /// </summary>
        private enum ProgressState
        {
            NoNodeSelected,  // initial state when no node is selected
            WaitingForInput, // a node is currently selected, the dialog is opened, and we wait for input
            ValuesAreGiven,  // the dialog is closed and input is given
            EditIsCanceled,  // the edit action is canceled
        }

        /// <summary>
        /// The current state of the edit node process.
        /// </summary>
        private ProgressState progress = ProgressState.NoNodeSelected;

        /// <summary>
        /// The information we need to (re-)edit a node.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// Node whose state is represented here.
            /// </summary>
            public readonly Node Node;
            /// <summary>
            /// The original source name of the node that should be used when the state is to be restored.
            /// </summary>
            public readonly string OriginalName;
            /// <summary>
            /// The original type of the node that should be used when the state is to be restored.
            /// </summary>
            public readonly string OriginalType;
            /// <summary>
            /// The new source name of the node that should be used for Redo().
            /// </summary>
            public string NewName;
            /// <summary>
            /// The new type of the node that should be used for Redo().
            /// </summary>
            public string NewType;
            /// <summary>
            /// Constructor setting the information necessary to re-set an edited node to
            /// its original state.
            /// </summary>
            /// <param name="node">the node that was edited</param>
            public Memento(Node node)
            {
                this.Node = node;
                this.OriginalName = node.SourceName;
                this.OriginalType = node.Type;
                this.NewName = string.Empty;
                this.NewType = string.Empty;
            }
        }

        /// <summary>
        /// The memento holding the information for Undo and Redo.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// The Update method's behavior depends on the edit-progress state (sequential series).
        /// NoNodeSelected: Waits until a node is selected by selecting a game node via the mouse button.
        /// NodeSelected: Instantiates the canvasObject if a gameNode is selected.
        /// ValuesAreGiven: Saves the new values of the node in a memento and updates the specific node.
        /// EditIsCanceled: Removes the canvas and resets all values if the process is canceled.
        /// See <see cref="IReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;
            switch (progress)
            {
                case ProgressState.NoNodeSelected:
                    // FIXME: Needs adaptation for VR where no mouse is available.
                    if (Input.GetMouseButtonDown(0)
                        && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) == HitGraphElement.Node)
                    {
                        // the hit object is the node to be edited
                        GameObject editedNode = raycastHit.collider.gameObject;
                        if (editedNode.TryGetNode(out Node node))
                        {
                            if (!node.IsRoot())
                            {
                                progress = ProgressState.WaitingForInput;
                                memento = new Memento(node);
                                OpenDialog();
                            }
                            else
                            {
                                ShowNotification.Warn("Root node is readonly", "You cannot edit the root node.");
                            }
                        }
                        else
                        {
                            Debug.LogError($"Game node {editedNode.name}'s node reference is null.\n");
                        }
                    }
                    break;

                case ProgressState.WaitingForInput:
                    // Waiting until the dialog is closed and all input is present
                    break;

                case ProgressState.ValuesAreGiven:
                    progress = ProgressState.NoNodeSelected;
                    result = true;
                    CurrentState = IReversibleAction.Progress.Completed;
                    NotifyClients(memento.Node);
                    break;

                case ProgressState.EditIsCanceled:
                    progress = ProgressState.NoNodeSelected;
                    break;

                default:
                    throw new NotImplementedException($"Unhandled case {nameof(progress)}.");
            }
            return result;
        }

        /// <summary>
        /// Used to execute the <see cref="EditNodeAction"/> from the context menu.
        /// Opens the edit dialog for the <paramref name="node"/>
        /// and ensures that the <see cref="Update"/> method performs the execution via context menu.
        /// </summary>
        /// <param name="node">The node to be edit.</param>
        public void ContextMenuExecution(Node node)
        {
            progress = ProgressState.WaitingForInput;
            memento = new Memento(node);
            OpenDialog();
        }

        /// <summary>
        /// Sends an EditNodeNetAction to all clients with the given <paramref name="node"/>'s
        /// ID, SourceName and Type.
        /// </summary>
        /// <param name="node">node whose changes should be propagated</param>
        private static void NotifyClients(Node node)
        {
            new EditNodeNetAction(node.ID, node.SourceName, node.Type).Execute();
        }

        /// <summary>
        /// Undoes this EditNodeAction.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            GameNodeEditor.ChangeName(memento.Node, memento.OriginalName);
            GameNodeEditor.ChangeType(memento.Node, memento.OriginalType);
            NotifyClients(memento.Node);
        }

        /// <summary>
        /// Redoes this EditNodeAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            GameNodeEditor.ChangeName(memento.Node, memento.NewName);
            GameNodeEditor.ChangeType(memento.Node, memento.NewType);
            NotifyClients(memento.Node);
        }

        /// <summary>
        /// Returns a new instance of <see cref="EditNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static IReversibleAction CreateReversibleAction() => new EditNodeAction();

        /// <summary>
        /// Returns a new instance of <see cref="EditNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override IReversibleAction NewInstance() => CreateReversibleAction();

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.EditNode"/></returns>
        public override ActionStateType GetActionStateType() => ActionStateTypes.EditNode;

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects() =>
            memento.Node == null ? new HashSet<string>() : new HashSet<string> { memento.Node.ID };

        /// <summary>
        /// Opens a dialog where the user can enter the node name and type.
        /// If the user presses the OK button, the SourceName and Type of
        /// <see cref="memento.node"/> will have the new values entered
        /// and <see cref="memento.newName"/> and <see cref="memento.newType"/>
        /// will be set to memorize these and <see cref="progress"/> is
        /// moved forward to <see cref="ProgressState.ValuesAreGiven"/>.
        /// If the user presses the Cancel button, the <see cref="memento"/>
        /// including <see cref="memento.node"/> will not be changed and
        /// <see cref="progress"/> is moved forward to
        /// <see cref="ProgressState.EditIsCanceled"/>.
        /// </summary>
        private void OpenDialog()
        {
            // This dialog will set the source name and type of memento.node.
            NodePropertyDialog dialog = new NodePropertyDialog(memento.Node);
            dialog.OnConfirm.AddListener(OKButtonPressed);
            dialog.OnCancel.AddListener(CancelButtonPressed);
            dialog.Open();
            SEEInput.KeyboardShortcutsEnabled = false;

            void OKButtonPressed()
            {
                progress = ProgressState.ValuesAreGiven;
                memento.NewName = memento.Node.SourceName;
                memento.NewType = memento.Node.Type;
                InteractableObject.UnselectAll(true);
                SEEInput.KeyboardShortcutsEnabled = true;
            }

            void CancelButtonPressed()
            {
                progress = ProgressState.EditIsCanceled;
                SEEInput.KeyboardShortcutsEnabled = true;
            }
        }
    }
}
