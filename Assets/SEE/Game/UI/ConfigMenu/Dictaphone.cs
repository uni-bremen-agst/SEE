using Michsky.UI.ModernUIPack;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
namespace SEE.Game.UI.ConfigMenu
{
    /// <summary>
    /// This is the wrapper script for the dictaphone object that utilizes the windows speech api
    /// to take user input via voice.
    /// </summary>
    [RequireComponent(typeof(ButtonManagerBasicIcon))]
    public class Dictaphone : DynamicUIBehaviour
    {
        /// <summary>
        /// The event handler that gets invoked when a dictation is finished.
        /// A dictation is usually finished manually by a user.
        /// </summary>
        public delegate void OnDictationFinishedEventHandler(string recordedText);
        public event OnDictationFinishedEventHandler OnDictationFinished;

        private static readonly Color RecordingColor = new Color(0.94f, 0.27f, 0.27f);

        private DictationRecognizer _dictationRecognizer;
        private ButtonManagerBasicIcon _button;
        private Image _buttonImage;

        private bool _currentlyDictating;
        private Color _initialColor;

        void Awake()
        {
            MustGetComponent(out _button);
            MustGetComponent(out _buttonImage);
        }

        void Start()
        {
            _dictationRecognizer = new DictationRecognizer();
            _dictationRecognizer.DictationResult +=
                (text, confidence) => OnDictationFinished?.Invoke(text);

            _button.clickEvent.AddListener(ToggleDictation);
            _initialColor = _buttonImage.color;
        }

        private void ToggleDictation()
        {
            if (_currentlyDictating)
            {
                StopDictation();
            }
            else
            {
                StartDictation();
            }
        }

        private void StartDictation()
        {
            _currentlyDictating = true;
            _buttonImage.color = RecordingColor;
            _dictationRecognizer.Start();
        }

        private void StopDictation()
        {
            _currentlyDictating = false;
            _buttonImage.color = _initialColor;
            _dictationRecognizer.Stop();
        }
    }
}
