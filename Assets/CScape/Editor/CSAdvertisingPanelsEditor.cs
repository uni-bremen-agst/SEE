using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CScape;
using UnityEditor;

namespace CScape
{
    [CustomEditor(typeof(CSAdvertisingPanels))]
    // [CanEditMultipleObjects]


    public class CSAdvertisingPanelsEditor : Editor
    {
        private Texture banner;
        public bool configurePrefab = false;

        void OnEnable()
        {
            CSAdvertisingPanels bm = (CSAdvertisingPanels)target;
            banner = Resources.Load("CSHeader") as Texture;
            bm.UpdateAdv();
        }

        public override void OnInspectorGUI()
        {
            CSAdvertisingPanels bm = (CSAdvertisingPanels)target;
            GUILayout.Box(banner, GUILayout.ExpandWidth(true));


            bm.advPanelFrontPrefab = EditorGUILayout.ObjectField("AdvertisingPanel template", bm.advPanelFrontPrefab, typeof(GameObject), true) as GameObject;
            if (GUILayout.Button("Update Template"))
            {
                bm.UpdateAdv();
            }




            if (GUI.changed)
            {
                
                bm.UpdateAdv();
                EditorUtility.SetDirty(bm);

            }
        }

    }

}
