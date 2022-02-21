using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor.Windows
{
    internal abstract class BaseDissonanceEditorWindow
        : EditorWindow
    {
        #region constants
        private const float WindowBorder = 1f;

        private static readonly Color BackgroundColor = new Color32(51, 51, 51, 255);
        #endregion

        #region fields and properties
        private Texture2D _logo;

        private bool _styleCreated;
        protected GUIStyle LabelFieldStyle { get; private set; }
        #endregion

        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("Dissonance_Large_Icon");
        }

        protected void OnGUI()
        {
            if (!_styleCreated)
            {
                CreateStyles();
                _styleCreated = true;
            }

            var bg = DrawBackground();
            using (new GUILayout.AreaScope(bg))
            {
                EditorGUI.DrawPreviewTexture(new Rect(0, 7, 300, 125), _logo);
                using (new GUILayout.AreaScope(new Rect(10, 142, bg.width - 20, bg.height - 152)))
                {
                    DrawContent();
                }
            }
        }

        protected abstract void DrawContent();

        protected virtual void CreateStyles()
        {
            LabelFieldStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                richText = true,
                normal = {
                    textColor = Color.white,
                }
            };
        }

        private Rect DrawBackground()
        {
            var windowSize = new Vector2(position.width, position.height);
            var windowRect = new Rect(0, 0, windowSize.x, windowSize.y);
            var backgroundRect = new Rect(new Vector2(WindowBorder, WindowBorder), windowSize - new Vector2(WindowBorder, WindowBorder) * 2);

            var borderColor = EditorGUIUtility.isProSkin ? new Color(0.63f, 0.63f, 0.63f) : new Color(0.37f, 0.37f, 0.37f);
            EditorGUI.DrawRect(windowRect, borderColor);

            EditorGUI.DrawRect(backgroundRect, BackgroundColor);

            return backgroundRect;
        }
    }
}