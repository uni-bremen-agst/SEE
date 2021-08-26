using UnityEngine.Windows.Speech;
using static UnityEngine.Windows.Speech.PhraseRecognizer;

namespace SEE.Controls
{
    /// <summary>
    /// Speech input based on a Speech Recognition Grammar Specification (SRGS).
    /// The GrammarInput is a complement to the KeywordInput. In many cases
    /// developers will find the KeywordInput fills all their development needs.
    /// However, in some cases, more complex grammars will be better expressed in the
    /// form of an XML file on disk. The GrammarInput uses Extensible Markup
    /// Language (XML) elements and attributes, as specified in the World Wide Web
    /// Consortium (W3C) Speech Recognition Grammar Specification (SRGS) Version 1.0.
    /// These XML elements and attributes represent the rule structures that define
    /// the words or phrases (commands) recognized by speech recognition engines.
    ///
    /// Information on this format can be found here http://www.w3.orgspeech-grammarspeech-grammar/,
    /// https://www.w3.org/TR/speech-grammar/, or on MSDN.
    ///
    /// There can be many grammar recognizers active at any given point in time,
    /// but no two grammar recognizers may use the same grammar file.
    ///
    /// Grammar recognizer is currently functional only on Windows 10.
    /// 
    /// IMPORTANT NOTE.
    /// Dictation (DictationInput) and phrase recognition (KeywordInput or GrammarInput) cannot be 
    /// handled at the same time. If a GrammarInput or KeywordInput is active, a DictationInput 
    /// cannot be active and vice versa.
    /// </summary>
    public class GrammarInput : SpeechInput
    {
        /// <summary>
        /// The grammar recognizer parsing the spoken input based on the given grammar.
        /// </summary>
        private GrammarRecognizer recognizer;

        /// <summary>
        /// Sets up and starts the grammar recognizer.
        /// </summary>
        /// <param name="grammarFilePath">path to the SRGS grammar file</param>
        public GrammarInput(string grammarFilePath)
        {
            recognizer = new GrammarRecognizer(grammarFilePath);
        }

        /// <summary>
        /// Registers the given <paramref name="phraseRecognizedDelegate"/> as 
        /// callback to be called when a phrase was recognized.
        /// </summary>
        /// <param name="phraseRecognizedDelegate">callback to be registered</param>
        public void Register(PhraseRecognizedDelegate phraseRecognizedDelegate)
        {
            recognizer.OnPhraseRecognized += phraseRecognizedDelegate;
        }

        /// <summary>
        /// Unregisters the given <paramref name="phraseRecognizedDelegate"/> as 
        /// callback formerly to be called when a phrase was recognized.
        /// </summary>
        /// <param name="phraseRecognizedDelegate">callback to be unregistered</param>
        public void Unregister(PhraseRecognizedDelegate phraseRecognizedDelegate)
        {
            if (recognizer != null)
            {
                recognizer.OnPhraseRecognized -= phraseRecognizedDelegate;
            }
        }

        /// <summary>
        /// Starts the recognizer.
        /// </summary>
        public override void Start()
        {
            recognizer?.Start();
        }

        /// <summary>
        /// Stops the recognizer. It can be re-started by <see cref="Start"/> again.
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
