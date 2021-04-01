using SEE.DataModel.DG;
using SEE.GO;
using SEE.GO.Menu;
using SEE.Utils;
using System;
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
            ValuesAreGiven, // values are given from the input fields
            EditIsCanceled,  // the edit action is canceled
        }

        /// <summary>
        /// The current state of the edit node process.
        /// </summary>
        public ProgressState EditProgress { get; set; } = ProgressState.NoNodeSelected;

        /// <summary>
        /// The node edited by this action. 
        /// </summary>
        private GameObject editedNode;

        /// <summary>
        /// True, if the action is executed, else false.
        /// </summary>
        private bool executed = false;

        /// <summary>
        /// The new name of the node.
        /// </summary>
        public static string NodeName { get; set; }

        /// <summary>
        /// The new type of the node.
        /// </summary>
        public static string NodeType { get; set; }

        /// <summary>
        /// The information we need to (re-)edit a node.
        /// </summary>
        public struct EditNodeMemento
        {
            public readonly Node node;
            public readonly string name;
            public readonly string type;
            public EditNodeMemento(Node node, string name, string type)
            {
                this.node = node;
                this.name = name;
                this.type = type;
            }
        }

        /// <summary>
        /// The information needed to undo an edit process. (The previous values)
        /// </summary>
        private EditNodeMemento undoEditNodeMemento;

        /// <summary>
        /// The information needed to redo an edit process. (The values after editing)
        /// </summary>
        private EditNodeMemento redoEditNodeMemento;

        public override void Start()
        {
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
        /// ValuesAreGiven: Saves the new values of the node in a memento and updates the specific node.
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
                    if (executed)
                    {
                        result = true;
                        hadAnEffect = true;
                        executed = false;
                    }
                    break;

                case ProgressState.NodeSelected:
                    if (canvasObject.GetComponent<EditNodeCanvasAction>() == null)
                    {
                        CanvasGenerator generator = canvasObject.GetComponent<CanvasGenerator>();
                        EditNodeCanvasAction editNode = generator.InstantiateEditNodeCanvas(this);
                        editNode.nodeToEdit = hoveredObject.GetComponent<NodeRef>().Value;
                        editNode.gameObjectID = hoveredObject.name;

                        undoEditNodeMemento = new EditNodeMemento(
                            hoveredObject.GetComponent<NodeRef>()?.Value,
                            hoveredObject.GetComponent<NodeRef>()?.Value.SourceName,
                            hoveredObject.GetComponent<NodeRef>()?.Value.Type);

                        editedNode = hoveredObject;
                    }
                    break;

                case ProgressState.ValuesAreGiven:
                    if (editedNode.TryGetComponentOrLog(out NodeRef node))
                    {
                        redoEditNodeMemento = new EditNodeMemento(node.Value, NodeName, NodeType);
                        UpdateNode(redoEditNodeMemento);
                        executed = true;
                        EditProgress = ProgressState.NoNodeSelected;
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
            undoEditNodeMemento.node.SourceName = undoEditNodeMemento.name;
            undoEditNodeMemento.node.Type = undoEditNodeMemento.type;
            executed = false;
        }

        /// <summary>
        /// Redoes this DeleteAction
        /// </summary>
        public override void Redo()
        {
            base.Redo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> property.
            UpdateNode(redoEditNodeMemento);
        }

        /// <summary>
        /// Is called when a new action is started at the end of this action
        /// </summary>
        public override void Stop()
        {
            InteractableObject.LocalAnyHoverIn -= LocalAnyHoverIn;
            InteractableObject.LocalAnyHoverOut -= LocalAnyHoverOut;

            if (canvasObject.TryGetComponent(out CanvasGenerator canvasGenerator))
            {
                canvasGenerator.DestroyEditNodeCanvasAction();
            }
            hoveredObject = null;
            EditProgress = ProgressState.NoNodeSelected;
        }

        /// <summary>
        /// Updates the values such as nodename and nodetype of a specific <paramref name="node"/>
        /// </summary>
        /// <param name="editNodeMemento">information needed to edit a node</param>
        public static void UpdateNode(EditNodeMemento editNodeMemento)
        {
            Node node = editNodeMemento.node;

            node.SourceName = editNodeMemento.name;
            node.Type = editNodeMemento.type;
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
