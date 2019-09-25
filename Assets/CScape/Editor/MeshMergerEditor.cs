using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CScape;

[CustomEditor(typeof(MeshMerger))]
public class MeshMergerEditor : Editor
{

    public bool configurePrefab = false;
    //   float range = 30;


    void OnEnable()
    {

        MeshMerger um = (MeshMerger)target;

    }


    public override void OnInspectorGUI()
    {
        MeshMerger bm = (MeshMerger)target;

        GUILayout.BeginVertical("box");

        if (GUILayout.Button("Organize Buildings"))
        {
            bm.DeOrganize();
            bm.Organize();
        }

        if (GUILayout.Button("Merge Meshes"))
            bm.Unify();

        if (GUILayout.Button("De-Organize"))
        {
            bm.DeOrganize();
        }

        if (GUILayout.Button("Clean Unused Objects"))
            bm.CleanScene();
        bm.maximizeEfficiency = EditorGUILayout.Toggle(new GUIContent("Maximize Efficiency", "Groups objects to maximize efficiency"), bm.maximizeEfficiency);

        GUILayout.EndVertical();
    }

    // Update is called once per frame

}

