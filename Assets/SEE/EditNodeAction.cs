using SEE.GO;
using UnityEngine;


namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to edit an existing node which has to be selected first.
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

        public enum Progress
        {
            NoNodeSelected,
            NodeSelected,
            EditIsCanceled,
        }

        /// <summary>
        /// An instance of the ProgressEnum, which represents the current state of the Edit-Node-process.
        /// </summary>
        private Progress editProgress = Progress.NoNodeSelected;

        public Progress EditProgress { get => editProgress; set => editProgress = value; }

        void Start()
        {
            InitialiseCanvasObject();
            // An anonymous delegate is registered for the event <see cref="ActionState.OnStateChanged"/>.
            // This delegate will be called from <see cref="ActionState"/> upon every
            // state changed where the passed parameter is the newly entered state.
            ActionState.OnStateChanged += (ActionState.Type newState) =>
            {
                // Is this our action state where we need to do something?
                if (newState == ThisActionState)
                {
                    // The monobehaviour is enabled and Update() will be called by Unity.
                    enabled = true;
                    InteractableObject.LocalAnyHoverIn += LocalAnyHoverIn;
                    InteractableObject.LocalAnyHoverOut += LocalAnyHoverOut;
                    if (!instantiated)
                    {
                        instantiated = true;
                    }

                }
                else
                {
                    // The monobehaviour is diabled and Update() no longer be called by Unity.
                    enabled = false;
                    CanvasGenerator c = canvasObject.GetComponent<CanvasGenerator>();
                    c.DestroyEditNodeCanvas();
                    instantiated = false;
                    InteractableObject.LocalAnyHoverIn -= LocalAnyHoverIn;
                    InteractableObject.LocalAnyHoverOut -= LocalAnyHoverOut;
                    hoveredObject = null;
                }
            };
            enabled = ActionState.Is(ThisActionState);
            //ChangeState(ThisActionState);
        }

        /// <summary>
        /// The update-method interacts in dependency of the edit-progress-state. (sequencial series)
        /// NoNodeSelected: Waits until a node is selected by pushing mouse button on a gameNode 
        /// NodeSelected: Instantiates the canvasObject if a gameNode is selected 
        /// EditIsCanceled: Removes the canvas and resets values if the process is canceled
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

