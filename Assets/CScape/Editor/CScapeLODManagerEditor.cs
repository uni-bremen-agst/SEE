using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CScape;
using UnityEditor;

namespace CScape {
    [CustomEditor(typeof(CScapeLODManager))]

    public class CScapeStreetLightsLODEditor : Editor {
        
       

        public override void OnInspectorGUI()
        {
            CScapeLODManager ce = (CScapeLODManager)target;

            GUILayout.BeginVertical("box");
            ce.polesDistance = EditorGUILayout.IntField("Poles distance", ce.polesDistance);

            if (GUILayout.Button("Update LOD'S"))
            {
               ce.UpdateLightpoleLods();
            }

            GUILayout.EndVertical();
            GUILayout.BeginVertical("box");
            ce.treeDistance = EditorGUILayout.IntField("Trees distance", ce.treeDistance);

            if (GUILayout.Button("Update LOD'S"))
            {
                ce.UpdateTreesLods();
            }
            GUILayout.EndVertical();
            GUILayout.BeginVertical("box");
            ce.lightsDistance = EditorGUILayout.IntField("Lights Distance", ce.lightsDistance);

            if (GUILayout.Button("Update Lights"))
            {
                ce.UpdateLightsLods();
            }
            GUILayout.EndVertical();
            
            GUILayout.BeginVertical("box");
            ce.rooftopDensity = EditorGUILayout.IntField("Rooftop Density", ce.rooftopDensity);
            ce.rooftopsCullingSize = EditorGUILayout.Slider("Rooftop Culling Distance", ce.rooftopsCullingSize, 0f, 1f);
            if (GUILayout.Button("Update Rooftop LOD'S"))
            {
                ce.UpdateRooftopCulling();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            ce.useRooftops = EditorGUILayout.Toggle("Use Advertising panels", ce.useRooftops);
            ce.advertsDensity = EditorGUILayout.IntField("advertising Density", ce.advertsDensity);
            
            ce.advertsCullingSize = EditorGUILayout.Slider("Advertising Culling Distance", ce.advertsCullingSize, 0f, 1f);
            if (GUILayout.Button("Update advertising LOD'S"))
            {
                ce.UpdateAdvertisingCulling();
            }

            GUILayout.EndVertical();
        }
    }
}