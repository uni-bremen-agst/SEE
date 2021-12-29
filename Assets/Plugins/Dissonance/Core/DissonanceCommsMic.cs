using System;
using System.Collections.Generic;
using Dissonance.Audio.Capture;
using JetBrains.Annotations;

namespace Dissonance
{
    public partial class DissonanceComms
    {
        /// <summary>
        /// Get or set the microphone device name to use for voice capture
        /// </summary>
        [CanBeNull] public string MicrophoneName
        {
            get { return _micName; }
            set
            {
                if (_micName == value)
                    return;

                _capture.MicrophoneName = value;
                _micName = value;
            }
        }

        /// <summary>
        /// Get the microphone capture object. Will be null if Dissonance has not yet started.
        /// </summary>
        [CanBeNull] public IMicrophoneCapture MicrophoneCapture
        {
            get { return _capture.Microphone; }
        }

        /// <summary>
        /// Get a list of valid microphone devices that can be used.
        /// </summary>
        /// <param name="output"></param>
        public void GetMicrophoneDevices(List<string> output)
        {
            // Try to get the mic component from the capture pipeline if it has already been started
            // If that finds nothing, try to get the component directly
            var mic = _capture.Microphone;
            if (mic == null)
                mic = GetComponent<IMicrophoneCapture>();

            // Convert the mic into a device list. If that fails try to get a device list directly.
            var list = mic as IMicrophoneDeviceList;
            if (list == null)
                list = GetComponent<IMicrophoneDeviceList>();

            // If the list is null just fall back to using the Unity method
            if (list != null)
                list.GetDevices(output);
            else
                output.AddRange(UnityEngine.Microphone.devices);
        }

        /// <summary>
        /// Force the microphone capture system to be reset
        /// </summary>
        /// <remarks>This will destroy and recreate the microphone, preprocessor and encoder.</remarks>
        public void ResetMicrophoneCapture()
        {
            if (_capture != null)
                _capture.ForceReset();
        }

        /// <summary>
        ///     Subscribes to the stream of recorded audio data
        /// </summary>
        /// <param name="listener">
        ///     The listener which is to receive microphone audio data.
        /// </param>
        public void SubscribeToRecordedAudio([NotNull] IMicrophoneSubscriber listener)
        {
            _capture.Subscribe(listener);
        }

        // Marked obsolete on 2020-03-31
        [Obsolete("Use `SubscribeToRecordedAudio` instead")]
        public void SubcribeToRecordedAudio([NotNull] IMicrophoneSubscriber listener)
        {
            // Deprecated due to misspelling in the name on 2020-03-31

            SubscribeToRecordedAudio(listener);
        }

        /// <summary>
        ///     Unsubscribes from the stream of recorded audio data
        /// </summary>
        /// <param name="listener"></param>
        public void UnsubscribeFromRecordedAudio([NotNull] IMicrophoneSubscriber listener)
        {
            _capture.Unsubscribe(listener);
        }
    }
}
