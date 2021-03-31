using UnityEngine;
using UnityEngine.UI;
using SEE.Controls.Actions;

namespace SEE.Controls
{
    /// <summary>
    /// This component is added to the button of the adding-node canvas and the edit-node canvas.
    /// FIXME: What is the purpose of this component?
    /// </summary>
    public class NodeInteractionButtons : MonoBehaviour
    {
        /// <summary>
        /// The button on the adding-node canvas that is finishing the addition of a new node.
        /// </summary>
        public Button AddingButton;

        /// <summary>
        /// The button on the addNode canvas that is canceling the addition of a new node.
        /// </summary>
        public Button AddNodeCancel;

        /// <summary>
        /// The button on the editNode canvas that is canceling the editNode process.
        /// </summary>
        public Button EditNodeCancel;

        /// <summary>
        /// The button on the editNode canvas that is finishing the editNode process.
        /// </summary>
        public Button EditNodeButton;

        /// <summary>
        /// Adds a listener to the button which calls a method when the button is pushed.
        /// </summary>   
        void Start()
        {
            AddingButton?.onClick?.AddListener(SetNextAddingNodeStep);
            EditNodeCancel?.onClick?.AddListener(EditIsCanceled);
            EditNodeButton?.onClick?.AddListener(EditNode);
            AddNodeCancel?.onClick?.AddListener(AddingIsCanceled);
        }

        /// <summary>
        /// Increases the progress enum in the <see cref="AddNodeAction"/> instance. 
        /// This results in the next step of addingNode.
        /// </summary>
        public void SetNextAddingNodeStep()
        {
            AddingNodeCanvasAction.NextState = true;
        }

        /// <summary>
        /// Marks the action to edit an existing node as being canceled.
        /// </summary>
        public void EditIsCanceled()
        {
            EditNodeCanvasAction.Canceled = true;
        }

        /// <summary>
        /// Sets <see cref="EditNodeCanvasAction.EditNode"/> to true, which starts the edit process 
        /// and evaluation of the inputFields.
        /// </summary>
        public void EditNode()
        {
            EditNodeCanvasAction.EditNode = true;
        }

        /// <summary>
        /// Marks the action to add a new node as being canceled.
        /// </summary>
        public void AddingIsCanceled()
        {
            AddingNodeCanvasAction.Canceled = true;
        }
    }
}