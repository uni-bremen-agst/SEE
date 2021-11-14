using System;
using SEE.Game;
using SEE.Game.City;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

namespace SEEEditor
{
    /// <summary>
    /// Editor for SceneAnimator for capturing demo videos.
    /// </summary>
    [Obsolete("Introduced only for capturing videos.")]
    [CustomEditor(typeof(SceneAnimator))]
    public class SceneAnimatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SceneAnimator animator = target as SceneAnimator;

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

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Duration");
            animator.RevisionDuration = EditorGUILayout.FloatField(animator.RevisionDuration);
            GUILayout.Label("Min");
            animator.MinNodesPerRevision = EditorGUILayout.IntField(animator.MinNodesPerRevision);
            GUILayout.Label("Max");
            animator.MaxNodesPerRevision = EditorGUILayout.IntField(animator.MaxNodesPerRevision);
            GUILayout.Label("Swaps");
            animator.RandomSwaps = EditorGUILayout.IntField(animator.RandomSwaps);
            if (GUILayout.Button("Evolution"))
            {
                animator.Evolution();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Duration");
            animator.CallDuration = EditorGUILayout.FloatField(animator.CallDuration);
            if (GUILayout.Button("Call Graph"))
            {
                animator.CallGraph();
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