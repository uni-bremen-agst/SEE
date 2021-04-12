#if UNITY_EDITOR

using SEE.GO;
using UnityEditor;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// A custom editor for instances of EdgeRef.
    /// </summary>
    [CustomEditor(typeof(EdgeRef))]
    [CanEditMultipleObjects]
    public class EdgeRefEditor : GraphElementRefEditor
    {
        public override void OnInspectorGUI()
        {
            EdgeRef edgeRef = target as EdgeRef;

            GUILayout.Label("Edge attributes", EditorStyles.boldLabel);
            if (edgeRef.Value != null)
            {
                EditorGUILayout.TextField("Source node", edgeRef.Value.Source.ID);
                EditorGUILayout.TextField("Target node", edgeRef.Value.Target.ID);
                ShowTypeAndAttributes(edgeRef.Value);
            }
            else
            {
                GUILayout.Label("Edge: NONE", EditorStyles.boldLabel);
            }
        }
    }
}

#endif
