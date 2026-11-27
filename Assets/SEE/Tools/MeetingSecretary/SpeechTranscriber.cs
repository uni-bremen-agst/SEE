using System;
using Cysharp.Threading.Tasks;
using SEE.UI;
using SEE.UI.Notification;
using SEE.UserSettings;
using UnityEngine;
using Whisper;
using Whisper.Utils;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Turns utterances into text with the speech recognition Whisper, which runs
    /// locally on this computer. One utterance is transcribed at a time; further
    /// utterances wait in a small queue.
    /// </summary>
    /// <remarks>All members must be called on Unity's main thread. Neither the audio
    /// nor the recognized text is written to disk or to the log.</remarks>
    public class SpeechTranscriber
    {
        /// <summary>
        /// Whether Whisper should run on the graphics card (Metal or Vulkan). If that
        /// is not possible on this computer, Whisper falls back to the processor.
        /// </summary>
        private const bool useGpu = true;

        /// <summary>
        /// The title of the notifications shown to the user.
        /// </summary>
        private const string notificationTitle = "Transcription";

        /// <summary>
        /// An utterance waiting to be transcribed.
        /// </summary>
        private readonly struct PendingUtterance
        {
            /// <summary>
            /// The mono samples at <see cref="AudioResampler.TargetSampleRate"/>.
            /// </summary>
            public readonly float[] Samples;

            /// <summary>
            /// When the utterance was spoken (UTC).
            /// </summary>
            public readonly DateTime SpokenAt;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="samples">The mono samples at <see cref="AudioResampler.TargetSampleRate"/>.</param>
            /// <param name="spokenAt">When the utterance was spoken (UTC).</param>
            public PendingUtterance(float[] samples, DateTime spokenAt)
            {
                Samples = samples;
                SpokenAt = spokenAt;
            }
        }

        /// <summary>
        /// The utterances waiting to be transcribed.
        /// </summary>
        private readonly UtteranceQueue<PendingUtterance> queue = new();

        /// <summary>
        /// The loaded Whisper model; null if this transcriber is not started.
        /// </summary>
        private WhisperWrapper whisper;

        /// <summary>
        /// The settings of Whisper used for each transcription.
        /// </summary>
        private WhisperParams parameters;

        /// <summary>
        /// True while the model is being downloaded or loaded.
        /// </summary>
        private bool starting;

        /// <summary>
        /// True while <see cref="ProcessQueueAsync"/> is running.
        /// </summary>
        private bool processing;

        /// <summary>
        /// Counts the calls of <see cref="Stop"/>. Lets <see cref="StartAsync(Transcription)"/>
        /// notice that it was stopped while it was waiting for the model.
        /// </summary>
        private int stopCount;

        /// <summary>
        /// Called on Unity's main thread for each transcribed utterance with its text
        /// and the time (UTC) at which it was spoken.
        /// </summary>
        public event Action<string, DateTime> OnTranscribed;

        /// <summary>
        /// True if the model is loaded and utterances can be transcribed.
        /// </summary>
        public bool IsRunning => whisper != null;

        /// <summary>
        /// Loads the model named in <paramref name="settings"/>. The model is downloaded
        /// first if it is not yet on this computer. If the model is not available,
        /// the user is notified.
        /// </summary>
        /// <param name="settings">The language and the model to be used.</param>
        /// <returns>True if this transcriber is running afterwards.</returns>
        public async UniTask<bool> StartAsync(Transcription settings)
        {
            if (IsRunning)
            {
                return true;
            }
            if (starting)
            {
                return false;
            }

            starting = true;
            int stopCountAtStart = stopCount;
            try
            {
                string path = await WhisperModelStore.GetModelPathAsync(settings.ModelFileName);
                if (path == null)
                {
                    return false;
                }

                // By default, the Whisper package logs every recognized text. What the
                // players say must not end up in the log file.
                LogUtils.Level = LogLevel.Warning;

                WhisperContextParams context = WhisperContextParams.GetDefaultParams();
                context.UseGpu = useGpu;

                WhisperWrapper loaded;
                using (LoadingSpinner.ShowIndeterminate("Loading speech model..."))
                {
                    loaded = await WhisperWrapper.InitFromFileAsync(path, context);
                }
                await UniTask.SwitchToMainThread();

                if (loaded == null)
                {
                    ShowNotification.Error(notificationTitle,
                                           $"The speech model {settings.ModelFileName} could not be loaded. "
                                           + $"If the problem persists, delete the file in {WhisperModelStore.Folder}.");
                    return false;
                }
                if (stopCount != stopCountAtStart)
                {
                    // Stopped while the model was loading: the model is not kept.
                    return false;
                }

                TranscriptionLanguage language = settings.Language;
                if (language != TranscriptionLanguage.English && !loaded.IsMultilingual)
                {
                    Debug.LogWarning($"The speech model {settings.ModelFileName} understands English only.\n");
                }

                parameters = WhisperParams.GetDefaultParams();
                parameters.Language = language.ToWhisperCode();
                // Each utterance is transcribed on its own, so that a wrong
                // recognition cannot influence the following utterances.
                parameters.NoContext = true;
                whisper = loaded;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ShowNotification.Error(notificationTitle, $"The speech recognition could not be started: {e.Message}");
                return false;
            }
            finally
            {
                starting = false;
            }
        }

        /// <summary>
        /// Stops transcribing. Waiting utterances are discarded, and the model is
        /// released so that its memory can be reclaimed.
        /// </summary>
        public void Stop()
        {
            stopCount++;
            // The Whisper package has no method to free a model; it is freed
            // once it is no longer referenced.
            whisper = null;
            parameters = null;
            queue.Clear();
        }

        /// <summary>
        /// Queues an utterance for transcription. If too many utterances are
        /// waiting already, the oldest one is dropped.
        /// </summary>
        /// <param name="samples">The mono samples of the utterance.</param>
        /// <param name="sampleRate">The sample rate of <paramref name="samples"/> in Hz.</param>
        public void Enqueue(float[] samples, int sampleRate)
        {
            if (!IsRunning)
            {
                return;
            }

            float[] resampled = AudioResampler.ToTargetSampleRate(samples, sampleRate);
            if (queue.Enqueue(new PendingUtterance(resampled, DateTime.UtcNow)))
            {
                Debug.LogWarning("The speech recognition is too slow for the amount of speech. "
                                 + $"An utterance was skipped ({queue.Dropped} so far).\n");
            }

            if (!processing)
            {
                ProcessQueueAsync().Forget();
            }
        }

        /// <summary>
        /// Transcribes the waiting utterances one after another until none is left.
        /// </summary>
        private async UniTaskVoid ProcessQueueAsync()
        {
            processing = true;
            try
            {
                while (IsRunning && queue.TryDequeue(out PendingUtterance utterance))
                {
                    WhisperWrapper usedModel = whisper;
                    // Whisper computes on a background thread.
                    WhisperResult result = await usedModel.GetTextAsync(utterance.Samples, AudioResampler.TargetSampleRate, 1, parameters);
                    await UniTask.SwitchToMainThread();

                    if (whisper != usedModel)
                    {
                        // Stopped in the meantime: the result is no longer wanted.
                        continue;
                    }
                    if (result == null)
                    {
                        Debug.LogWarning("The speech recognition failed for an utterance.\n");
                        continue;
                    }

                    string text = result.Result.Trim();
                    if (!HallucinationFilter.IsHallucination(text))
                    {
                        OnTranscribed?.Invoke(text, utterance.SpokenAt);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                processing = false;
            }
        }
    }
}
