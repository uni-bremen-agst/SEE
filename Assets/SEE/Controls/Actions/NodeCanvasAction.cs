using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// The abstract superclass for the EditNodeCanvasAction and the AddNodeCanvasAction.
    /// </summary>
    public abstract class NodeCanvasAction : MonoBehaviour
    {
        /// <summary>
        /// The gameObject that contains the canvas-prefab instance.
        /// </summary>
        public GameObject Canvas;

        /// <summary>
        /// The gameObject that contains the canvasPrefab.
        /// </summary>
        public GameObject canvasObject;

        /// <summary>
        /// Instantiates the prefab with given <paramref name="prefabPath"/> and saves it 
        /// in <see cref="Canvas"/>.
        /// </summary>
        /// <param name="prefabPath">The path of the prefab</param>
        public void InstantiatePrefab(string prefabPath)
        {
            Canvas = Instantiate(Resources.Load(prefabPath, typeof(GameObject))) as GameObject;
            canvasObject = GameObject.Find("CanvasObject");
        }

        /// <summary>
        /// Destroys <see cref="Canvas"/> of the <paramref name="action"/> and <paramref name="action"/>
        /// itself.
        /// </summary>
        public static void DestroyInstance(NodeCanvasAction action)
        {
            Destroy(action.Canvas);
            Object.Destroy(action);
        }
    }
}
