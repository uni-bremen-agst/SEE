using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SEE.Controls.KeyActions;
using SEE.Net;
using SEE.Net.Actions;
using SEE.UI.Notification;
using SEE.UserSettings;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Transcribes what the local player says in the voice chat and shows it
    /// to all players as text.
    ///
    /// The transcription is off at every start and is turned on and off by the
    /// local player with a keyboard shortcut. Only the player's own microphone
    /// is transcribed, and only while the player is not muted. The audio is
    /// processed on this computer and kept in memory only.
    /// </summary>
    /// <remarks>This component is attached to the local desktop player by
    /// <see cref="SEE.Game.Avatars.AvatarAdapter"/>.</remarks>
    public class MeetingSecretaryController : MonoBehaviour
    {
        /// <summary>
        /// The title of the notifications shown to the user.
        /// </summary>
        private const string notificationTitle = "Transcription";

        /// <summary>
        /// The minimal time (in seconds) between two warnings about lost audio.
        /// </summary>
        private const float warningIntervalSeconds = 10f;

        /// <summary>
        /// The samples read from <see cref="audioSource"/> in one go.
        /// </summary>
        private readonly float[] readBuffer = new float[16384];

        /// <summary>
        /// Turns utterances into text.
        /// </summary>
        private readonly SpeechTranscriber transcriber = new();

        /// <summary>
        /// Provides the audio of the local microphone; null while the transcription is off.
        /// </summary>
        private ITranscriptAudioSource audioSource;

        /// <summary>
        /// Cuts the audio into utterances; null while the transcription is off.
        /// </summary>
        private UtteranceSegmenter segmenter;

        /// <summary>
        /// True while the transcription is on.
        /// </summary>
        private bool isOn;

        /// <summary>
        /// True while the transcription is being turned on, which may take a while
        /// if the speech model must be downloaded.
        /// </summary>
        private bool isTurningOn;

        /// <summary>
        /// The value of <see cref="ITranscriptAudioSource.DroppedSamples"/> at the last warning.
        /// </summary>
        private long reportedDroppedSamples;

        /// <summary>
        /// The earliest time (<see cref="Time.unscaledTime"/>) of the next warning about lost audio.
        /// </summary>
        private float nextWarningTime;

        /// <summary>
        /// Registers for the texts recognized by the <see cref="transcriber"/>.
        /// </summary>
        private void Awake()
        {
            transcriber.OnTranscribed += OnTranscribed;
        }

        /// <summary>
        /// Turns the transcription off when the local player leaves.
        /// </summary>
        private void OnDestroy()
        {
            transcriber.OnTranscribed -= OnTranscribed;
            if (isOn)
            {
                TurnOff(leaving: true);
            }
            else
            {
                // The model may still be loading.
                transcriber.Stop();
            }
        }

        /// <summary>
        /// Toggles the transcription if the user requests it and processes the
        /// audio of the microphone while the transcription is on.
        /// </summary>
        private void Update()
        {
            if (SEEInput.ToggleTranscription())
            {
                if (isOn)
                {
                    TurnOff(leaving: false);
                }
                else if (!isTurningOn)
                {
                    TurnOnAsync().Forget();
                }
            }

            if (isOn)
            {
                ProcessAudio();
            }
        }

        /// <summary>
        /// Turns the transcription on: connects to the voice chat, loads the speech
        /// model and tells all players. If anything is missing, the user is notified
        /// and the transcription stays off.
        /// </summary>
        private async UniTaskVoid TurnOnAsync()
        {
            isTurningOn = true;
            try
            {
                ITranscriptAudioSource source = CreateAudioSource();
                if (source == null)
                {
                    return;
                }
                if (!source.Start())
                {
                    ShowNotification.Warn(notificationTitle, "The voice chat is not running. The transcription stays off.");
                    return;
                }

                bool started = await transcriber.StartAsync(UserSetting.Instance.Audio.Transcription);
                // This component may have been destroyed while the model was loading.
                if (this == null || !started)
                {
                    source.Stop();
                    transcriber.Stop();
                    return;
                }

                audioSource = source;
                segmenter = new UtteranceSegmenter(source.SampleRate);
                // What was said before the players were told must not be transcribed.
                DiscardSamples();
                reportedDroppedSamples = source.DroppedSamples;
                isOn = true;

                Publish(TranscriptEntryKind.Started, "", DateTime.UtcNow);
                ShowNotification.Info(notificationTitle,
                                      "The transcription is on. What you say in the voice chat is shown to all players as text.");
            }
            finally
            {
                isTurningOn = false;
            }
        }

        /// <summary>
        /// Turns the transcription off and tells all players.
        /// </summary>
        /// <param name="leaving">True if the local player is leaving the scene. Then nothing
        /// is shown locally anymore, and the other players are told only if the network
        /// is still available.</param>
        private void TurnOff(bool leaving)
        {
            isOn = false;
            audioSource.Stop();
            audioSource = null;
            segmenter = null;
            transcriber.Stop();

            if (leaving)
            {
                try
                {
                    Send(new TranscriptEntry(PlayerName(), "", DateTime.UtcNow, TranscriptEntryKind.Stopped));
                }
                catch (Exception)
                {
                    // The scene, the user settings and the network are being shut down.
                    // If the notice cannot be sent anymore, there is nothing we can do about it.
                }
            }
            else
            {
                Publish(TranscriptEntryKind.Stopped, "", DateTime.UtcNow);
                ShowNotification.Info(notificationTitle, "The transcription is off.");
            }
        }

        /// <summary>
        /// Returns the audio source for the voice chat system selected by the user.
        /// If there is none, the user is notified.
        /// </summary>
        /// <returns>The audio source, or null if the transcription is not possible
        /// with the selected voice chat system.</returns>
        private static ITranscriptAudioSource CreateAudioSource()
        {
            switch (UserSetting.Instance.VoiceChat)
            {
                case VoiceChatSystems.Dissonance:
                    return new DissonanceAudioSource();
                case VoiceChatSystems.LiveKit:
                    ShowNotification.Warn(notificationTitle, "The transcription is not available with LiveKit yet.");
                    return null;
                default:
                    ShowNotification.Warn(notificationTitle,
                                          "The transcription needs a voice chat. Select a voice chat system in the user settings.");
                    return null;
            }
        }

        /// <summary>
        /// Passes the audio received from the microphone since the last frame to the
        /// <see cref="segmenter"/> and queues the finished utterances for transcription.
        /// </summary>
        private void ProcessAudio()
        {
            if (audioSource.SampleRate != segmenter.SampleRate)
            {
                // The microphone was changed.
                segmenter = new UtteranceSegmenter(audioSource.SampleRate);
            }

            if (audioSource.IsMuted)
            {
                // The other players cannot hear the local player, so nothing must be
                // transcribed either, including an utterance that was just begun.
                DiscardSamples();
                segmenter.Reset();
                return;
            }

            int count = audioSource.Read(readBuffer);
            while (count > 0)
            {
                IReadOnlyList<float[]> utterances = segmenter.Feed(readBuffer, count);
                for (int i = 0; i < utterances.Count; i++)
                {
                    transcriber.Enqueue(utterances[i], segmenter.SampleRate);
                }
                count = audioSource.Read(readBuffer);
            }

            WarnAboutLostAudio();
        }

        /// <summary>
        /// Reads and ignores all samples the <see cref="audioSource"/> has received so far.
        /// </summary>
        private void DiscardSamples()
        {
            while (audioSource.Read(readBuffer) > 0)
            {
                // Nothing to be done; the samples are just removed.
            }
        }

        /// <summary>
        /// Logs a warning if audio was lost because it was not read in time,
        /// but not more often than every <see cref="warningIntervalSeconds"/>.
        /// </summary>
        private void WarnAboutLostAudio()
        {
            long dropped = audioSource.DroppedSamples;
            if (dropped > reportedDroppedSamples && Time.unscaledTime >= nextWarningTime)
            {
                float seconds = (dropped - reportedDroppedSamples) / (float)audioSource.SampleRate;
                Debug.LogWarning($"The transcription lost {seconds:F1} s of microphone audio because the application was busy.\n");
                reportedDroppedSamples = dropped;
                nextWarningTime = Time.unscaledTime + warningIntervalSeconds;
            }
        }

        /// <summary>
        /// Called by the <see cref="transcriber"/> for each recognized utterance.
        /// </summary>
        /// <param name="text">The recognized text.</param>
        /// <param name="spokenAt">When the utterance was spoken (UTC).</param>
        private void OnTranscribed(string text, DateTime spokenAt)
        {
            if (isOn)
            {
                Publish(TranscriptEntryKind.Speech, text, spokenAt);
            }
        }

        /// <summary>
        /// Adds a new entry of the local player to the local transcript and
        /// sends it to all other players.
        /// </summary>
        /// <param name="kind">The kind of the entry.</param>
        /// <param name="text">What the local player said; empty for the notices.</param>
        /// <param name="time">When the entry was created (UTC).</param>
        private static void Publish(TranscriptEntryKind kind, string text, DateTime time)
        {
            TranscriptEntry entry = new(PlayerName(), text, time, kind);
            // The sender does not receive its own network action.
            TranscriptLog.Add(entry);
            Send(entry);
        }

        /// <summary>
        /// Sends <paramref name="entry"/> to all other players if the network is running.
        /// Otherwise nothing happens.
        /// </summary>
        /// <param name="entry">The entry to be sent.</param>
        private static void Send(TranscriptEntry entry)
        {
            if (NetworkIsRunning())
            {
                new TranscriptNetAction(entry).Execute();
            }
        }

        /// <summary>
        /// True if a network action can be sent. That is not the case anymore while
        /// the network is shutting down, e.g., when the player leaves; sending then
        /// would raise an exception.
        /// </summary>
        /// <returns>True if the network is running and the component that sends
        /// the network actions is spawned.</returns>
        private static bool NetworkIsRunning()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening || networkManager.ShutdownInProgress)
            {
                return false;
            }
            ActionNetwork actionNetwork = Net.Network.ActionNetworkInst.Value;
            return actionNetwork != null && actionNetwork.IsSpawned;
        }

        /// <summary>
        /// Returns the name of the local player as set in the user settings.
        /// </summary>
        /// <returns>The name of the local player.</returns>
        private static string PlayerName()
        {
            return UserSetting.Instance.Player.PlayerName;
        }
    }
}
