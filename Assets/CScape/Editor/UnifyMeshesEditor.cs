using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CScape;

[CustomEditor(typeof(UnifyMeshes))]
public class UnifyMeshesEditor : Editor
{

    private Texture banner;
    public bool configurePrefab = false;
    //   float range = 30;


    void OnEnable()
    {

        UnifyMeshes um = (UnifyMeshes)target;
        banner = Resources.Load("CSHeader") as Texture;

    }


    public override void OnInspectorGUI()
    {
        UnifyMeshes bm = (UnifyMeshes)target;
        GUILayout.Box(banner, GUILayout.ExpandWidth(true));

        GUILayout.BeginVertical("box");
        if (GUILayout.Button("Unify"))
            bm.Unify();

        if (GUILayout.Button("Make editable"))
            bm.Modify();

        if (GUILayout.Button("DeOrganize"))
            bm.DeOrganize();

        if (GUILayout.Button("Export Mesh"))
            bm.ExportMesh();
        GUILayout.EndVertical();
    }

        // Update is called once per frame

}
