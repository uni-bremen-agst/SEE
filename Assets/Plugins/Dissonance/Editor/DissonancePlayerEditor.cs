using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    public abstract class BaseIDissonancePlayerEditor
        : UnityEditor.Editor
    {
        private Texture2D _logo;

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

            var player = (IDissonancePlayer)target;

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Player: ", player.PlayerId);
                EditorGUILayout.LabelField("Type: ", player.Type.ToString());
                EditorGUILayout.Toggle("Tracking: ", player.IsTracking);
            }

            DrawDefaultInspector();
        }
    }
}
