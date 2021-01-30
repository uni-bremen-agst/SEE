using SEE.GO;
using UnityEngine;


namespace SEE.Controls.Actions
{
    public class EditNodeAction : NodeAction
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
            ChangeState(ThisActionState);
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
                    RemoveScript();
                    EditProgress = Progress.NoNodeSelected;
                    break;
            }
        }

    }
}

