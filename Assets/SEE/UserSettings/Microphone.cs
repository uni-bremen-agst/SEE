using System.Collections.Generic;
using SEE.Utils.Config;

namespace SEE.UserSettings
{
    public class Microphone
    {
        /// <summary>
        /// The microphone which should be used for the voice chat.
        /// </summary>
        public string MicrophoneDevice = "";

        public ulong MaxBitrate = 64000;

        public bool EchoCancellation;

        public bool NoiseSuppression;

        public bool AutoGainControl;

        private const string microphoneDeviceLabel = "microphoneDevice";

        private const string maxBitrateLabel = "maxBitrate";

        private const string echoCancellationLabel = "echoCancellation";

        private const string noiseSuppressionLabel = "noiseSuppression";

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
