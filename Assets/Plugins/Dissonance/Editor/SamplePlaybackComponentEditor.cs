using Dissonance.Audio.Playback;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    [CustomEditor(typeof(SamplePlaybackComponent))]
    [UsedImplicitly]
    public class SamplePlaybackComponentEditor
        : UnityEditor.Editor
    {
        private Texture2D _logo;

        private readonly AnimationCurve _rateGraph = new AnimationCurve();
        private float _nextRateGraphKey;

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

            if (Application.isPlaying)
            {
                var component = (SamplePlaybackComponent)target;
                var maybeSession = component.Session;
                if (maybeSession != null)
                {
                    var session = maybeSession.Value;
                    var sync = session.SyncState;

                    EditorGUILayout.LabelField(string.Format("Buffered Packets: {0}", session.BufferCount));
                    EditorGUILayout.LabelField(string.Format("Playback Position: {0:0.00}s", sync.ActualPlaybackPosition.TotalSeconds));
                    EditorGUILayout.LabelField(string.Format("Ideal Position: {0:0.00}s", sync.IdealPlaybackPosition.TotalSeconds));
                    EditorGUILayout.LabelField(string.Format("Desync: {0:000}ms", sync.Desync.TotalMilliseconds));
                    EditorGUILayout.LabelField(string.Format("Compensated Playback Speed: {0:P1}", sync.CompensatedPlaybackSpeed));

                    _rateGraph.AddKey(_nextRateGraphKey++, sync.CompensatedPlaybackSpeed);
                    while (_rateGraph.length > 200)
                        _rateGraph.RemoveKey(0);
                    EditorGUILayout.CurveField(_rateGraph, GUILayout.Height(100));
                }
                else
                {
                    EditorGUILayout.LabelField("Not Speaking");

                    // Clear the data from the buffer graph
                    while (_rateGraph.length > 0)
                        _rateGraph.RemoveKey(_rateGraph.length - 1);
                    _nextRateGraphKey = 0;
                }
            }
        }
    }
}