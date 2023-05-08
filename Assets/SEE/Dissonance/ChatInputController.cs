using Dissonance;
using Dissonance.Demo;
using SEE.Controls;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Dissonance
{
    /// <summary>
    /// Controls the text chat provided by Dissonance.
    /// </summary>
    /// <remarks>This code stems from a Dissonance demo and was then
    /// adapted to our needs.</remarks>
    public class ChatInputController
        : MonoBehaviour
    {
        #region fields and properties

        /// <summary>
        /// The name of the text channel where to send messages.
        /// </summary>
        private string targetChannel;

        /// <summary>
        /// The dissonance network to broadcast the messages.
        /// </summary>
        public DissonanceComms Comms;

        /// <summary>
        /// The input field of the text chat.
        /// </summary>
        private InputField inputField;

        /// <summary>
        /// The controller for the chat log. The log contains the messages
        /// being entered so far.
        /// </summary>
        private ChatLogController chatLog;
        #endregion

        /// <summary>
        /// Sets up <see cref="Comms"/>, <see cref="inputField, and <see cref="chatLog"/>.
        /// Registers <see cref="OnInputEndEdit(string)"/> to be called when the user
        /// has ended his/her input.
        /// </summary>
        private void Start ()
        {
            Comms = Comms ?? FindObjectOfType<DissonanceComms>();

            inputField = GetComponentsInChildren<InputField>().Single(a => a.name == "ChatInput");
            inputField.gameObject.SetActive(false);

            inputField.onEndEdit.AddListener(OnInputEndEdit);

            chatLog = GetComponent<ChatLogController>();
        }

        /// <summary>
        /// Broadcasts the <paramref name="message"/> to all clients, then disables
        /// the <see cref="inputField"/>, hides the <see cref="chatLog"/>, and
        /// re-enables <see cref="SEEInput.KeyboardShortcutsEnabled"/>.
        ///
        /// This method is a callback alled when the user has ended his/her input.
        /// </summary>
        /// <param name="message">the message entered by the user</param>
        private void OnInputEndEdit(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                // Send the text to dissonance network
                if (Comms != null)
                {
                    Comms.Text.Send(targetChannel, message);
                }

                // Display in the local log
                if (chatLog != null)
                {
                    chatLog.AddMessage(string.Format("Me ({0}): {1}", targetChannel, message), Color.blue);
                }
            }

            // Clear the UI
            inputField.text = "";
            inputField.gameObject.SetActive(false);

            // Stop forcing the chat visible
            if (chatLog != null)
            {
                chatLog.ForceShow = false;
            }
            SEEInput.KeyboardShortcutsEnabled = true;
        }

        /// <summary>
        /// If the user requests to open the text chat, we will do so.
        /// </summary>
        private void Update ()
        {
            if (SEEInput.OpenTextChat())
            {
                ShowTextInput("Global");
            }
        }

        /// <summary>
        /// Disables <see cref="SEEInput.KeyboardShortcutsEnabled"/> and activates
        /// the <see cref="inputField"/> and <see cref="chatLog"/> so that the user
        /// can add a message.
        /// </summary>
        /// <param name="channel">the name of the Dissonance channel where to send messages</param>
        private void ShowTextInput(string channel)
        {
            SEEInput.KeyboardShortcutsEnabled = false;
            targetChannel = channel;
            inputField.gameObject.SetActive(true);
            inputField.ActivateInputField();

            // Force the chat log to show
            if (chatLog != null)
            {
                chatLog.ForceShow = true;
            }
        }
    }
}
