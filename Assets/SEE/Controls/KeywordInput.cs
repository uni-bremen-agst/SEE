#if UNITY_ANDROID
#else
using UnityEngine.Windows.Speech;
using static UnityEngine.Windows.Speech.PhraseRecognizer;

namespace SEE.Controls
{
    /// <summary>
    /// Speech input based on a predefined set of keywords.
    /// Only this fixed set of keywords can be detected.
    /// 
    /// KeywordInput listens to speech input and attempts to match 
    /// uttered phrases to a list of registered keywords.
    /// There can be many KeywordInputs active at any given time, 
    /// but no two KeywordInputs may be listening for the same keyword.
    /// 
    /// As an example on how to use this class, take a look at the test
    /// case TestKeywordInput.
    /// 
    /// IMPORTANT NOTE.
    /// Dictation (DictationInput) and phrase recognition (KeywordInput or GrammarInput) cannot be 
    /// handled at the same time. If a GrammarInput or KeywordInput is active, a DictationInput 
    /// cannot be active and vice versa.
    /// </summary>
    public class KeywordInput : SpeechInput
    {
        /// <summary>
        /// Constructor allowing to pass the list of keywords to be recognized.
        /// </summary>
        /// <param name="keywords">keywords to be recognized</param>
        public KeywordInput(string[] keywords)
        {
            recognizer = new KeywordRecognizer(keywords);
        }

        /// <summary>
        /// The recognizer for the <see cref="keywords"/>.
        /// </summary>
        private KeywordRecognizer recognizer;

        /// <summary>
        /// Starts the <see cref="recognizer"/>.
        /// </summary>
        public override void Start()
        {
            recognizer.Start();
        }

        /// <summary>
        /// Registers <paramref name="phraseRecognizedDelegate"/> as a callback to
        /// be called when one of the keywords was recognized.
        /// </summary>
        /// <param name="phraseRecognizedDelegate">delegate to be registered</param>
        public void Register(PhraseRecognizedDelegate phraseRecognizedDelegate)
        {
            recognizer.OnPhraseRecognized += phraseRecognizedDelegate;
        }

        /// <summary>
        /// Unregisters <paramref name="phraseRecognizedDelegate"/> as a callback formerly to
        /// be called when one of the keywords was recognized.
        /// </summary>
        /// <param name="phraseRecognizedDelegate">delegate to be unregistered</param>
        public void Unregister(PhraseRecognizedDelegate phraseRecognizedDelegate)
        {
            recognizer.OnPhraseRecognized -= phraseRecognizedDelegate;
        }

        /// <summary>
        /// Shuts down <see cref="recognizer"/>.
        /// Called by Unity when the application closes.
        /// </summary>
        public override void Stop()
        {
            if (recognizer != null && recognizer.IsRunning)
            {
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
#endif
