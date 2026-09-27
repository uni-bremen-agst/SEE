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

        private const string microphoneDeviceLabel = "microphoneDevice";

        private const string maxBitrateLabel = "maxBitrate";

        public virtual void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(MicrophoneDevice, microphoneDeviceLabel);
            writer.Save(MaxBitrate, maxBitrateLabel);
            writer.EndGroup();

        }

        public virtual void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.Restore(values, microphoneDeviceLabel, ref MicrophoneDevice);
                ConfigIO.Restore(values, maxBitrateLabel, ref MaxBitrate);
            }
        }
    }
}
