using UnityEngine;

namespace Dissonance
{
    /// <summary>
    ///     The central Dissonance Voice Comms component.
    ///     Place one of these on a voice comm entity near the root of your scene.
    /// </summary>
    /// <remarks>
    ///     Handles recording the local player's microphone and sending the data to the network.
    ///     Handles managing the playback entities for the other users on the network.
    ///     Provides the API for opening and closing channels.
    /// </remarks>
    // ReSharper disable once InheritdocConsiderUsage
    [HelpURL("https://placeholder-software.co.uk/dissonance/docs/Reference/Components/Dissonance-Comms/")]
    public sealed partial class DissonanceComms
        : MonoBehaviour
    {
        // The implementation for this behaviour is contained in other files. e.g. `Core/DissonanceCommsImpl.cs`
    }
}
