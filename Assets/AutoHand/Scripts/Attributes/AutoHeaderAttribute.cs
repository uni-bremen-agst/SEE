using UnityEngine;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Autohand
{
    public class AutoHeaderAttribute : PropertyAttribute
    {
        public int count;
        public int depth;

        public string label;
        public string tooltip;
        public string toggleBool;

        public System.Type type;

        /// <summary>
        /// Add a header above a field
        /// </summary>
        /// <param name="label">A title for the header label</param>
        /// <param name="count">the number of child elements under this header</param>
        /// <param name="depth">the depth of this header element in the inspector foldout</param>
        public AutoHeaderAttribute(string label, int count = default, int depth = default)
        {
            this.count = count;
            this.depth = depth;
            this.label = label;
        }

        /// <summary>
        /// Add a header above a field with a tooltip
        /// </summary>
        /// <param name="label">A title for the header label</param>
        /// <param name="tooltip">A note or instruction shown when hovering over the header</param>
        /// <param name="count">the number of child elements under this header</param>
        /// <param name="depth">the depth of this header element in the inspector foldout</param>
        public AutoHeaderAttribute(string label, string tooltip, string toggleName, System.Type classType, int count = default, int depth = default)
        {
            this.count = count;
            this.depth = depth;
            this.label = label;
            this.tooltip = tooltip;
        }
    }
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(AutoHeaderAttribute))]
    public class AutoHeaderDrawer : PropertyDrawer
    {
        const float padding = 2f;
        const float margin = -20f;

        static Texture autohandlogo = null;
        static Font _labelFont = null;
        static Font labelFont
        {
            get
            {
                if (_labelFont == null)
                    _labelFont = Resources.Load<Font>("Righteous-Big");
                return _labelFont;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            position.yMax += position.height;

            var attr = (attribute as AutoHeaderAttribute);

            var newRect = position;
            newRect.position = new Vector2(newRect.x+newRect.width-18, newRect.y);
            

            // draw header background and label
            var headerRect = new Rect(position.x + margin, position.y, (position.width - margin) + (padding * 2), position.height);
            EditorGUI.DrawRect(headerRect, Constants.BackgroundColor);

            var labelStyle = Constants.HeaderStyle;
            labelStyle.font = labelFont;

            if (autohandlogo == null)
                autohandlogo = Resources.Load<Texture>("AutoHandLogo");
            EditorGUI.LabelField(headerRect, new GUIContent(" " + attr.label, autohandlogo, attr.tooltip), labelStyle);


            EditorGUILayout.Space();
            EditorGUILayout.Space();
            position.y += 2f;
        }

    }


    public static class Constants{
        private static readonly Color[] _barColors = new Color[5] {
            new Color(0.3411765f, 0.6039216f, 0.7803922f),
            new Color(0.145098f, 0.6f, 0.509804f),
            new Color(0.9215686f, 0.6431373f, 0.282353f),
            new Color(0.8823529f, 0.3529412f, 0.4039216f),
            new Color(0.9529412f, 0.9294118f, 0.682353f)
        };

        public static Color ColorForDepth(int depth) => _barColors[depth % _barColors.Length];

        public static Color BackgroundColor { get; } = EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f, 0.75f) : new Color(0.7f, 0.7f, 0.7f, 0.75f);
        
        public static GUIStyle LabelStyle { get; } = new GUIStyle(GUI.skin.label){
            alignment = TextAnchor.MiddleLeft,
            fontSize = 15
        };
        public static GUIStyle HeaderStyle { get; } = new GUIStyle(GUI.skin.label){
            alignment = TextAnchor.MiddleCenter,
            fontSize = 26
        };
    }
#endif
}