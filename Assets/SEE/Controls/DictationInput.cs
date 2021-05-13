using UnityEngine;
using UnityEngine.Windows.Speech;

namespace SEE.Controls
{
    /// <summary>
    /// Speech input for arbitrary spoken text. Unlike <see cref="KeywordInput"/> and
    /// <see cref="GrammarInput"/>, the spoken sentences are not limited to fixed
    /// set of keywords or a particular grammar. The recognization, however, may
    /// be less accurate. This speech recognition may be useful for free texts
    /// in the context of dictation.
    /// </summary>
    public class DictationInput : SpeechInput
    {
        // Dictation recognizer is currently functional only on Windows 10, and requires that dictation 
        // is permitted in the user's Speech privacy policy (Settings->Privacy->Speech, inking & typing). 
        // If dictation is not enabled, DictationRecognizer will fail on Start. Developers can handle this 
        // failure in an app-specific way by providing a DictationError delegate and testing for 
        // SPERR_SPEECH_PRIVACY_POLICY_NOT_ACCEPTED (0x80045509).

        /// <summary>
        /// The recognizer used for dictation.
        /// </summary>
        private DictationRecognizer recognizer;

        /// <summary>
        /// Starts the <see cref="recognizer"/>.
        /// </summary>
        private void Start()
        {
            StartDictationEngine();
        }

        /// <summary>
        /// Event that is triggered when the recognizer changes its hypothesis for the current fragment.
        /// </summary>
        /// <param name="text"></param>
        private void OnDictationHypothesis(string text)
        {
            Debug.Log($"Dictation hypothesis: {text}\n");
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
                    CloseDictationEngine();
                    StartDictationEngine();
                    break;
                case DictationCompletionCause.UnknownError:
                case DictationCompletionCause.AudioQualityFailure:
                case DictationCompletionCause.MicrophoneUnavailable:
                case DictationCompletionCause.NetworkFailure:
                    // Error
                    CloseDictationEngine();
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
        /// Event indicating a phrase has been recognized with the specified <paramref name="confidence"/> level.
        /// </summary>
        /// <param name="text">phrase recognized</param>
        /// <param name="confidence">confidence level of the recognition</param>
        private void OnDictationResult(string text, ConfidenceLevel confidence)
        {
            Debug.Log("Dictation result: " + text);
        }

        /// <summary>
        /// Callback that is triggered when the recognizer session encouters an error.
        /// </summary>
        /// <param name="error"></param>
        /// <param name="hresult"></param>
        private void OnDictationError(string error, int hresult)
        {
            Debug.Log("Dictation error: " + error);
        }

        /// <summary>
        /// Shuts down <see cref="recognizer"/>.
        /// Called by Unity when the application closes.
        /// </summary>
        private void OnApplicationQuit()
        {
            CloseDictationEngine();
        }

        /// <summary>
        /// Starts the <see cref="recognizer"/>.
        /// </summary>
        private void StartDictationEngine()
        {
            recognizer = new DictationRecognizer();

            recognizer.DictationHypothesis += OnDictationHypothesis;
            recognizer.DictationResult += OnDictationResult;
            recognizer.DictationComplete += OnDictationComplete;
            recognizer.DictationError += OnDictationError;

            recognizer.Start();
        }

        /// <summary>
        /// Shuts down the <see cref="recognizer"/>.
        /// </summary>
        private void CloseDictationEngine()
        {
            if (recognizer != null)
            {
                recognizer.DictationHypothesis -= OnDictationHypothesis;
                recognizer.DictationComplete -= OnDictationComplete;
                recognizer.DictationResult -= OnDictationResult;
                recognizer.DictationError -= OnDictationError;

                if (recognizer.Status == SpeechSystemStatus.Running)
                {
                    recognizer.Stop();
                }

                recognizer.Dispose();
            }
        }
    }
}