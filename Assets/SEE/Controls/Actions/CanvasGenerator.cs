using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This class instantiates and destroys the adding-node canvas, which contains the addingNodeCanvas prefab.
    /// This also applies to the EditNodeCanvas.
    /// </summary>
    public class CanvasGenerator : MonoBehaviour
    {
        /// <summary>
        /// Instantiates the AddingNodeCanvasScript and adds it to the CanvasObject gameObject.
        /// </summary>
        /// <returns>the added AddingNodeCanvasScript</returns>
        public AddingNodeCanvasAction InstantiateAddingNodeCanvas()
        {
            return gameObject.AddComponent<AddingNodeCanvasAction>();
        }

        /// <summary>
        /// Instantiates the EditNodeCanvasScript and adds it to the CanvasObject gameObject.
        /// </summary>
        /// <returns>the added EditNodeCanvasScript</returns>
        public EditNodeCanvasAction InstantiateEditNodeCanvas(EditNodeAction editNodeAction)
        {
            gameObject.AddComponent<EditNodeCanvasAction>();
            gameObject.GetComponent<EditNodeCanvasAction>().editNodeAction = editNodeAction;
            return gameObject.GetComponent<EditNodeCanvasAction>();
        }

        /// <summary>
        /// Destroys the component <see cref="AddingNodeCanvasAction"/> if it is
        /// attached to the gameObject.
        /// </summary>
        public void DestroyAddNodeCanvasAction()
        {
            if (gameObject.TryGetComponent(out AddingNodeCanvasAction action))
            {
                NodeCanvasAction.DestroyInstance(action);
            }
        }

        /// <summary>
        /// Destroys the component <see cref="EditNodeCanvasAction"/> if it is
        /// attached to the gameObject.
        /// </summary>
        public void DestroyEditNodeCanvasAction()
        {
            if (gameObject.TryGetComponent(out EditNodeCanvasAction action))
            {
                NodeCanvasAction.DestroyInstance(action);
            }
        }
    }
}