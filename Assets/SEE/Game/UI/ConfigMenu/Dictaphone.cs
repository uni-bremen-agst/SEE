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
#if !UNITY_ANDROID
using Michsky.UI.ModernUIPack;
using SEE.Controls;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;

namespace SEE.Game.UI.ConfigMenu
{
    /// <summary>
    /// This is the wrapper script for the dictaphone object that utilizes the
    /// MS Windows speech API to take user input via voice.
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

        /// <summary>
        /// A dictation input shared by all instances of this class.
        /// </summary>
        private static DictationInput dictationInput;

        private ButtonManagerBasicIcon button;
        private Image buttonImage;

        private bool currentlyDictating;
        private Color initialColor;
        private void Awake()
        {
            MustGetComponent(out button);
            MustGetComponent(out buttonImage);
        }
        private void Start()
        {
            button.clickEvent.AddListener(ToggleDictation);
            initialColor = buttonImage.color;
        }
        private void ToggleDictation()
        {
            if (currentlyDictating)
            {
                StopDictation();
            }
            else
            {
                StartDictation();
            }
        }
        private void DictationResultCallBack(string text, ConfidenceLevel confidence)
        {
            OnDictationFinished?.Invoke(text);
        }
        private void StartDictation()
        {
            currentlyDictating = true;
            buttonImage.color = RecordingColor;
            if (dictationInput == null)
            {
                dictationInput = new DictationInput();
            }
            dictationInput.Register(DictationResultCallBack);
            dictationInput.Start();
        }
        private void StopDictation()
        {
            currentlyDictating = false;
            buttonImage.color = initialColor;
            dictationInput.Unregister(DictationResultCallBack);
            dictationInput.Stop();
        }
    }
}
#endif