using System;
using System.Text;
using UnityEngine;
using UnityEngine.Windows.Speech;

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
        /// Path to the grammar file.
        /// </summary>
        private readonly string grammarFilePath = Application.streamingAssetsPath + "/PersonalAssistantGrammar.grxml";

        private void Start()
        {
            try
            {
                recognizer = new GrammarRecognizer(grammarFilePath);
                recognizer.OnPhraseRecognized += OnPhraseRecognized;
                recognizer.Start();
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception occured when loading grammar from {grammarFilePath}: {e.Message}\n");
                enabled = false;
            }
        }

        private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
        {            
            Debug.Log($"Phrase detected phrase '{args.text}' with confidence {args.confidence}\n");
            SemanticMeaning[] meanings = args.semanticMeanings;
            if (meanings != null)
            {
                foreach (SemanticMeaning meaning in meanings)
                {
                    Debug.Log($"Meaning: {meaning.key} => {ToString(meaning.values)}\n");
                }
            }
        }

        /// <summary>
        /// Returns the concatenation of all <paramref name="values"/> (separated by
        /// a blank). Can be used for debugging.
        /// </summary>
        /// <param name="values"></param>
        /// <returns>concatenation of all <paramref name="values"/></returns>
        private string ToString(string[] values)
        {           
            StringBuilder builder = new StringBuilder();
            foreach (string value in values)
            {
                builder.Append(value + " ");
            }
            return builder.ToString();
        }

        /// <summary>
        /// Shuts down <see cref="recognizer"/>.
        /// Called by Unity when the application closes.
        /// </summary>
        private void OnApplicationQuit()
        {
            if (recognizer != null && recognizer.IsRunning)
            {
                recognizer.OnPhraseRecognized -= OnPhraseRecognized;
                recognizer.Stop();
            }
        }
    }
}
