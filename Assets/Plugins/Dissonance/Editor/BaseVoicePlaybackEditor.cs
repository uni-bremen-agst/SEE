using Dissonance.Audio.Playback;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    public abstract class BaseVoicePlaybackEditor<TPlayback>
        : UnityEditor.Editor
        where TPlayback : MonoBehaviour
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

            OnGuiTop();

            if (Application.isPlaying)
            {
                var player = (IVoicePlaybackInternal) (TPlayback) target;
                if (player.IsActive)
                {
                    EditorGUILayout.LabelField("Player Name", player.PlayerName);
                    EditorGUILayout.LabelField("Positional Playback Available", player.AllowPositionalPlayback.ToString());
                    EditorGUILayout.LabelField("Priority", player.Priority.ToString());
                    EditorGUILayout.LabelField("Packet Loss", $"{player.PacketLoss ?? 0}%");
                    EditorGUILayout.LabelField("Jitter", $"{player.Jitter * 1000}σms");

                    _amplitudeMeter.DrawInspectorGui(player.Amplitude, !player.IsSpeaking);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnGuiTop()
        {
        }
    }
}
