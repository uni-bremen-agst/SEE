using System;
using Dissonance;
using Dissonance.Audio.Capture;
using NAudio.Wave;
using SEE.Dissonance;
using UnityEngine;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Provides the audio of the local microphone as recorded by the Dissonance voice chat.
    /// </summary>
    public class DissonanceAudioSource : ITranscriptAudioSource, IMicrophoneSubscriber
    {
        /// <summary>
        /// The sample rate (in Hz) at which Dissonance delivers the microphone audio.
        /// The actual rate is taken from the delivered audio; this value is used
        /// until the first audio arrives and to choose the size of the buffer.
        /// </summary>
        private const int expectedSampleRate = 48000;

        /// <summary>
        /// The samples received from Dissonance and not yet read.
        /// </summary>
        private readonly TranscriptAudioBuffer buffer = new(expectedSampleRate * TranscriptAudioBuffer.DefaultSeconds);

        /// <summary>
        /// The sample rate of the samples in <see cref="buffer"/>. It is written by
        /// Dissonance's audio thread and read by the main thread.
        /// </summary>
        private volatile int sampleRate = expectedSampleRate;

        /// <summary>
        /// The Dissonance voice chat; null if this source is not started.
        /// </summary>
        private DissonanceComms comms;

        /// <summary>
        /// The component with which the user mutes and unmutes the voice chat;
        /// null if this source is not started or there is no such component.
        /// </summary>
        private VoiceChatInputController voiceChatInput;

        /// <inheritdoc/>
        public int SampleRate => sampleRate;

        /// <inheritdoc/>
        public long DroppedSamples => buffer.DroppedSamples;

        /// <summary>
        /// True if this source is not started, Dissonance is muted or turned off,
        /// or the user has muted the voice chat.
        /// </summary>
        public bool IsMuted
        {
            get
            {
                if (comms == null || !comms.isActiveAndEnabled || comms.IsMuted)
                {
                    return true;
                }
                if (voiceChatInput != null && voiceChatInput.Trigger != null)
                {
                    return voiceChatInput.Trigger.IsMuted;
                }
                return false;
            }
        }

        /// <summary>
        /// Subscribes to the microphone audio recorded by Dissonance.
        /// </summary>
        /// <returns>True if Dissonance is running in the current scene.</returns>
        public bool Start()
        {
            if (comms != null)
            {
                return true;
            }

            DissonanceComms found = UnityEngine.Object.FindAnyObjectByType<DissonanceComms>();
            if (found == null || !found.isActiveAndEnabled)
            {
                return false;
            }
            comms = found;
            voiceChatInput = UnityEngine.Object.FindAnyObjectByType<VoiceChatInputController>();
            if (voiceChatInput == null)
            {
                Debug.LogWarning($"There is no {nameof(VoiceChatInputController)} in the current scene. "
                                 + "Whether the voice chat is muted is determined by Dissonance alone.\n");
            }

            buffer.Clear();
            comms.SubscribeToRecordedAudio(this);
            return true;
        }

        /// <summary>
        /// Unsubscribes from Dissonance and discards the samples not yet read.
        /// </summary>
        public void Stop()
        {
            // The check for null is Unity's: it is also true if the component was destroyed.
            if (comms != null)
            {
                comms.UnsubscribeFromRecordedAudio(this);
            }
            comms = null;
            voiceChatInput = null;
            buffer.Clear();
        }

        /// <inheritdoc/>
        public int Read(float[] destination)
        {
            return buffer.Read(destination);
        }

        /// <summary>
        /// Stores the microphone audio delivered by Dissonance.
        /// </summary>
        /// <remarks>Called by Dissonance on its audio thread. Therefore, the samples
        /// are only copied here and no Unity API is used. Dissonance delivers mono audio.</remarks>
        /// <param name="data">The recorded samples; the array is reused by Dissonance.</param>
        /// <param name="format">The format of <paramref name="data"/>.</param>
        void IMicrophoneSubscriber.ReceiveMicrophoneData(ArraySegment<float> data, WaveFormat format)
        {
            if (format.SampleRate != sampleRate)
            {
                // Samples of different rates must not be mixed in the buffer.
                buffer.Clear();
                sampleRate = format.SampleRate;
            }
            buffer.Write(data.Array, data.Offset, data.Count);
        }

        /// <summary>
        /// Discards the samples not yet read, because the stream of audio was interrupted.
        /// </summary>
        /// <remarks>Called by Dissonance, possibly on its audio thread, when the
        /// microphone capture is restarted.</remarks>
        void IMicrophoneSubscriber.Reset()
        {
            buffer.Clear();
        }
    }
}
