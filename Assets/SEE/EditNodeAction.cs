using SEE.GO;
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
        const ActionState.Type ThisActionState = ActionState.Type.EditNode;

        /// <summary>
        /// The life cycle of this edit action.
        /// </summary>
        public enum Progress
        {
            NoNodeSelected,  // initial state when no node is selected
            NodeSelected,    // a node is currently selected
            EditIsCanceled,  // the edit action is canceled
        }

        private Progress editProgress = Progress.NoNodeSelected;
        /// <summary>
        /// The current state of the edit-node process.
        /// </summary>
        public Progress EditProgress { get => editProgress; set => editProgress = value; }

        void Start()
        {
            if (!InitializeCanvasObject())
            {
                Debug.LogError($"No canvas object named {nameOfCanvasObject} could be found in the scene.\n");
                enabled = false;
                return;
            }
            ActionState.OnStateChanged += (ActionState.Type newState) =>
            {
                // Is this our action state where we need to do something?
                if (newState == ThisActionState)
                {
                    // The monobehaviour is enabled and Update() will be called by Unity.
                    enabled = true;
                    InteractableObject.LocalAnyHoverIn += LocalAnyHoverIn;
                    InteractableObject.LocalAnyHoverOut += LocalAnyHoverOut;
                    instantiated = true;
                }
                else
                {
                    // The monobehaviour is diabled and Update() no longer be called by Unity.
                    enabled = false;
                    CanvasGenerator c;
                    canvasObject.TryGetComponentOrLog<CanvasGenerator>(out c);
                    c.DestroyEditNodeCanvas();
                    instantiated = false;
                    InteractableObject.LocalAnyHoverIn -= LocalAnyHoverIn;
                    InteractableObject.LocalAnyHoverOut -= LocalAnyHoverOut;
                    hoveredObject = null;
                }
            };
            enabled = ActionState.Is(ThisActionState);
        }

        /// <summary>
        /// The Update method's behavior depends on the edit-progress state (sequential series).
        /// NoNodeSelected: Waits until a node is selected by selecting a game node via the mouse button.
        /// NodeSelected: Instantiates the canvasObject if a gameNode is selected.
        /// EditIsCanceled: Removes the canvas and resets all values if the process is canceled.
        /// </summary>
        void Update()
        {
            switch (editProgress)
            {
                case Progress.NoNodeSelected:
                    if (hoveredObject != null && Input.GetMouseButtonDown(0))
                    {
                        EditProgress = Progress.NodeSelected;
                    }
                    break;

                case Progress.NodeSelected:
                    if (canvasObject.GetComponent<EditNodeCanvasAction>() == null)
                    {
                        CanvasGenerator generator = canvasObject.GetComponent<CanvasGenerator>();
                        EditNodeCanvasAction script = generator.InstantiateEditNodeCanvas();
                        script.nodeToEdit = hoveredObject.GetComponent<NodeRef>().Value;
                        script.gameObjectID = hoveredObject.name;
                    }
                    break;

                case Progress.EditIsCanceled:
                    CanvasGenerator canvasGenerator = canvasObject.GetComponent<CanvasGenerator>();
                    canvasGenerator.DestroyEditNodeCanvas();
                    hoveredObject = null;
                    EditProgress = Progress.NoNodeSelected;
                    break;
            }
        }
    }
}

