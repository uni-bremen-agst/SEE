using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CSToolset;


namespace CScape
{
    [CustomEditor(typeof(CScapeToolsetManager))]
    // [CanEditMultipleObjects]


    public class CScapeToolsetManagerEditor : Editor
    {
        private Texture banner;
        public bool configurePrefab = false;
        //   float range = 30;


        void OnEnable()
        {

            CScapeToolsetManager bm = (CScapeToolsetManager)target;

        }



        public override void OnInspectorGUI()
        {
            CScapeToolsetManager bm = (CScapeToolsetManager)target;
            //GUILayout.Box(banner, GUILayout.ExpandWidth(true));

            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal("box");
            if (GUILayout.Button("Previous Tile"))
            {
                bm.bakeAll = false;
                bm.UpdateMe();
                CScapeToolsetManager.PreviousTile();
            }
            if (GUILayout.Button("Next Tile"))
            {
                bm.bakeAll = false;
                bm.UpdateMe();
                CScapeToolsetManager.NextTile();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Bake Textures"))
            {
                bm.bakeAll = true;
                bm.UpdateMe();

            }


            GUILayout.EndVertical();
        }
    }
}
