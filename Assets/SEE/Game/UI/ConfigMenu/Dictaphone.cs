// Copyright 2021 Ruben Smidt
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

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
