using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// The abstract superclass for the EditNodeCanvasAction and the AddNodeCanvasAction.
    /// </summary>
    public abstract class NodeCanvasAction : MonoBehaviour
    {
        /// <summary>
        /// The gameObject that contains the canvas-prefab clone.
        /// </summary>
        public GameObject Canvas;

        /// <summary>
        /// Instantiates a clone of the given prefab and saves it in <see cref="Canvas"/>.
        /// </summary>
        /// <param name="directory">The directory of the prefab</param>
        public void InstantiatePrefab(string directory)
        {
            Canvas = Instantiate(Resources.Load(directory, typeof(GameObject))) as GameObject;
        }

        /// <summary>
        /// Destroys <see cref="Canvas"/> of the <paramref name="action"/> and <paramref name="action"/>
        /// itself.
        /// </summary>
        public static void Destroy(ref NodeCanvasAction action)
        {
            Destroy(action.Canvas);
            UnityEngine.Object.Destroy(action);
            action = null;
        }
    }
}
