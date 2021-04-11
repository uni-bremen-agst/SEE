﻿using SEE.DataModel.DG;
using SEE.Game.UI.PropertyDialog;
using SEE.GO;
using SEE.Utils;
using System;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to edit an existing node's attributes.
    /// </summary>
    public class EditNodeAction : AbstractPlayerAction
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
            public readonly Node node;
            /// <summary>
            /// The original source name of the node that should be used when the state is to be restored.
            /// </summary>
            public readonly string originalName;
            /// <summary>
            /// The original type of the node that should be used when the state is to be restored.
            /// </summary>
            public readonly string originalType;
            /// <summary>
            /// The new source name of the node that should be used for Redo().
            /// </summary>
            public string newName;
            /// <summary>
            /// The new type of the node that should be used for Redo().
            /// </summary>
            public string newType;
            /// <summary>
            /// Constructor setting the information necessary to re-set an edited node to
            /// its original state.
            /// </summary>
            /// <param name="node">the node that was edited</param>
            public Memento(Node node)
            {
                this.node = node;
                this.originalName = node.SourceName;
                this.originalType = node.Type;
                this.newName = string.Empty;
                this.newType = string.Empty;
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
        /// See <see cref="ReversibleAction.Update"/>.
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
                            progress = ProgressState.WaitingForInput;
                            memento = new Memento(node);
                            OpenDialog();
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
                    hadAnEffect = true;
                    NotifyClients(memento.node);
                    break;

                case ProgressState.EditIsCanceled:
                    progress = ProgressState.NoNodeSelected;
                    break;

                default:
                    throw new NotImplementedException("Unhandled case.");
            }
            return result;
        }

        /// <summary>
        /// Sends an EditNodeNetAction to all clients with the given <paramref name="node"/>'s
        /// ID, SourceName and Type.
        /// </summary>
        /// <param name="node">node whose changes should be propagated</param>
        private static void NotifyClients(Node node)
        {
            new EditNodeNetAction(node.ID, node.SourceName, node.Type);
        }

        /// <summary>
        /// Undoes this EditNodeAction.
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> property.
            memento.node.SourceName = memento.originalName;
            memento.node.Type = memento.originalType;
            NotifyClients(memento.node);
        }

        /// <summary>
        /// Redoes this EditNodeAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> property.
            memento.node.SourceName = memento.newName;
            memento.node.Type = memento.newType;
            NotifyClients(memento.node);
        }

        /// <summary>
        /// Returns a new instance of <see cref="EditNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new EditNodeAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="EditNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.EditNode"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.EditNode;
        }

        /// <summary>
        /// Opens a dialog where the user can enter the node name and type.
        /// </summary>
        private void OpenDialog()
        {
            // This dialog will set the source name and type of memento.node.
            NodePropertyDialog dialog = new NodePropertyDialog(memento.node);
            // If the OK button is pressed, we continue with ProgressState.ValuesAreGiven.
            dialog.OnConfirm.AddListener(() => progress = ProgressState.ValuesAreGiven);
            // If the Cancel button is pressed, we continue with ProgressState.AddingIsCanceled.
            dialog.OnCancel.AddListener(() => progress = ProgressState.EditIsCanceled);
            dialog.Open();
        }
    }
}
