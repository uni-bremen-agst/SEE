using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Windows.Speech;
using NUnit.Framework;
using System.Linq;

namespace SEE.Controls
{
    /// <summary>
    /// Test cases for <see cref="KeywordInput"/>. 
    /// 
    /// Note: This test is not automated. It expects that a human tester uses
    /// a microphone to talk. The recognized input is shown on the console
    /// for inspection and will also be compared to the expected result.
    /// </summary>
    [Category("NonDeterministic")]
    internal class TestKeywordInput
    {
        /// <summary>
        /// The keywords to be recognized.
        /// </summary>
        private readonly string[] keywords =
        {
            "move up",
            "hi SEE",
            "move down",
            "stop talking"
        };

        [UnityTest]
        public IEnumerator TestDialog()
        {
            LogAssert.ignoreFailingMessages = true;

            KeywordInput input = new KeywordInput(keywords);
            input.Register(OnPhraseRecognized);
            input.Start();

            for (int i = 10; i > 0; i--)
            {
                Debug.Log($"Say one of the keywords {ToString(keywords)} and watch the console output. I am listening for "
                          + "another {i} seconds and report what I understood to the console.\n");
                yield return new WaitForSeconds(1);
            }

            input.Unregister(OnPhraseRecognized);
            input.Dispose();
        }

        private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
        {
            // General information on what was recognized.
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0} ({1}){2}", args.text, args.confidence, Environment.NewLine);
            builder.AppendFormat("\tTimestamp: {0}{1}", args.phraseStartTime, Environment.NewLine);
            builder.AppendFormat("\tDuration: {0} seconds{1}", args.phraseDuration.TotalSeconds, Environment.NewLine);
            Debug.Log(builder.ToString());

            // Make sure whatever was recognized is one of the expected keywords.
            Assert.That(keywords.Any(keyword => args.text == keyword));
        }

        /// <summary>
        /// Returns the concatenation of all <paramref name="values"/> (separated by
        /// a blank). Can be used for debugging.
        /// </summary>
        /// <param name="values">values to be concatenated</param>
        /// <returns>concatenation of all <paramref name="values"/></returns>
        private string ToString(string[] values)
        {
            return string.Join(", ", values.Select(x => $"'{x}'"));
        }
    }
}