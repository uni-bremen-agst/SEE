using SEE.Controls;
using SEE.GO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SEE.Dissonance
{
    /// <summary>
    /// Controls the input (field) for the Dissonance text chat.
    ///
    /// It must be attached to a game object that has a direct child
    /// named <see cref="canvasName"/> holding the canvas where the chat
    /// is shown. That game object should also have a <see cref="ChatLogController"/>
    /// component attached to it.
    /// </summary>
    /// <remarks>This code stems from a Dissonance demo and was then
    /// adapted to our needs.</remarks>
    public class ChatInputController : ChatController
    {
        #region fields and properties

        /// <summary>
        /// The name of the text channel where to send messages.
        /// </summary>
        private const string targetChannel = "Global";

        /// <summary>
        /// The name of the game object representing the input field for the chat.
        /// The respective game object must be a descendant of the game object holding
        /// the canvas.
        /// </summary>
        private const string chatInputName = "ChatInput";

        /// <summary>
        /// The input field of the text chat. This is the game object named
        /// <see cref="chatInputName"/>. It will be retrieved in <see cref="Start"/>.
        /// </summary>
        private InputField inputField;

        /// <summary>
        /// The controller for the chat log. The log contains the messages being entered so far.
        ///
        /// The <see cref="ChatLogController"/> component must be attached to the same game
        /// object as this component.
        /// </summary>
        private ChatLogController chatLog;
        #endregion

        /// <summary>
        /// Sets up <see cref="Comms"/>, <see cref="inputField"/>, and <see cref="chatLog"/>.
        /// Registers <see cref="OnInputEndEdit(string)"/> to be called when the user
        /// has ended his/her input.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            // Find the input field.
            // Note: GetComponentsInChildren recurses into all transitive descendants.
            inputField = GetComponentsInChildren<InputField>(true).SingleOrDefault(a => a.name == chatInputName);
            if (inputField == null)
            {
                Debug.LogError($"Could not find input field named {chatInputName}.\n");
                enabled = false;
                return;
            }
            inputField.gameObject.SetActive(false);
            inputField.onEndEdit.AddListener(OnInputEndEdit);

            // Find the chat log.
            if (!gameObject.TryGetComponentOrLog(out chatLog))
            {
                enabled = false;
                return;
            }
        }

        /// <summary>
        /// Broadcasts the <paramref name="message"/> to all clients, then disables
        /// the <see cref="inputField"/>, hides the <see cref="chatLog"/>, and
        /// re-enables <see cref="SEEInput.KeyboardShortcutsEnabled"/>.
        ///
        /// This method is a callback that is called when the user has ended his/her input.
        /// </summary>
        /// <param name="message">the message entered by the user</param>
        private void OnInputEndEdit(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                // Send the text to the Dissonance network.
                Comms.Text.Send(targetChannel, message);

                // Display in the local log.
                chatLog.AddMessage($"Me ({targetChannel}): {message}", Color.blue);
            }

            // Clear the UI.
            inputField.text = "";
            inputField.gameObject.SetActive(false);

            // Stop forcing the chat visible.
            chatLog.ForceShow = false;
            SEEInput.KeyboardShortcutsEnabled = true;
        }

        /// <summary>
        /// If the user requests to open the text chat, we will do so.
        /// </summary>
        private void Update()
        {
            if (SEEInput.OpenTextChat())
            {
                ShowTextInput();
            }
        }

        /// <summary>
        /// Disables <see cref="SEEInput.KeyboardShortcutsEnabled"/> and activates
        /// the <see cref="inputField"/> and <see cref="chatLog"/> so that the user
        /// can add a message.
        /// </summary>
        private void ShowTextInput()
        {
            SEEInput.KeyboardShortcutsEnabled = false;
            inputField.gameObject.SetActive(true);
            inputField.ActivateInputField();
            EnableCanvas(true);
            EventSystem.current.SetSelectedGameObject(inputField.gameObject);

            // Force the chat log to show
            chatLog.ForceShow = true;
        }
    }
}
