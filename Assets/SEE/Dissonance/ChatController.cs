using Dissonance;
using UnityEngine;

namespace SEE.Dissonance
{
    public abstract class ChatController : MonoBehaviour
    {
        /// <summary>
        /// The dissonance network to broadcast the messages. Can
        /// be set in the Unity inspector or otherwise will be set
        /// automatically by <see cref="Start"/>.
        /// </summary>
        [Tooltip("The dissonance network to broadcast the messages.")]
        public DissonanceComms Comms;

        /// <summary>
        /// The name of the direct child holding the canvas where the chat
        /// is displayed. The canvas will be disabled when the chat is toggled off.
        ///
        /// </summary>
        private const string canvasName = "Canvas";

        /// <summary>
        /// The game object holding the canvas where the chat is displayed.
        /// It has the name <see cref="canvasName"/> and is a direct child of
        /// the game object this component is attached to.
        /// </summary>
        protected GameObject canvas;

        /// <summary>
        /// Initiliazes the <see cref="canvas"/> and <see cref="Comms"/>.
        /// </summary>
        protected virtual void Start()
        {
            // Find the canvas.
            canvas = transform.Find(canvasName)?.gameObject;
            if (canvas == null)
            {
                Debug.LogError($"Could not find canvas named {canvasName}.\n");
                enabled = false;
                return;
            }

            Comms = Comms ?? FindObjectOfType<DissonanceComms>();
        }

        /// <summary>
        /// Enables or disables the canvas where the chat is displayed.
        /// </summary>
        /// <param name="enable">whether the canvas should be enabled</param>
        protected void EnableCanvas(bool enable)
        {
            canvas.SetActive(enable);
        }
    }
}
