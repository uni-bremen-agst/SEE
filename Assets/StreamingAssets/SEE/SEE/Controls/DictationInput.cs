using UnityEngine;
using UnityEngine.Windows.Speech;
using static UnityEngine.Windows.Speech.DictationRecognizer;

namespace SEE.Controls
{
    /// <summary>
    /// Speech input for arbitrary spoken text. Unlike <see cref="KeywordInput"/> and
    /// <see cref="GrammarInput"/>, the spoken sentences are not limited to fixed
    /// set of keywords or a particular grammar. The recognization, however, may
    /// be less accurate. This speech recognition may be useful for free texts
    /// in the context of dictation.
    /// 
    /// As an example on how to use this class, take a look at the test class
    /// TestDictationInput.
    /// 
    /// IMPORTANT NOTE.
    /// Dictation (DictationInput) and phrase recognition (KeywordInput or GrammarInput) cannot be 
    /// handled at the same time. If a GrammarInput or KeywordInput is active, a DictationInput 
    /// cannot be active and vice versa.
    /// </summary>
    public class DictationInput : SpeechInput
    {
        // Dictation recognizer is currently functional only on Windows 10, and requires that dictation 
        // is permitted in the user's Speech privacy policy (Settings->Privacy->Speech, inking & typing). 
        // If dictation is not enabled, DictationRecognizer will fail on Start. Developers can handle this 
        // failure in an app-specific way by providing a DictationError delegate and testing for 
        // SPERR_SPEECH_PRIVACY_POLICY_NOT_ACCEPTED (0x80045509).

        /// <summary>
        /// Constructor setting up the recognizer.
        /// </summary>
        public DictationInput()
        {
            recognizer = new DictationRecognizer();
        }

        /// <summary>
        /// The recognizer used for dictation.
        /// </summary>
        private DictationRecognizer recognizer;

        /// <summary>
        /// Starts the <see cref="recognizer"/>.
        /// </summary>
        public override void Start()
        {
            recognizer.DictationComplete += OnDictationComplete;
            recognizer.DictationError += OnDictationError;
            recognizer.Start();
        }

        /// <summary>
        /// Stops the <see cref="recognizer"/>.
        /// </summary>
        public override void Stop()
        {
            if (recognizer != null)
            {               
                recognizer.DictationComplete -= OnDictationComplete;
                recognizer.DictationError -= OnDictationError;

                if (recognizer.Status == SpeechSystemStatus.Running)
                {
                    recognizer.Stop();
                }
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

        /// <summary>
        /// Registers <paramref name="dictationResultDelegate"/> as a callback
        /// to be called when a phrase was recognized to report the result.
        /// </summary>
        /// <param name="dictationResultDelegate">delegate to be registered</param>
        public void Register(DictationResultDelegate dictationResultDelegate)
        {
            recognizer.DictationResult += dictationResultDelegate;
        }

        /// <summary>
        /// Unregisters <paramref name="dictationResultDelegate"/> as a callback
        /// formerly to be called when a phrase was recognized to report the result.
        /// </summary>
        /// <param name="dictationResultDelegate">delegate to be unregistered</param>
        public void Unregister(DictationResultDelegate dictationResultDelegate)
        {
            recognizer.DictationResult -= dictationResultDelegate;
        }

        /// <summary>
        /// Registers <paramref name="dictationHypothesisDelegate"/> as a callback
        /// to be called when there is a hypothesis on the phrase currently being 
        /// recognized. The hypothesis is subject to change depending upon the
        /// following input. The final result will be provided by a 
        /// DictationResultDelegate later.
        /// </summary>
        /// <param name="dictationHypothesisDelegate">delegate to be registered</param>
        public void Register(DictationHypothesisDelegate dictationHypothesisDelegate)
        {
            recognizer.DictationHypothesis += dictationHypothesisDelegate;
        }

        /// <summary>
        /// Unregisters <paramref name="dictationHypothesisDelegate"/> as a callback
        /// formerly to be called when there is a hypothesis on the phrase currently being 
        /// recognized.
        /// </summary>
        /// <param name="dictationHypothesisDelegate">delegate to be unregistered</param>
        public void Unregister(DictationHypothesisDelegate dictationHypothesisDelegate)
        {
            recognizer.DictationHypothesis -= dictationHypothesisDelegate;
        }

        /// <summary>
        /// Callback that is triggered when the recognizer session completes.
        /// </summary>
        /// <param name="completionCause">the reason for completion</param>
        private void OnDictationComplete(DictationCompletionCause completionCause)
        {
            ReportCompletion(completionCause);
            switch (completionCause)
            {
                case DictationCompletionCause.TimeoutExceeded:
                case DictationCompletionCause.PauseLimitExceeded:
                case DictationCompletionCause.Canceled:
                case DictationCompletionCause.Complete:
                    // Restart required
                    Stop();
                    Start();
                    break;
                case DictationCompletionCause.UnknownError:
                case DictationCompletionCause.AudioQualityFailure:
                case DictationCompletionCause.MicrophoneUnavailable:
                case DictationCompletionCause.NetworkFailure:
                    // Error
                    Stop();
                    break;
            }
        }

        /// <summary>
        /// Reports the given reason <paramref name="completionCause"/> why the dictation
        /// was completed to the user. If that value is <see cref="DictationCompletionCause.Complete"/>,
        /// nothing happens.
        /// </summary>
        /// <param name="completionCause">the reason why the dictation was completed</param>
        private void ReportCompletion(DictationCompletionCause completionCause)
        {
            switch (completionCause)
            {
                case DictationCompletionCause.Complete:
                    // All is fine, nothing to report.
                    break;
                case DictationCompletionCause.AudioQualityFailure:
                    Debug.LogError("Dictation session completion was caused by bad audio quality.\n");
                    break;
                case DictationCompletionCause.Canceled:
                    Debug.LogError("Dictation session completion was caused by bad audio quality.\n");
                    break;
                case DictationCompletionCause.TimeoutExceeded:
                    Debug.LogError("Dictation session has reached its timeout.\n");
                    break;
                case DictationCompletionCause.NetworkFailure:
                    Debug.LogError("Dictation session has finished because network connection was not available.\n");
                    break;
                case DictationCompletionCause.MicrophoneUnavailable:
                    Debug.LogError("Dictation session has finished because a microphone was not available.\n");
                    break;
                case DictationCompletionCause.UnknownError:
                    Debug.LogError("Dictation session has completed due to an unknown error.\n");
                    break;
                default:
                    Debug.LogError("Dictation session has completed due to an unknown error.\n");
                    break;
            }
        }

        /// <summary>
        /// Callback that is triggered when the recognizer session encouters an error.
        /// </summary>
        /// <param name="error">the error message</param>
        /// <param name="hresult">HRESULT code that corresponds to the error</param>
        private void OnDictationError(string error, int hresult)
        {
            Debug.Log($"Dictation error: {error} with hresult code: {hresult}.\n");
        }
    }
}