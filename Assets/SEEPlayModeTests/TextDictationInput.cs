using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Windows.Speech;

namespace SEE.Controls
{
    /// <summary>
    /// Test cases for <see cref="DictationInput"/>.
    ///
    /// Note: This test is not automated. It expects that a human tester uses
    /// a microphone to talk. The recognized input is shown on the console
    /// for inspection.
    /// </summary>
    [Category("NonDeterministic")]
    internal class TextDictationInput
    {
        [UnityTest]
        public IEnumerator TestDialog()
        {
            LogAssert.ignoreFailingMessages = true;

            DictationInput input = new DictationInput();
            input.Register(OnDictationResult);
            input.Register(OnDictationHypothesis);
            input.Start();

            for (int i = 10; i > 0; i--)
            {
                Debug.Log($"Please speak and watch the console output. I am listening for another {i} seconds and report what I understood to the console.\n");
                yield return new WaitForSeconds(1);
            }

            input.Unregister(OnDictationResult);
            input.Unregister(OnDictationHypothesis);
            input.Dispose();
        }

        /// <summary>
        /// Event indicating a phrase has been recognized with the specified <paramref name="confidence"/> level.
        /// </summary>
        /// <param name="text">phrase recognized</param>
        /// <param name="confidence">confidence level of the recognition</param>
        private static void OnDictationResult(string text, ConfidenceLevel confidence)
        {
            Debug.Log($"Dictation result: '{text}' with confidence {confidence}.\n");
        }

        /// <summary>
        /// Event that is triggered when the recognizer changes its hypothesis for the current fragment.
        /// Could be registered for recognizer.DictationHypothesis.
        /// </summary>
        /// <param name="text">the currently understood text (subject to change)</param>
        private static void OnDictationHypothesis(string text)
        {
            Debug.Log($"Dictation hypothesis: '{text}'\n");
        }
    }
}