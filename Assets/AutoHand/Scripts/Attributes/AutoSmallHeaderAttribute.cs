using UnityEngine;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Autohand
{
    public class AutoSmallHeaderAttribute : PropertyAttribute
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
        public AutoSmallHeaderAttribute(string label, int count = default, int depth = default)
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
        public AutoSmallHeaderAttribute(string label, string tooltip, string toggleName, System.Type classType, int count = default, int depth = default)
        {
            this.count = count;
            this.depth = depth;
            this.label = label;
            this.tooltip = tooltip;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(AutoSmallHeaderAttribute))]
    public class AutoSmallHeaderDrawer : PropertyDrawer
    {
        const float padding = 2f;
        const float margin = -20f;

        static Font _labelFont = null;
        static Font labelFont
        {
            get {
                if(_labelFont == null)
                    _labelFont = Resources.Load<Font>("Righteous-Regular");
                return _labelFont;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            EditorGUILayout.Space();
            position.y += 14f;
            position.yMax += position.height/1.8f;

            var attr = (attribute as AutoSmallHeaderAttribute);

            var newRect = position;
            newRect.position = new Vector2(newRect.x+newRect.width-18, newRect.y);
            

            // draw header background and label
            var headerRect = new Rect(position.x + margin, position.y, (position.width - margin) + (padding * 2), position.height);
            EditorGUI.DrawRect(headerRect, Constants.BackgroundColor);


            var labelStyle = Constants.LabelStyle;
            labelStyle.font = labelFont;


            EditorGUI.LabelField(headerRect, new GUIContent(" " + attr.label, attr.tooltip), labelStyle);


            //var oldColor = GUI.color;
            //GUI.color = property.boolValue ? new Color(0.7f, 1f, 0.7f) : new Color(1f, 0.7f, 0.7f);

            //property.boolValue = EditorGUI.Toggle(newRect, property.boolValue);

            //GUI.color = oldColor;

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            position.y += 2f;
        }

    }

#endif
}