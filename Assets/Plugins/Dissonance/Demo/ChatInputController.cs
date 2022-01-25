using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Dissonance.Demo
{
    public class ChatInputController
        : MonoBehaviour
    {
        #region fields and properties
        private bool _isInputtingText;
        private string _targetChannel;

        public DissonanceComms Comms;
        public string Team1Channel = "A";
        public string Team2Channel = "B";

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

        private void OnInputEndEdit([CanBeNull] string message)
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
            _isInputtingText = false;

            //Stop forcing the chat visible
            if (_log!= null)
                _log.ForceShow = false;
        }

        public void Update ()
        {
            //Monitor keyboard keys if we're not inputting text
            if (!_isInputtingText)
            {
                var global = Input.GetKey(KeyCode.Y);
                var red = Input.GetKey(KeyCode.U);
                var blue = Input.GetKey(KeyCode.I);

                //If a key is pressed
                if (global)
                    ShowTextInput("Global");
                else if (red)
                    ShowTextInput(Team1Channel);
                else if (blue)
                    ShowTextInput(Team2Channel);
            }
        }

        private void ShowTextInput(string channel)
        {
            _isInputtingText = true;
            _targetChannel = channel;
            _input.gameObject.SetActive(true);
            _input.ActivateInputField();

            //Force the chat log to show
            if (_log != null)
                _log.ForceShow = true;
        }
    }
}
