using System;
using System.Text;
using UnityEngine;
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
            recognizer.Start();
        }

        public void Register(PhraseRecognizedDelegate phraseRecognizedDelegate)
        {
            recognizer.OnPhraseRecognized += phraseRecognizedDelegate;
        }

        public void Unregister(PhraseRecognizedDelegate phraseRecognizedDelegate)
        {
            recognizer.OnPhraseRecognized -= phraseRecognizedDelegate;
        }

        /// <summary>
        /// Shuts down this GrammarInput.
        /// </summary>
        public void Close()
        {
            if (recognizer != null && recognizer.IsRunning)
            {
                recognizer.Stop();
            }
        }
    }
}
