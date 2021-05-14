using System;
using System.Text;
using UnityEngine;
using UnityEngine.Windows.Speech;

namespace SEE.Controls
{
    /// <summary>
    /// Speech input based on a predefined set of keywords.
    /// Only this fixed set of keywords can be detected.
    /// </summary>
    public class KeywordInput : SpeechInput
    {
        /// <summary>
        /// The keywords to be recognized.
        /// </summary>
        private string[] keywords = new string[] { "up", "down", "left", "right" };

        /// <summary>
        /// The recognizer for the <see cref="keywords"/>.
        /// </summary>
        private KeywordRecognizer recognizer;

        /// <summary>
        /// Starts the <see cref="recognizer"/>.
        /// </summary>
        public override void Start()
        {
            recognizer = new KeywordRecognizer(keywords);
            recognizer.OnPhraseRecognized += OnPhraseRecognized;
            recognizer.Start();
        }

        private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0} ({1}){2}", args.text, args.confidence, Environment.NewLine);
            builder.AppendFormat("\tTimestamp: {0}{1}", args.phraseStartTime, Environment.NewLine);
            builder.AppendFormat("\tDuration: {0} seconds{1}", args.phraseDuration.TotalSeconds, Environment.NewLine);
            Debug.Log(builder.ToString());
        }

        /// <summary>
        /// Shuts down <see cref="recognizer"/>.
        /// Called by Unity when the application closes.
        /// </summary>
        public override void Stop()
        {
            if (recognizer != null && recognizer.IsRunning)
            {
                recognizer.OnPhraseRecognized -= OnPhraseRecognized;
                recognizer.Stop();
            }
        }

        /// <summary>
        /// Stops and disposes the recognizer. It cannot be re-started again.
        /// </summary>
        public override void Dispose()
        {
            Stop();
            recognizer?.Dispose();
            recognizer = null;
        }
    }
}
