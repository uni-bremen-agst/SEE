#if UNITY_EDITOR

using SEE.GO;
using UnityEditor;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// A custom editor for instances of NodeRef.
    /// </summary>
    [CustomEditor(typeof(NodeRef))]
    [CanEditMultipleObjects]
    public class NodeRefEditor : GraphElementRefEditor
    {
        public override void OnInspectorGUI()
        {
            NodeRef nodeRef = target as NodeRef;

            GUILayout.Label("Node attributes", EditorStyles.boldLabel);
            if (nodeRef.node != null)
            {
                ShowTypeAndAttributes(nodeRef.node);
            }
            else
            {
                GUILayout.Label("Node: NONE", EditorStyles.boldLabel);
            }
        }
    }
}

#endif
