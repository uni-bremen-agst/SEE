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
            ValuesAreGiven,  // values are given from the input fields
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
        /// True, if the action has started to be executed, that is, something
        /// has already happened, else false.
        /// </summary>
        private bool executed = false;

        /// <summary>
        /// The new source name of the node.
        /// </summary>
        public static string NodeName { get; set; }

        /// <summary>
        /// The new type of the node.
        /// </summary>
        public static string NodeType { get; set; }

        /// <summary>
        /// The information we need to (re-)edit a node.
        /// </summary>
        private struct EditNodeMemento
        {
            /// <summary>
            /// Node whose state is represented here.
            /// </summary>
            public readonly Node node;
            /// <summary>
            /// The source name of the node that should be used when the state is to be restored.
            /// </summary>
            public readonly string name;
            /// <summary>
            /// The type of the node that should be used when the state is to be restored.
            /// </summary>
            public readonly string type;
            /// <summary>
            /// Constructor setting the information necessary to re-set an edited node to
            /// its original state.
            /// </summary>
            /// <param name="node">the node that was edited</param>
            /// <param name="name">the source name of the node that should be used when the state is to be restored</param>
            /// <param name="type">the type of the node that should be used when the state is to be restored</param>
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

        /// <summary>
        /// Initializes the canvas and register the action at <see cref="InteractableObject"/>
        /// for hovering events.
        /// See <see cref="ReversibleAction.Start"/>.
        /// </summary>
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
                    // This case will be reached after editing a node for saving
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
                        if (hoveredObject.TryGetComponent(out NodeRef nodeRef))
                        {
                            editNode.nodeToEdit = nodeRef.Value;
                            editNode.gameObjectID = hoveredObject.name;

                            undoEditNodeMemento 
                                = new EditNodeMemento(nodeRef.Value, nodeRef.Value.SourceName, nodeRef.Value.Type);
                            editedNode = hoveredObject;
                        }
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
                    throw new NotImplementedException("Unhandled case.");
            }
            return result;
        }

        /// <summary>
        /// Undoes this EditNodeAction.
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> property.
            undoEditNodeMemento.node.SourceName = undoEditNodeMemento.name;
            undoEditNodeMemento.node.Type = undoEditNodeMemento.type;
            executed = false;
        }

        /// <summary>
        /// Redoes this EditNodeAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> property.
            UpdateNode(redoEditNodeMemento);
        }

        /// <summary>
        /// Unregisters the action at <see cref="InteractableObject"/> for hovering events.
        /// <see cref="ReversibleAction.Stop"/>. Destroys the canvas.
        /// Is called when a new action is started at the end of this action.
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
        /// Sets the source name and node type given in <paramref name="editNodeMemento"/>
        /// for the node given in <paramref name="editNodeMemento"/>.
        /// </summary>
        /// <param name="editNodeMemento">information needed to edit a node</param>
        private static void UpdateNode(EditNodeMemento editNodeMemento)
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
