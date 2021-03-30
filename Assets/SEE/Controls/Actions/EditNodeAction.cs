using SEE.DataModel.DG;
using SEE.GO;
using SEE.GO.Menu;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to edit an existing node.
    /// </summary>
    public class EditNodeAction : AbstractPlayerAction
    {

        /// <summary>
        /// The life cycle of this edit action.
        /// </summary>
        public enum ProgressState
        {
            NoNodeSelected,  // initial state when no node is selected
            NodeSelected,    // a node is currently selected
            EditIsCanceled,  // the edit action is canceled
        }

        /// <summary>
        /// The current state of the edit node process.
        /// </summary>
        public ProgressState EditProgress { get; set; } = ProgressState.NoNodeSelected;

        /// <summary>
        /// The edited node and their previous name and type edited by this action. 
        /// </summary>
        private Tuple<GameObject, string, string> editedNode;

        /// <summary>
        /// The information we need to (re-)edit a node.
        /// </summary>
        private struct EditNodeMemento
        {
        }

        /// <summary>
        /// The new name and new type of the node, which could be undone.
        /// </summary>
        private Tuple<string, string, Node> changesToBeRedone;

        /// <summary>
        /// The previous values (name and type) of the edited node.
        /// </summary>
        //private Tuple<GameObject, string, string> nodeToEdit;

        public override void Start()
        {
            Debug.Log("STARTED");
            if (!InitializeCanvasObject())
            {
                Debug.LogError($"No canvas object named {nameOfCanvasObject} could be found in the scene.\n");
                return;
            }
            InteractableObject.LocalAnyHoverIn += LocalAnyHoverIn;
            InteractableObject.LocalAnyHoverOut += LocalAnyHoverOut;
        }

        /// <summary>
        /// The Update method's behavior depends on the edit-progress state (sequential series).
        /// NoNodeSelected: Waits until a node is selected by selecting a game node via the mouse button.
        /// NodeSelected: Instantiates the canvasObject if a gameNode is selected.
        /// EditIsCanceled: Removes the canvas and resets all values if the process is canceled.
        /// See <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;
            switch (EditProgress)
            {
                case ProgressState.NoNodeSelected:
                    if (hoveredObject != null && Input.GetMouseButtonDown(0))
                    {
                        EditProgress = ProgressState.NodeSelected;
                    }
                    // this case will be reached after editing a node for saving
                    // the previous values of this specific node.
                    if (editedNode != null)
                    {
                        result = true;
                        hadAnEffect = true;
                        editedNode = null; //WHAT - BOOl - null setzen nicht möglich
                    }
                    break;

                case ProgressState.NodeSelected:
                    if (canvasObject.GetComponent<EditNodeCanvasAction>() == null)
                    {
                        CanvasGenerator generator = canvasObject.GetComponent<CanvasGenerator>();
                        EditNodeCanvasAction editNode = generator.InstantiateEditNodeCanvas(this);
                        editNode.nodeToEdit = hoveredObject.GetComponent<NodeRef>().Value;
                        editNode.gameObjectID = hoveredObject.name;
                        editedNode = new Tuple<GameObject, string, string>
                            (hoveredObject, hoveredObject.GetComponent<NodeRef>().Value.SourceName, hoveredObject.GetComponent<NodeRef>().Value.Type);
                        changesToBeRedone = null;
                    }
                    break;

                case ProgressState.EditIsCanceled:
                    CanvasGenerator canvasGenerator = canvasObject.GetComponent<CanvasGenerator>();
                    canvasGenerator.DestroyEditNodeCanvasAction();
                    hoveredObject = null;
                    editedNode = null;
                    PlayerMenu.InteractionIsForbidden = false;
                    EditProgress = ProgressState.NoNodeSelected;
                    break;

                default:
                    throw new System.NotImplementedException("Unhandled case.");
            }
            return result;
        }

        /// <summary>
        /// Undoes this EditNodeAction
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> property.
            changesToBeRedone = new Tuple<string, string, Node>
                (editedNode.Item1.GetComponent<NodeRef>().Value.SourceName,
                editedNode.Item1.GetComponent<NodeRef>().Value.Type,
                editedNode.Item1.GetComponent<NodeRef>().Value);
            editedNode.Item1.GetComponent<NodeRef>().Value.SourceName = editedNode.Item2;
            editedNode.Item1.GetComponent<NodeRef>().Value.Type = editedNode.Item3;
        }

        /// <summary>
        /// Redoes this DeleteAction
        /// </summary>
        public override void Redo()
        {
            base.Redo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> property.
            Debug.Log(changesToBeRedone.Item1);
            UpdateNode(changesToBeRedone.Item1, changesToBeRedone.Item2, changesToBeRedone.Item3);
        }

        /// <summary>
        /// Is called when a new action is started at the end of this action
        /// </summary>
        public override void Stop()
        {
            InteractableObject.LocalAnyHoverIn -= LocalAnyHoverIn;
            InteractableObject.LocalAnyHoverOut -= LocalAnyHoverOut;
            CanvasGenerator canvasGenerator = canvasObject.GetComponent<CanvasGenerator>();
            canvasGenerator.DestroyEditNodeCanvasAction();
            hoveredObject = null;
            EditProgress = ProgressState.NoNodeSelected;
        }

        /// <summary>
        /// Updates the values such as nodename and nodetype of a specific <paramref name="node"/>
        /// </summary>
        /// <param name="newName">the new name of the <paramref name="node"/></param>
        /// <param name="newType">the new type of the <paramref name="node"/></param>
        /// <param name="node">the node to be editing</param>
        public static void UpdateNode(string newName, string newType, Node node)
        {
            if (!newName.Equals(node.SourceName))
            {
                node.SourceName = newName;
            }
            if (!newType.Equals(node.Type))
            {
                node.Type = newType;
            }
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
    }
}

