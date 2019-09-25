using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using CScape;
//using UnityEditor;

namespace CScape
{
    [CustomEditor(typeof(StreetModifier))]
   [CanEditMultipleObjects]

    public class StreetModifierEditor : Editor
    {
        private Texture banner;
        public bool configurePrefab = false;


        void OnEnable()
        {
            StreetModifier bm = (StreetModifier)target;
            bm.AwakeCity();
            bm.UpdateCity();
            banner = Resources.Load("CSHeader") as Texture;


        }




        public override void OnInspectorGUI()
        {
            StreetModifier bm = (StreetModifier)target;
            bm.streetType = (CScape.StreetModifier.CScapeStreetType)EditorGUILayout.EnumPopup("Street Type", bm.streetType);

            GUILayout.Box(banner, GUILayout.ExpandWidth(true));

            GUILayout.BeginVertical("box");
            bm.districtStyle = EditorGUILayout.ObjectField("District Style", bm.districtStyle, typeof(DistrictStyle), true) as DistrictStyle;
            GUILayout.Label("Facade assignement");
            bm.materialId1 = EditorGUILayout.IntSlider("Material ID 1", bm.materialId1, 0, 15);
            bm.materialId2 = EditorGUILayout.IntSlider("Material ID 2", bm.materialId2, 0, 15);
            bm.materialId3 = EditorGUILayout.IntSlider("Material ID 3", bm.materialId3, 0, 15);
            bm.sidewalkID = EditorGUILayout.IntSlider("Material Sidewalk ID", bm.sidewalkID, 0, 15);
            bm.streetID = EditorGUILayout.IntSlider("Material Road ID", bm.streetID, 0, 15);
            GUILayout.EndHorizontal();
            //configure Prefab
            GUILayout.BeginVertical("box");
            configurePrefab = EditorGUILayout.Foldout(configurePrefab, "Prefab Configuration");
            if (configurePrefab)
            {
                bm.meshOriginal = EditorGUILayout.ObjectField("Mesh Template", bm.meshOriginal, typeof(Mesh), true) as Mesh;
                if (GUILayout.Button("Update Mesh Template"))
                {
                    bm.AwakeCity();
                    bm.UpdateCity();
                }
                bm.lowFloorBound = EditorGUILayout.Vector3Field("Resizer Center", bm.lowFloorBound);
                bm.lowFloorBoundSecond = EditorGUILayout.Vector3Field("Resizer Midpoint", bm.lowFloorBoundSecond);
                bm.prefabFloors = EditorGUILayout.IntField("Prefab floor number", bm.prefabFloors);
                bm.prefabWidth = EditorGUILayout.IntField("Prefab width", bm.prefabWidth);
                bm.prefabDepth = EditorGUILayout.IntField("Prefab depth", bm.prefabDepth);
                bm.prefabCenterSectionWidth = EditorGUILayout.IntField("Center section width", bm.prefabCenterSectionWidth);
                bm.prefabCenterSectionDepth = EditorGUILayout.IntField("Center Section depth", bm.prefabCenterSectionDepth);
                bm.normalThreshold = EditorGUILayout.Slider("Normal threshold", bm.normalThreshold, 0f, 0.5f);
                bm.floorHeight = EditorGUILayout.FloatField("Floor Height", bm.floorHeight);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical("box");
            GUILayout.Label("Variations");
            //bm.windowOpen = EditorGUILayout.Slider("Window Close", bm.windowOpen, 0f, 5f);
            //bm.pattern = EditorGUILayout.Slider("Floor Pattern", bm.pattern, 0f, 1f);
            //bm.colorVariation.x = EditorGUILayout.Slider("Lightness", bm.colorVariation.x, 0f, 5f);
            //bm.colorVariation.y = EditorGUILayout.Slider("Hue", bm.colorVariation.y, 0f, 1f);
  //          bm.uniqueMapping = EditorGUILayout.IntField("Unique Mapping", bm.uniqueMapping);
   //         bm.floorNumber = EditorGUILayout.IntField("Floor Number", bm.floorNumber);
            bm.blockDepth = EditorGUILayout.IntField("Block depth", bm.blockDepth);
            bm.blockWidth = EditorGUILayout.IntField("Block Width", bm.blockWidth);
            bm.sectionDepth = EditorGUILayout.IntField("Section depth", bm.sectionDepth);
            bm.sectionWidth = EditorGUILayout.IntField("Section Width", bm.sectionWidth);
            bm.randomSeed = EditorGUILayout.IntField("Random Seed", bm.randomSeed);

            bm.slitRB = EditorGUILayout.IntSlider("Slit RB", bm.slitRB, -30, 30);
            bm.slitLF = EditorGUILayout.IntSlider("Slit LF", bm.slitLF, -30, 30);
            bm.averageBuildingSizeMin = EditorGUILayout.IntField("Building size min", bm.averageBuildingSizeMin);
            bm.averageBuildingSizeMax = EditorGUILayout.IntField("Building Size Max", bm.averageBuildingSizeMax);
            bm.useSkewLR = EditorGUILayout.Toggle("Use LR Skewing", bm.useSkewLR);
            bm.useSkewFB = EditorGUILayout.Toggle("Use FB Skewing", bm.useSkewFB);
            bm.skewRotationFront = EditorGUILayout.FloatField("Skew Rotation Front", bm.skewRotationFront);
            bm.skewRotationLeft = EditorGUILayout.FloatField("Skew Rotation Left", bm.skewRotationLeft);
            bm.skewRotationBack = EditorGUILayout.FloatField("Skew Rotation Back", bm.skewRotationBack);
            bm.skewRotationRight = EditorGUILayout.FloatField("Skew Rotation Right", bm.skewRotationRight);

            bm.lenghtFront = EditorGUILayout.FloatField("Lenght Front", bm.lenghtFront);
            bm.lenghtLeft = EditorGUILayout.FloatField("Lenght Left", bm.lenghtLeft);
            bm.lenghtBack = EditorGUILayout.FloatField("Lenght Back", bm.lenghtBack);
            bm.lenghtRight = EditorGUILayout.FloatField("Lenght Right", bm.lenghtRight);
            bm.sidewalkSize = EditorGUILayout.IntSlider("Sidewalk Size", bm.sidewalkSize, 1, 15);
            bm.distantiateBuildings = EditorGUILayout.IntSlider("Distantiate buildings", bm.distantiateBuildings, 0, Mathf.Min(bm.blockDepth, bm.blockWidth));
            bm.useGraffiti = EditorGUILayout.Toggle("UseGraffiti", bm.useGraffiti);
            if (GUILayout.Button("Generate Block"))
            {
                bm.GenerateBuildings();
            }
            if (GUILayout.Button("Generate Street Details"))
            {
                bm.GenerateDetails();
            }

            if (bm.useSkewFB)
            {
                if (GUILayout.Button("SplitX"))
                {
                    bm.SplitX();
                }
            }
            else if (bm.useSkewLR)
            {
                if (GUILayout.Button("SplitZ"))
                {
                    bm.SplitZ();
                }
            }
            else
            {
                if (GUILayout.Button("SplitX"))
                {
                    bm.SplitX();
                }
                if (GUILayout.Button("SplitZ"))
                {
                    bm.SplitZ();
                }
            }


            GUILayout.EndHorizontal();




            if (GUI.changed)
            {
                bm.UpdateCity();
                EditorUtility.SetDirty(bm);
#if UNITY_5_4_OR_NEWER
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
#endif
            }

        }


        void OnSceneGUI()
        {
            StreetModifier t = (target as StreetModifier);
            Quaternion newRotation = t.gameObject.transform.rotation;
            Undo.RecordObject(t, "Undo Street modifier");

            /////////
            if (Event.current.type == EventType.Repaint)
            {
                Handles.color = Color.blue;
                Handles.Label(t.transform.position + Vector3.up * 2,
                                     "Depth: " + t.blockDepth + "cs units (" + t.blockDepth * 3 + "m) \n" +
                                     "Width: " + t.blockWidth + "cs units (" + t.blockWidth * 3 + "m) \n" 
                                     );



                Vector3 pos = t.transform.position;

                Vector3[] verts = new Vector3[] {
                                            newRotation * (new Vector3(pos.x + t.blockWidth *3, pos.y, pos.z+ t.blockDepth *3)- t.gameObject.transform.position) +t.gameObject.transform.position ,
                                            newRotation * (new Vector3(pos.x + t.blockWidth*3, pos.y, pos.z) - t.gameObject.transform.position) +t.gameObject.transform.position ,
                                            newRotation * (new Vector3(pos.x, pos.y,pos.z) - t.gameObject.transform.position) +t.gameObject.transform.position ,
                                            newRotation * (new Vector3(pos.x, pos.y, pos.z + t.blockDepth * 3) - t.gameObject.transform.position) +t.gameObject.transform.position };

                Handles.DrawSolidRectangleWithOutline(verts, new Color(0, 0, 0, 0.1f), new Color(1, 1, 1, 0.2f));
                Handles.color = Color.white;
                float bwdth = t.blockWidth;

                // foreach (Vector3 posCube in verts)
                // bwdth = Handles.ScaleValueHandle(t.blockWidth, posCube, Quaternion.identity, 1, Handles.CubeCap, 1);
                t.blockWidth = Mathf.CeilToInt(bwdth);
            }

            /// Advertising panels configuration
            if (Event.current.type == EventType.Repaint)
            {
                Handles.color = Color.red;
                Vector3 pos = t.transform.position;

                Vector3[] verts = new Vector3[] {
                                            newRotation * (new Vector3(pos.x + t.sectionWidth *3, pos.y, pos.z+ t.sectionDepth *3)- t.gameObject.transform.position) + t.gameObject.transform.position ,
                                            newRotation * (new Vector3(pos.x + t.sectionWidth*3, pos.y, pos.z)- t.gameObject.transform.position) + t.gameObject.transform.position ,
                                            newRotation * (new Vector3(pos.x, pos.y, pos.z)- t.gameObject.transform.position) + t.gameObject.transform.position ,
                                            newRotation * (new Vector3(pos.x, pos.y, pos.z + t.sectionDepth * 3)- t.gameObject.transform.position) + t.gameObject.transform.position };

                Handles.DrawSolidRectangleWithOutline(verts, new Color(0, 0, 0, 0.1f), new Color(1, 1, 1, 0.2f));
                Handles.color = Color.white;
                float bwdth = t.sectionWidth;

                // foreach (Vector3 posCube in verts)
                // bwdth = Handles.ScaleValueHandle(t.blockWidth, posCube, Quaternion.identity, 1, Handles.CubeCap, 1);
                t.sectionWidth = Mathf.CeilToInt(bwdth);
            }


            ////////
            //Show planes if configure prefab is active
            if (configurePrefab)
            {

            }

            EditorGUI.BeginChangeCheck();
            float bdepth = Handles.ScaleSlider(t.blockWidth + 0f, t.transform.position + newRotation * ((new Vector3(t.blockWidth * 3 - 5, 0, 0) - t.gameObject.transform.position) + t.gameObject.transform.position), t.transform.right, Quaternion.identity, 5, 3);
            //   float bdepth = t.blockWidth;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Width Slider");
                t.blockWidth = Mathf.FloorToInt(bdepth);
                t.UpdateCity();
            }

            EditorGUI.BeginChangeCheck();
            float width = Handles.ScaleSlider(t.blockDepth + 0f, t.transform.position + newRotation * ((new Vector3(0, 0, t.blockDepth * 3 - 5) - t.gameObject.transform.position) + t.gameObject.transform.position), t.transform.forward, Quaternion.identity, 5, 3);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Depth Slider");
                t.blockDepth = Mathf.FloorToInt(width);
                t.UpdateCity();
            }
            EditorGUI.BeginChangeCheck();
            float swidth = Handles.ScaleSlider(t.sectionWidth + 0f, t.transform.position + newRotation * ((new Vector3(t.sectionWidth * 3 - 8, 0, 0) - t.gameObject.transform.position) + t.gameObject.transform.position), t.transform.right, Quaternion.identity, 5, 3);
            //   float bdepth = t.blockWidth;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Street Width Slider");
                t.sectionWidth = Mathf.FloorToInt(swidth);
                t.UpdateCity();
            }

            EditorGUI.BeginChangeCheck();
            float sdepth= Handles.ScaleSlider(t.sectionDepth + 0f, t.transform.position + newRotation * ((new Vector3(0, 0, t.sectionDepth * 3 - 8) - t.gameObject.transform.position) + t.gameObject.transform.position), t.transform.forward, Quaternion.identity, 5, 3);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Street Depth Slider");
                t.sectionDepth = Mathf.FloorToInt(sdepth);
                t.UpdateCity();
            }

            Vector3 point = Camera.current.WorldToScreenPoint(t.transform.position);
            float size = HandleUtility.GetHandleSize(point);

            EditorGUI.BeginChangeCheck();
            Handles.color = new Color(0, 1, 1, 1);
            int oldDepthX = t.blockDepth;
            width = Handles.ScaleValueHandle(t.blockDepth + 0f, (t.transform.position) + newRotation * ((new Vector3(t.blockWidth * 1.5f, 0, t.blockDepth * 3) - t.gameObject.transform.position) + t.gameObject.transform.position), t.transform.rotation * Quaternion.LookRotation(Vector3.forward), 10, Handles.CircleHandleCap, 3);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Depth Slider");
                t.blockDepth = Mathf.FloorToInt(width);
                if (oldDepthX != t.blockDepth) { 
                int difference =  t.blockDepth - oldDepthX ;
                    t.sectionDepth = t.sectionDepth + difference;
                }
                t.UpdateCity();
            }

            EditorGUI.BeginChangeCheck();
            Handles.color = new Color(1, 0, 0, 1);
            int oldWidthX = t.blockWidth;
            bdepth = Handles.ScaleValueHandle(t.blockWidth + 0f, (t.transform.position) + newRotation * ((new Vector3(t.blockWidth * 3, 0, t.blockDepth * 1.5f) - t.gameObject.transform.position) + t.gameObject.transform.position), t.transform.rotation * Quaternion.LookRotation(Vector3.right), 10, Handles.CircleHandleCap, 3);
            //   float bdepth = t.buildingWidth;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Width Slider");
                t.blockWidth = Mathf.FloorToInt(bdepth);
                if (oldWidthX != t.blockWidth)
                {
                    int difference = t.blockWidth - oldWidthX;
                    t.sectionWidth = t.sectionWidth + difference;
                }
                t.UpdateCity();
            }

            EditorGUI.BeginChangeCheck();
            Handles.color = new Color(1, 1, 0, 1);
            int oldDepth = t.blockDepth;
            int oldSectionDepth = t.sectionDepth;
            float depth2 = Handles.ScaleValueHandle(t.blockDepth, (t.transform.position) + newRotation * ((new Vector3(t.blockWidth * 1.5f, 0, 0) - t.gameObject.transform.position) + t.gameObject.transform.position), t.transform.rotation * Quaternion.LookRotation(Vector3.forward), 10, Handles.CircleHandleCap, -3);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Depth2 Slider");

                t.blockDepth = Mathf.FloorToInt(depth2);

                t.UpdateCity();
                if (oldDepth != t.blockDepth)
                {
                    t.gameObject.transform.Translate(0f, 0f, (oldDepth - t.blockDepth) * 3);
                   
                }
            }

            EditorGUI.BeginChangeCheck();
            Handles.color = new Color(1, 0, 1, 1);
            int oldWidth = t.blockWidth;
            float width2 = Handles.ScaleValueHandle(t.blockWidth, (t.transform.position) + newRotation * ((new Vector3(0, 0, t.blockDepth * 1.5f) - t.gameObject.transform.position) + t.gameObject.transform.position), t.transform.rotation * Quaternion.LookRotation(Vector3.right), 10, Handles.CircleHandleCap, 3);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Width2 Slider");

                t.blockWidth = Mathf.FloorToInt(width2);

                t.UpdateCity();
                if (oldWidth != t.blockWidth)
                    t.gameObject.transform.Translate((oldWidth - t.blockWidth) * 3, 0f, 0f);
            }
        }



  
    }
}