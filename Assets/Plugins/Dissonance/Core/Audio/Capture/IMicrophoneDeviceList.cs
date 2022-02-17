using System.Collections.Generic;

namespace Dissonance.Audio.Capture
{
    public interface IMicrophoneDeviceList
    {
        /// <summary>
        /// Gets a list of all valid microphone devices.
        /// </summary>
        /// <param name="output">A list for results to be added to</param>
        void GetDevices(List<string> output);
    }
}
