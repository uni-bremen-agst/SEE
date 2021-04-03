using UnityEngine;
using UnityEngine.UI;
using SEE.Controls.Actions;

namespace SEE.Controls
{
    /// <summary>
    /// This component is attached to the prefabs Resources/Prefabs/NewNode.prefab
    /// and Resources/Prefabs/EditNode.prefab and 
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
        private void Start()
        {
            AddingButton?.onClick?.AddListener(SetNextAddingNodeStep);
            EditNodeCancel?.onClick?.AddListener(EditIsCanceled);
            EditNodeButton?.onClick?.AddListener(EditNode);
            AddNodeCancel?.onClick?.AddListener(AddingIsCanceled);
        }

        /// <summary>
        /// This results in the next step of addingNode.
        /// This method is registered as a callback listening to the <see cref="AddingButton"/>.
        /// </summary>
        private void SetNextAddingNodeStep()
        {
            AddingNodeCanvasAction.AddNode = true;
        }

        /// <summary>
        /// Marks the action to add a new node as being canceled.
        /// </summary>
        private void AddingIsCanceled()
        {
            AddingNodeCanvasAction.Canceled = true;
        }

        /// <summary>
        /// Marks the action to edit an existing node as being canceled.
        /// </summary>
        private void EditIsCanceled()
        {
            EditNodeCanvasAction.Canceled = true;
        }

        /// <summary>
        /// Sets <see cref="EditNodeCanvasAction.EditNode"/> to true, which starts the edit process 
        /// and evaluation of the inputFields.
        /// </summary>
        private void EditNode()
        {
            EditNodeCanvasAction.EditNode = true;
        }
    }
}