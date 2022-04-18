using System.Linq;
using JetBrains.Annotations;
using SEE.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace Dissonance.Demo
{
    /// <summary>
    /// Controls the input to the Dissonance voice chat. This component is attached
    /// to the ChatBox game object of the chat canvas.
    /// </summary>
    public class ChatInputController : MonoBehaviour
    {
        /// <summary>
        /// Whether we are currently allowing input to the chat text field.
        /// </summary>
        private bool isInputtingText;

        /// <summary>
        /// Name of the channel at which to send the text messages.
        /// </summary>
        private string targetChannel;

        /// <summary>
        /// The dissonance communication retrieved from the scene.
        /// </summary>
        public DissonanceComms Comms;

        /// <summary>
        /// The name of the global channel.
        /// </summary>
        private const string GlobalChannel = "Global";

        /// <summary>
        /// The name of the channel for team 1.
        /// </summary>
        public string Team1Channel = "A";

        /// <summary>
        /// The name of the channel for team 2.
        /// </summary>
        public string Team2Channel = "B";

        /// <summary>
        /// The name of the game object holding the <see cref="input"/> text field of the chat.
        /// </summary>
        private const string ChatInputGameObjectName = "ChatInput";

        /// <summary>
        /// The text input field where the text messages are entered. It will be retrieved
        /// from a game object named <see cref="ChatInputGameObjectName"/> in <see cref="Start"/>.
        /// </summary>
        private InputField input;

        /// <summary>
        /// The logger for the chat. This component must be attached to the same game object
        /// this component is attached to.
        /// </summary>
        private ChatLogController log;

        /// <summary>
        /// Sets <see cref="Comms"/>, <see cref="input"/>, and <see cref="log"/>.
        /// </summary>
        private void Start ()
        {
            Comms = Comms ?? FindObjectOfType<DissonanceComms>();

            input = GetComponentsInChildren<InputField>().Single(a => a.name == ChatInputGameObjectName);
            input.gameObject.SetActive(false);

            input.onEndEdit.AddListener(OnInputEndEdit);

            log = GetComponent<ChatLogController>();
        }

        /// <summary>
        /// Call back that is called when editing of the <see cref="input"/> field
        /// has been ended (event <see cref="input.onEndEdit"/>).
        /// </summary>
        /// <param name="message">the entered message</param>
        private void OnInputEndEdit([CanBeNull] string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                // Send the text to dissonance network.
                Comms?.Text.Send(targetChannel, message);

                // Display in the local log.
                log?.AddMessage(string.Format("Me ({0}): {1}", targetChannel, message), Color.gray);
            }

            // Clear the UI.
            input.text = "";
            input.gameObject.SetActive(false);
            EnableEditing(false);

            // Stop forcing the chat visible.
            if (log != null)
            {
                log.ForceShow = false;
            }
        }

        /// <summary>
        /// Monitors the keyboard keys for toggling the voice chats if we're not inputting text
        /// (<see cref="isInputtingText"/>) and calls <see cref="ShowTextInput(string)"/> with
        /// the activated channel.
        /// </summary>
        private void Update ()
        {
            if (!isInputtingText)
            {
                if (SEEInput.ToggleGlobalChat())
                {
                    ShowTextInput(GlobalChannel);
                }
                else if (SEEInput.ToggleTeam1Channel())
                {
                    ShowTextInput(Team1Channel);
                }
                else if (SEEInput.ToggleTeam2Channel())
                {
                    ShowTextInput(Team2Channel);
                }
            }
        }

        /// <summary>
        /// Enables entering text into the chat and sends it to the
        /// the given <paramref name="channel"/>.
        /// </summary>
        /// <param name="channel">name of the channel</param>
        private void ShowTextInput(string channel)
        {
            EnableEditing(true);
            targetChannel = channel;
            input.gameObject.SetActive(true);
            input.ActivateInputField();

            // Force the chat log to show.
            if (log != null)
            {
                log.ForceShow = true;
            }
        }

        /// <summary>
        /// Enables/disables editing and disables/enables the keyboard shortcuts of <see cref="SEEInput"/>.
        /// </summary>
        /// <param name="enable">whether editing should be enabled and the <see cref="SEEInput"/>
        /// keyboard shortcuts should be disabled</param>
        private void EnableEditing(bool enable)
        {
            isInputtingText = enable;
            SEEInput.KeyboardShortcutsEnabled = !enable;
        }
    }
}
