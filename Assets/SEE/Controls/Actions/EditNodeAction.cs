using SEE.GO;
using SEE.Utils;
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
        /// The current state of the edit-node process.
        /// </summary>
        public ProgressState EditProgress { get; set; } = ProgressState.NoNodeSelected;

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
        /// EditIsCanceled: Removes the canvas and resets all values if the process is canceled.
        /// See <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;

            // FIXME: When is this action done? That is, when should result be true?
            switch (EditProgress)
            {
                case ProgressState.NoNodeSelected:
                    if (hoveredObject != null && Input.GetMouseButtonDown(0))
                    {
                        EditProgress = ProgressState.NodeSelected;
                    }
                    break;

                case ProgressState.NodeSelected:
                    if (canvasObject.GetComponent<EditNodeCanvasAction>() == null)
                    {
                        CanvasGenerator generator = canvasObject.GetComponent<CanvasGenerator>();
                        EditNodeCanvasAction editNode = generator.InstantiateEditNodeCanvas(this);
                        editNode.nodeToEdit = hoveredObject.GetComponent<NodeRef>().Value;
                        editNode.gameObjectID = hoveredObject.name;                        
                    }
                    break;

                case ProgressState.EditIsCanceled:
                    CanvasGenerator canvasGenerator = canvasObject.GetComponent<CanvasGenerator>();
                    canvasGenerator.DestroyEditNodeCanvasAction();
                    hoveredObject = null;
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
            Debug.Log("Undo EditNode");
        }

        /// <summary>
        /// Redoes this DeleteAction
        /// </summary>
        public override void Redo()
        {
            Debug.Log("Redo EditNode");
        }

        /// <summary>
        /// Is called when a new action is started at the end of this action
        /// </summary>
        public override void Stop()
        {
            CanvasGenerator canvasGenerator = canvasObject.GetComponent<CanvasGenerator>();
            canvasGenerator.DestroyEditNodeCanvasAction();
            hoveredObject = null;
            EditProgress = ProgressState.NoNodeSelected;
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

