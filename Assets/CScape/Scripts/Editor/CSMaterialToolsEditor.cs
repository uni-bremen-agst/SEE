using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace CScape
{
    [CustomEditor(typeof(CSMaterialTools))]
    public class CSMaterialToolsEditor : Editor
    {
        bool options = false;


        void OnEnable()
        {
            CSMaterialTools bm = (CSMaterialTools)target;




        }

        public void OnSceneGUI()
        {
            CSMaterialTools bm = (CSMaterialTools)target;


        }

        public override void OnInspectorGUI()
        {
            CSMaterialTools mt = (CSMaterialTools)target;
            options = EditorGUILayout.Foldout(options, "compiler options", false);
            if (options)
            {
                
                mt.TextureFolder = EditorGUILayout.TextField("TextureFolder", mt.TextureFolder);
                mt.size = EditorGUILayout.IntField("Shape texture size", mt.size);
                mt.surfaceSize = EditorGUILayout.IntField("Surface texture size", mt.surfaceSize);
                mt.entSize = EditorGUILayout.IntField("Interior texture size", mt.entSize);
                mt.shopsSize = EditorGUILayout.IntField("Shops texture size", mt.shopsSize);
                mt.Dirt_IlluminationSize = EditorGUILayout.IntField("Shops texture size", mt.Dirt_IlluminationSize);
                mt.blindsSize = EditorGUILayout.IntField("Blinds texture size", mt.blindsSize);
                mt.decallsSize = EditorGUILayout.IntField("Street Decalls texture size", mt.decallsSize);
                mt.streetsSize = EditorGUILayout.IntField("Street texture size", mt.streetsSize);
                mt.cityMaterial = EditorGUILayout.ObjectField("Buildings Material ", mt.cityMaterial, typeof(Material), true) as Material;
                mt.streetsMaterial = EditorGUILayout.ObjectField("Buildings Material ", mt.streetsMaterial, typeof(Material), true) as Material;

            }

            if (GUILayout.Button("Compile only styles"))
                {
                    mt.CreateStyleShapes();
                }

                if (GUILayout.Button("Compile all textures"))
                {
                    mt.CreateStyleShapes();
                    mt.CreateMaterialsNew();
                    mt.CreateBlinds();
                    mt.CreateStreets();
                    mt.CreateDirt();
                    mt.CreateInt();
                    mt.CreateShops();
                    mt.CreateStreetDecalls();
                }
            



        }
            // Use this for initialization

    }
}
