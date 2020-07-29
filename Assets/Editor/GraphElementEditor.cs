#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using SEE.DataModel;
using SEE.GO;

namespace SEEEditor
{
    //[CustomEditor(typeof(NodeRef))]
    //[CanEditMultipleObjects]
    public class GraphElementEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            NodeRef nodeRef = target as NodeRef;
            Node node = nodeRef.node;
            if (ReferenceEquals(node, null))
            {
                Debug.LogError("NodeRef references null");
            }
            else
            {
                node.ID = EditorGUILayout.TextField("Linkname:", node.ID);
                EditorGUILayout.TextField("Graph:", node.ItsGraph == null ? "NONE" : node.ItsGraph.Name);
            }
        }
    }
}

#endif
