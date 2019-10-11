using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CScape;

[CustomEditor(typeof(BuildingEditorOrganizer))]
public class BuildingEditorOrganizerEditor : Editor
{

    private Texture banner;
    public bool configurePrefab = false;
    //   float range = 30;


    void OnEnable()
    {

        BuildingEditorOrganizer um = (BuildingEditorOrganizer)target;
        banner = Resources.Load("CSHeader") as Texture;

    }


    public override void OnInspectorGUI()
    {
        BuildingEditorOrganizer bm = (BuildingEditorOrganizer)target;
        GUILayout.Box(banner, GUILayout.ExpandWidth(true));

        GUILayout.BeginVertical("box");

        if (GUILayout.Button("Organize Buildings"))
        {
            bm.DeOrganize();
            bm.Organize();
        }


        if (GUILayout.Button("De-Organize")) {
            bm.DeOrganize();
        }

        if (GUILayout.Button("Clean Unused Objects"))
            bm.CleanScene();
        bm.maximizeEfficiency = EditorGUILayout.Toggle(new GUIContent("Maximize Efficiency", "Groups objects to maximize efficiency"), bm.maximizeEfficiency);
        if (GUILayout.Button("Debug Stats"))
            bm.printStats();

        GUILayout.EndVertical();
    }

        // Update is called once per frame

}
