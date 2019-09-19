using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using CScape;

namespace CScape
{
    [CustomEditor(typeof(CSArray))]

    public class SCBalcony : Editor
    {
        private Texture banner;
        public bool configurePrefab = false;


        void OnEnable()
        {
            CSArray bm = (CSArray)target;
            banner = Resources.Load("CSHeader") as Texture;
            bm.AwakeMe();
            bm.UpdateElements();



        }
        public void OnSceneGUI()
        {
            CSArray bm = (CSArray)target;
            if (bm.rooftopHolder[0] != null)
            {
                for (int x = 0; x < bm.numberOfModifiers; x++)
                {
                    bm.rooftopHolder[x].transform.position = bm.gameObject.transform.position;
                    bm.rooftopHolder[x].transform.rotation = bm.gameObject.transform.rotation;
                }
            }

        }

        public override void OnInspectorGUI()
        {
            CSArray bm = (CSArray)target;
            GUILayout.Box(banner, GUILayout.ExpandWidth(true));

            GUILayout.BeginVertical();
            for (int x = 0; x < bm.numberOfModifiers; x++)
            {

                //if (GUILayout.Button("Update Template"))
                //{
                //    bm.Awake();
                //    bm.UpdateElements();
                //}
                bm.modifierType[x] = (CScape.CSArray.ModifierType)EditorGUILayout.EnumPopup("Type", bm.modifierType[x]);
                bm.alignTo[x] = (CScape.CSArray.Alignement)EditorGUILayout.EnumPopup("Start from", bm.alignTo[x]);
                bm.useAdvertising = EditorGUILayout.Toggle("Use Advertising Panels", bm.useAdvertising);
                bm.lodDistance = EditorGUILayout.Slider("Culling Distance", bm.lodDistance, 0f, 1f);
                bm.instancesX = EditorGUILayout.IntField("Max number of vertical instances", bm.instancesX);
                bm.instancesZ = EditorGUILayout.IntField("Max number of horizontal instances", bm.instancesZ);
                bm.randomSeed = EditorGUILayout.IntField("Random seed", bm.randomSeed);
                bm.upStart[x] = EditorGUILayout.IntField("Top offset start", bm.upStart[x]);
                
                bm.leftSideStart[x] = EditorGUILayout.IntField("Left offset start", bm.leftSideStart[x]);
                bm.rightSideStart[x] = EditorGUILayout.IntField("Right offset start", bm.rightSideStart[x]);
                
                bm.downStart[x] = EditorGUILayout.IntField("Down Offset start", bm.downStart[x]);
                
                bm.skipX[x] = EditorGUILayout.IntSlider("Skip Columns", bm.skipX[x], 1, 5);
                bm.skipY[x] = EditorGUILayout.IntSlider("Skip Rows", bm.skipY[x], 1, 5);
                bm.sparseRemove[x] = EditorGUILayout.Toggle("Sparse Remove", bm.sparseRemove[x]);
                bm.sparseRandom[x] = EditorGUILayout.IntField("Sparse remove seed", bm.sparseRandom[x]);
                bm.greebleMat[x] = EditorGUILayout.ObjectField("Rooftop Material", bm.greebleMat[x], typeof(Material), true) as Material;
                bm.placingDepth[x] = EditorGUILayout.FloatField("Z Depth", bm.placingDepth[x]);
                




                GUILayout.BeginVertical();

                bm.rooftopElements[x] = EditorGUILayout.ObjectField(bm.rooftopElements[x], typeof(GameObject), true) as GameObject;
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                
            }

            
            GUILayout.EndVertical();

                GUILayout.EndVertical();

                GUILayout.BeginHorizontal("Box");
                if (GUILayout.Button("-", "Label", GUILayout.Width(20), GUILayout.Height(15)))
                {
                    bm.numberOfModifiers --;
                }
                if (GUILayout.Button("+", "Label", GUILayout.Width(20), GUILayout.Height(15)))
                {
                    bm.numberOfModifiers ++;
                }

                GUILayout.EndHorizontal();




                if (GUI.changed)
                {
                    bm.AwakeMe();
                    bm.UpdateElements();
                    EditorUtility.SetDirty(bm);

                }
            }
        }
    }

