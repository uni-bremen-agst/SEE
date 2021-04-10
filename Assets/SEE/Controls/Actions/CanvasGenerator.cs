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
        /// Instantiates the EditNodeCanvasScript and adds it to the CanvasObject gameObject.
        /// </summary>
        /// <returns>the added EditNodeCanvasScript</returns>
        public EditNodeCanvasAction InstantiateEditNodeCanvas(EditNodeAction editNodeAction)
        {
            EditNodeCanvasAction result = gameObject.AddComponent<EditNodeCanvasAction>();
            result.editNodeAction = editNodeAction;
            return result;
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