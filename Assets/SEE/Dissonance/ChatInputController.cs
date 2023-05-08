using SEE.Controls;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Dissonance.Demo
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
        private string _targetChannel;

        public DissonanceComms Comms;

        private InputField _input;
        private ChatLogController _log;
        #endregion

        public void Start ()
        {
            Comms = Comms ?? FindObjectOfType<DissonanceComms>();

            _input = GetComponentsInChildren<InputField>().Single(a => a.name == "ChatInput");
            _input.gameObject.SetActive(false);

            _input.onEndEdit.AddListener(OnInputEndEdit);

            _log = GetComponent<ChatLogController>();
        }

        private void OnInputEndEdit(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                //Send the text to dissonance network
                if (Comms != null)
                    Comms.Text.Send(_targetChannel, message);

                //Display in the local log
                if (_log != null)
                    _log.AddMessage(string.Format("Me ({0}): {1}", _targetChannel, message), Color.gray);
            }

            //Clear the UI
            _input.text = "";
            _input.gameObject.SetActive(false);

            //Stop forcing the chat visible
            if (_log != null)
            {
                _log.ForceShow = false;
            }
            SEEInput.KeyboardShortcutsEnabled = true;
        }

        public void Update ()
        {
            if (SEEInput.OpenTextChat())
            {
                ShowTextInput("Global");
            }
        }

        private void ShowTextInput(string channel)
        {
            SEEInput.KeyboardShortcutsEnabled = false;
            _targetChannel = channel;
            _input.gameObject.SetActive(true);
            _input.ActivateInputField();

            //Force the chat log to show
            if (_log != null)
                _log.ForceShow = true;
        }
    }
}
