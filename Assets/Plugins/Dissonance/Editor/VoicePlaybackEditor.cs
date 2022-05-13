using Dissonance.Audio.Playback;
using UnityEditor;

namespace Dissonance.Editor
{
    [CustomEditor(typeof(VoicePlayback))]
    [CanEditMultipleObjects]
    public class VoicePlaybackEditor
        : BaseVoicePlaybackEditor<VoicePlayback>
    {
    }
}
