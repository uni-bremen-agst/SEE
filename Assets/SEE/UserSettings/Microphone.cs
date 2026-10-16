using System.Collections.Generic;
using SEE.Graphs.Config;

namespace SEE.UserSettings
{
    /// <summary>
    /// Configuration for the microphone.
    /// </summary>
    public class Microphone
    {
        /// <summary>
        /// The microphone which should be used for voice chat.
        /// </summary>
        public string MicrophoneDevice = "";

        /// <summary>
        /// Maximal bitrate to use for microphone.
        /// </summary>
        public ulong MaxBitrate = 64000;

        /// <summary>
        /// Whenever echo cancellation should be enabled.
        /// </summary>
        public bool EchoCancellation;

        /// <summary>
        /// Whenever noise suppression should be enabled.
        /// </summary>
        public bool NoiseSuppression;

        /// <summary>
        /// Whenever auto gain controll should be enabled.
        /// </summary>
        public bool AutoGainControl;

        /// <summary>
        /// Label of <see cref="MicrophoneDevice"/> in the configuration file.
        /// </summary>
        private const string microphoneDeviceLabel = "microphoneDevice";

        /// <summary>
        /// Label of <see cref="MaxBitrate"/> in the configuration file.
        /// </summary>
        private const string maxBitrateLabel = "maxBitrate";

        /// <summary>
        /// Label of <see cref="EchoCancellation"/> in the configuration file.
        /// </summary>
        private const string echoCancellationLabel = "echoCancellation";

        /// <summary>
        /// Label of <see cref="NoiseSuppression"/> in the configuration file.
        /// </summary>
        private const string noiseSuppressionLabel = "noiseSuppression";

        /// <summary>
        /// Label of <see cref="AutoGainControl"/> in the configuration file.
        /// </summary>
        private const string autoGainControlLabel = "autoGainControl";

        public virtual void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(MicrophoneDevice, microphoneDeviceLabel);
            writer.Save(MaxBitrate, maxBitrateLabel);
            writer.Save(EchoCancellation, echoCancellationLabel);
            writer.Save(NoiseSuppression, noiseSuppressionLabel);
            writer.Save(AutoGainControl, autoGainControlLabel);
            writer.EndGroup();

        }

        public virtual void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.Restore(values, microphoneDeviceLabel, ref MicrophoneDevice);
                ConfigIO.Restore(values, maxBitrateLabel, ref MaxBitrate);
                ConfigIO.Restore(values, echoCancellationLabel, ref EchoCancellation);
                ConfigIO.Restore(values, noiseSuppressionLabel, ref NoiseSuppression);
                ConfigIO.Restore(values, autoGainControlLabel, ref AutoGainControl);
            }
        }
    }
}
