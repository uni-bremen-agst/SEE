using Dissonance.Audio.Playback;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    [CustomEditor(typeof (VoicePlayback))]
    [CanEditMultipleObjects]
    public class VoicePlaybackEditor : UnityEditor.Editor
    {
        private Texture2D _logo;

        private readonly VUMeter _amplitudeMeter = new VUMeter();

        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Label(_logo);

            if (!Application.isPlaying)
                return;

            var player = (IVoicePlaybackInternal)target;

            if (player.IsActive)
            {
                EditorGUILayout.LabelField("Player Name", player.PlayerName);
                EditorGUILayout.LabelField("Positional Playback Available", player.AllowPositionalPlayback.ToString());
                EditorGUILayout.LabelField("Priority", player.Priority.ToString());
                EditorGUILayout.LabelField("Packet Loss", string.Format("{0}%", player.PacketLoss ?? 0));
                EditorGUILayout.LabelField("Network Jitter", string.Format("{0}σms", player.Jitter * 1000));

                _amplitudeMeter.DrawInspectorGui(player.Amplitude, !player.IsSpeaking);

                // External spatialisation is not currently supported, making this notification meaningless.
                //if (player.IsApplyingAudioSpatialization)
                //{
                //    EditorGUILayout.LabelField("Playback Mode", "Internally Spatialized");
                //    EditorGUILayout.HelpBox("Dissonance has detected that the AudioSource is not spatialized by an external audio spatializer. Dissonance will apply basic spatialization.", MessageType.Info, true);
                //}
                //else
                //{
                //    EditorGUILayout.LabelField("Playback Mode", "Externally Spatialized");
                //    EditorGUILayout.HelpBox("Dissonance has detected that the AudioSource is spatialized by an external audio spatializer.", MessageType.Info, true);
                //}
            }
        }
    }
}
