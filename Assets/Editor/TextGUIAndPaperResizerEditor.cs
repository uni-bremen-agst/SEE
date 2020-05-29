#if UNITY_EDITOR

using SEE.GO;
using UnityEditor;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// Custom editor for TextGUIAndPaperResizer.
    /// </summary>
    [CustomEditor(typeof(TextGUIAndPaperResizer))]
    public class TextGUIAndPaperResizerEditor : Editor
    {
        private TextGUIAndPaperResizer _sourceGuiAndPaperResizer;

        private SerializedProperty TheTextProp;
        private SerializedProperty TheMarginProp;
        private SerializedProperty TheScaleProp;

        private void OnEnable()
        {
            TheTextProp = serializedObject.FindProperty("text");
            TheMarginProp = serializedObject.FindProperty("Margin");
            TheScaleProp = serializedObject.FindProperty("FontScale");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginVertical();

            EditorGUILayout.PrefixLabel(new GUIContent("Text", "left/right and top/bottom")); // label for text area
            TheTextProp.stringValue = EditorGUILayout.TextArea(TheTextProp.stringValue, GUILayout.MaxHeight(75));
            TheMarginProp.vector2Value = EditorGUILayout.Vector2Field(new GUIContent("Margin", "left/right and top/bottom"), TheMarginProp.vector2Value);
            TheScaleProp.floatValue = EditorGUILayout.FloatField(new GUIContent("Font Scale", "Scale of the font: \n1.00 = 89 chars per meter in a line,\n0.21 = 89 chars per line on a A4 paper"), TheScaleProp.floatValue);

            EditorGUILayout.EndVertical();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                _sourceGuiAndPaperResizer = (TextGUIAndPaperResizer)target;
                _sourceGuiAndPaperResizer.OnGuiChangedHandler();
            }

            // Show default inspector property editor
            //DrawDefaultInspector ();
        }
    }
}

#endif
