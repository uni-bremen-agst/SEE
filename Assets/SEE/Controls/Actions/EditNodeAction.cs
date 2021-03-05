using SEE.GO;
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
        /// Start() will register an anonymous delegate of type 
        /// <see cref="ActionState.OnStateChangedFn"/> on the event
        /// <see cref="ActionState.OnStateChanged"/> to be called upon every
        /// change of the action state, where the newly entered state will
        /// be passed as a parameter. The anonymous delegate will compare whether
        /// this state equals <see cref="ThisActionState"/> and if so, execute
        /// what needs to be done for this action here. If that parameter is
        /// different from <see cref="ThisActionState"/>, this action will
        /// put itself to sleep. 
        /// Thus, this action will be executed only if the new state is 
        /// <see cref="ThisActionState"/>.
        /// </summary>
        private readonly ActionStateType ThisActionState = ActionStateType.EditNode;

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

        public void Start()
        {
            if (!InitializeCanvasObject())
            {
                Debug.LogError($"No canvas object named {nameOfCanvasObject} could be found in the scene.\n");
                return;
            }
            ActionState.OnStateChanged += newState =>
            {
                // Is this our action state where we need to do something?
                if (Equals(newState, ThisActionState))
                {
                    // The MonoBehaviour is enabled and Update() will be called by Unity.
                    InteractableObject.LocalAnyHoverIn += LocalAnyHoverIn;
                    InteractableObject.LocalAnyHoverOut += LocalAnyHoverOut;
                    instantiated = true;
                }
                else
                {
                    // The MonoBehaviour is disabled and Update() no longer be called by Unity.
                    canvasObject.TryGetComponentOrLog(out CanvasGenerator canvasGenerator);
                    canvasGenerator.DestroyEditNodeCanvasAction();
                    instantiated = false;
                    InteractableObject.LocalAnyHoverIn -= LocalAnyHoverIn;
                    InteractableObject.LocalAnyHoverOut -= LocalAnyHoverOut;
                    hoveredObject = null;
                }
            };
        }

        /// <summary>
        /// The Update method's behavior depends on the edit-progress state (sequential series).
        /// NoNodeSelected: Waits until a node is selected by selecting a game node via the mouse button.
        /// NodeSelected: Instantiates the canvasObject if a gameNode is selected.
        /// EditIsCanceled: Removes the canvas and resets all values if the process is canceled.
        /// </summary>
        public override void Update()
        {
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
                        EditNodeCanvasAction script = generator.InstantiateEditNodeCanvas();
                        script.nodeToEdit = hoveredObject.GetComponent<NodeRef>().Value;
                        script.gameObjectID = hoveredObject.name;
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
        }

        /// <summary>
        /// Undoes this EditNodeAction
        /// </summary>
        public override void Undo()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Redoes this DeleteAction
        /// </summary>
        public override void Redo()
        {
            throw new NotImplementedException();
        }

    }
}

