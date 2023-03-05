using UnityEngine;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Autohand {
    public class AutoLineAttribute : PropertyAttribute {
        public int count;
        public int depth;

        public string tooltip;
        public string toggleBool;

        public System.Type type;

        /// <summary>
        /// Add a header above a field
        /// </summary>
        /// <param name="label">A title for the header label</param>
        /// <param name="count">the number of child elements under this header</param>
        /// <param name="depth">the depth of this header element in the inspector foldout</param>
        public AutoLineAttribute(int count = default, int depth = default) {
            this.count = count;
            this.depth = depth;
        }

        /// <summary>
        /// Add a header above a field with a tooltip
        /// </summary>
        /// <param name="label">A title for the header label</param>
        /// <param name="tooltip">A note or instruction shown when hovering over the header</param>
        /// <param name="count">the number of child elements under this header</param>
        /// <param name="depth">the depth of this header element in the inspector foldout</param>
        public AutoLineAttribute(string tooltip, string toggleName, System.Type classType, int count = default, int depth = default) {
            this.count = count;
            this.depth = depth;
            this.tooltip = tooltip;
        }
    }
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(AutoLineAttribute))]
    public class AutoLineDrawer : PropertyDrawer {
        const float padding = 2f;
        const float margin = -20f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {



            // draw header background and label
            var headerRect = new Rect(position.x + margin, position.y+ 4, (position.width - margin) + (padding * 2), position.height);
            EditorGUI.DrawRect(headerRect, Constants.BackgroundColor);


            EditorGUILayout.Space();
        }

    }

#endif
}