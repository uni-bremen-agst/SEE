#if UNITY_EDITOR

using UnityEditor;
using SEE.GO;
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
            if (edgeRef.edge != null)
            {
                EditorGUILayout.TextField("Source node", edgeRef.edge.Source.ID);
                EditorGUILayout.TextField("Target node", edgeRef.edge.Target.ID);
                ShowTypeAndAttributes(edgeRef.edge);
            }
            else
            {
                GUILayout.Label("Edge: NONE", EditorStyles.boldLabel);
            }
        }
    }
}

#endif
