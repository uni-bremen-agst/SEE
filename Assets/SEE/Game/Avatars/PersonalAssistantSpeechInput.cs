using SEE.Controls;
using SEE.GO;
using SEE.Utils;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Windows.Speech;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// A component to be attached to the personal assistant to enable speech
    /// recognition to control it.
    /// </summary>
    public class PersonalAssistantSpeechInput : MonoBehaviour
    {
        /// <summary>
        /// Path to the SRGS grammar file. The grammar is expected to define the
        /// following semantics: help, time, about.
        /// </summary>
        [Tooltip("Path to the SRGS grammar file defining the speech input language.")]
        public FilePath GrammarFilePath;

        /// <summary>
        /// The grammar recognizer used to interpret the speech input.
        /// </summary>
        private GrammarInput input;

        /// <summary>
        /// The brain of the personal assistant.
        /// </summary>
        private PersonalAssistantBrain brain;

        /// <summary>
        /// Sets up the grammar <see cref="input"/> and registers the callback
        /// <see cref="OnPhraseRecognized(PhraseRecognizedEventArgs)"/>.
        /// </summary>
        private void Start()
        {
            if (string.IsNullOrEmpty(GrammarFilePath.Path))
            {
                Debug.LogError("Grammar file for speech recognition is not defined.\n");
                enabled = false;
                return;
            }

            if (!File.Exists(GrammarFilePath.Path))
            {
                Debug.LogError($"Grammar file {GrammarFilePath} for speech recognition does not exist.\n");
                enabled = false;
                return;
            }

            try
            {
                input = new GrammarInput(GrammarFilePath.Path);
                input.Register(OnPhraseRecognized);
                input.Start();
                if (!gameObject.TryGetComponentOrLog(out brain))
                {
                    enabled = false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failure in starting speech recognition with grammar file {GrammarFilePath}: {e.Message}\n");
                enabled = false;
            }
        }

        /// <summary>
        /// Callback that is called by the <see cref="input"/> recognizer when a
        /// sentence was recognized.
        /// </summary>
        /// <param name="args">details about the recognized sentence</param>
        private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
        {
            // Debug.Log($"Detected phrase '{args.text}' with confidence {args.confidence}\n");
            SemanticMeaning[] meanings = args.semanticMeanings;
            if (meanings != null)
            {
                foreach (SemanticMeaning meaning in meanings)
                {
                    // Debug.Log($"Meaning: {meaning.key} => {ToString(meaning.values)}\n");
                    foreach (string value in meaning.values)
                    {
                        // data, help, time, about, goodBye
                        if (value == "data")
                        {
                            brain.Overview();
                        }
                        else if (value == "interact")
                        {
                            brain.Interaction();
                        }
                        else if (value == "time")
                        {
                            brain.CurrentTime();
                        }
                        else if (value == "about")
                        {
                            brain.About();
                        }
                        else if (value == "goodBye")
                        {
                            brain.GoodBye();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the concatenation of all <paramref name="values"/> (separated by
        /// a blank). Can be used for debugging.
        /// </summary>
        /// <param name="values">values to be concatenated</param>
        /// <returns>concatenation of all <paramref name="values"/></returns>
        private string ToString(string[] values)
        {
            return string.Join(", ", values);
        }

        /// <summary>
        /// Shuts down <see cref="input"/>.
        /// Called by Unity when the application closes.
        /// </summary>
        private void OnApplicationQuit()
        {
            OnDisable();
            input?.Dispose();
        }

        /// <summary>
        /// Re-starts <see cref="input"/>.
        /// </summary>
        private void OnEnable()
        {
            input?.Start();
            input?.Register(OnPhraseRecognized);
        }

        /// <summary>
        /// Stops <see cref="input"/>.
        /// </summary>
        private void OnDisable()
        {
            input?.Stop();
            input?.Unregister(OnPhraseRecognized);
        }
    }
}
