using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CScape;

[CustomEditor(typeof(PowerMerger))]
public class PowerMergerEditor : Editor
{

    private Texture banner;
    public bool configurePrefab = false;
    //   float range = 30;


    void OnEnable()
    {

        PowerMerger um = (PowerMerger)target;
      //  banner = Resources.Load("CSHeader") as Texture;

    }


    public override void OnInspectorGUI()
    {
        PowerMerger bm = (PowerMerger)target;
        //GUILayout.Box(banner, GUILayout.ExpandWidth(true));

        GUILayout.BeginVertical("box");

        if (GUILayout.Button("Organize meshes"))
        {
            bm.DeOrganize();
            bm.Organize();
        }


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
