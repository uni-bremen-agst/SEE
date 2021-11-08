using SEE.Game;
using SEE.Game.City;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

namespace SEEEditor
{
    /// <summary>
    /// Editor for ArchitectureInteraction.
    /// </summary>
    //[Obsolete("Introduced only for capturing videos.")]
    [CustomEditor(typeof(ArchitectureInteraction))]
    public class ArchitectureInteractionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ArchitectureInteraction animator = target as ArchitectureInteraction;

            animator.CodeCity = (SEECity)EditorGUILayout.ObjectField
                                     (label: "Code City",
                                      obj: animator.CodeCity,
                                      objType: typeof(SEECity),
                                      allowSceneObjects: true);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Implementation Edges");
            {
                animator.ImplementationEdgesVisible = EditorGUILayout.Toggle(animator.ImplementationEdgesVisible);
                animator.ImplementationEdgesStartColor = EditorGUILayout.ColorField(animator.ImplementationEdgesStartColor);
                animator.ImplementationEdgesEndColor = EditorGUILayout.ColorField(animator.ImplementationEdgesEndColor);
                animator.ImplementationEdgesWidth = EditorGUILayout.FloatField(animator.ImplementationEdgesWidth);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Architecture Edges");
            {
                animator.ArchitectureEdgesVisible = EditorGUILayout.Toggle(animator.ArchitectureEdgesVisible);
                animator.ArchitectureEdgesStartColor = EditorGUILayout.ColorField(animator.ArchitectureEdgesStartColor);
                animator.ArchitectureEdgesEndColor = EditorGUILayout.ColorField(animator.ArchitectureEdgesEndColor);
                animator.ArchitectureEdgesWidth = EditorGUILayout.FloatField(animator.ArchitectureEdgesWidth);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Reflexion Edges");
            {
                animator.ReflexionEdgesVisible = EditorGUILayout.Toggle(animator.ReflexionEdgesVisible);
                animator.ReflexionEdgesWidth = EditorGUILayout.FloatField(animator.ReflexionEdgesWidth);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("Architecture Nodes");
                animator.ArchitectureNodesVisible = EditorGUILayout.Toggle(animator.ArchitectureNodesVisible);
                GUILayout.Label("Implementation Nodes");
                animator.ImplementationNodesVisible = EditorGUILayout.Toggle(animator.ImplementationNodesVisible);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Update"))
            {
                animator.UpdateCity();
            }
        }
    }
}

#endif