using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using CScape;

namespace CScape
{
    [CustomEditor(typeof(CSAdvertising))]

    public class SCAdvertisingEditor : Editor
    {
        private Texture banner;
        public bool configurePrefab = false;


        void OnEnable()
        {
            CSAdvertising bm = (CSAdvertising)target;
            banner = Resources.Load("CSHeader") as Texture;
            bm.AwakeMe();
            bm.UpdateElements();



        }
        public void OnSceneGUI()
        {
            CSAdvertising bm = (CSAdvertising)target;
            if (bm.rooftopHolder != null)
            {
                bm.rooftopHolder.transform.position = bm.gameObject.transform.position;
                bm.rooftopHolder.transform.rotation = bm.gameObject.transform.rotation;
            }

            }

        public override void OnInspectorGUI()
        {
            CSAdvertising bm = (CSAdvertising)target;
            GUILayout.Box(banner, GUILayout.ExpandWidth(true));



            if (GUILayout.Button("Update Template"))
            {
                bm.AwakeMe();
                bm.UpdateElements();
            }
            bm.useAdvertising = EditorGUILayout.Toggle("Use Advertising Panels", bm.useAdvertising);
            bm.lodDistance = EditorGUILayout.Slider("Culling Distance", bm.lodDistance, 0f, 1f);
            bm.instancesX = EditorGUILayout.IntField("Advertising density", bm.instancesX);
            bm.randomScaleMin = EditorGUILayout.FloatField("Scale Min", bm.randomScaleMin);
            bm.randomScaleMax = EditorGUILayout.FloatField("Scale Max", bm.randomScaleMax);
            bm.randomSeed = EditorGUILayout.IntField("Random seed", bm.randomSeed);
            bm.greebleMat = EditorGUILayout.ObjectField("Rooftop Material", bm.greebleMat, typeof(Material), true) as Material;

            GUILayout.BeginVertical();
            for (int i = 0; i < bm.rooftopElements.Length; i++)
            {
                bm.rooftopElements[i] = EditorGUILayout.ObjectField("" + i, bm.rooftopElements[i], typeof(GameObject), true) as GameObject;
            }
            GUILayout.BeginHorizontal("Box");
            if (GUILayout.Button("-", "Label", GUILayout.Width(20), GUILayout.Height(15)))
            {
                System.Array.Resize(ref bm.rooftopElements, bm.rooftopElements.Length - 1);
            }
            if (GUILayout.Button("+", "Label", GUILayout.Width(20), GUILayout.Height(15)))
            {
                System.Array.Resize(ref bm.rooftopElements, bm.rooftopElements.Length + 1);
            }
            
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();






            if (GUI.changed)
            {
                bm.AwakeMe();
                bm.UpdateElements();
                EditorUtility.SetDirty(bm);

            }
        }
    }
}