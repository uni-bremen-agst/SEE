//#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CScape;
//using UnityEditor;
//using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Linq;
//using UnityEditor;

namespace CScape
{
    [RequireComponent(typeof(Light))]

    public class CSInstantiatorLights : MonoBehaviour
    {


        public int instancesX;
        public int instancesZ;
        public Material mat;
        public Light lightOriginal;
        public GameObject originalObject;
        public int offsetX;
        public int offsetZ;
        public bool update;
        public int width;
        public int depth;
        public int maxMeshSize;
        public StreetModifier parentSection;
        bool isPrefabOriginal;

        // Use this for initialization
        public void Awake()
        {

            UpdateElements();
        }

        // Update is called once per frame

        public void UpdateElements()
        {
#if UNITY_EDITOR
            isPrefabOriginal = UnityEditor.PrefabUtility.GetPrefabParent(gameObject) == null && UnityEditor.PrefabUtility.GetPrefabObject(gameObject.transform) != null;
#endif
            if (!isPrefabOriginal)
            {


                instancesX = ((depth + 1) * 3 / offsetX);
                instancesZ = ((width + 1) * 3 / offsetZ);
                DeleteSolution();

                if (instancesX < 1) instancesX = 1;
                Vector3 baseOffset = new Vector3(0.5f, 0, 0.5f);
                Vector3 baseOffset2 = new Vector3(0.5f, 0, -0.5f);
                Vector3 baseOffsetSymetry = new Vector3(0.5f, 0, -0.5f);

                for (int j = 0; j < instancesX; j++)
                {
                    GameObject newObject = Instantiate(originalObject) as GameObject;
                    newObject.transform.position = new Vector3(gameObject.transform.position.x + (j * offsetX) + (offsetX*0.5f), gameObject.transform.position.y, gameObject.transform.position.z) + baseOffset;
                    newObject.transform.parent = gameObject.transform;
                    newObject.transform.Rotate(new Vector3(0, 0, 180));
                }

                for (int j = 0; j < instancesX; j++)
                {
                    GameObject newObject = Instantiate(originalObject) as GameObject;
                    newObject.transform.position = new Vector3(gameObject.transform.position.x + (j * offsetX), gameObject.transform.position.y, gameObject.transform.position.z + (width) * 3) + baseOffsetSymetry;
                    newObject.transform.parent = gameObject.transform;
                    newObject.transform.Rotate(new Vector3(0, 0, 0));
                }

                for (int j = 0; j < instancesZ; j++)
                {
                    GameObject newObject = Instantiate(originalObject) as GameObject;
                    newObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, gameObject.transform.position.z + (j * offsetZ) + (offsetZ * 0.5f))  + baseOffset;
                    newObject.transform.parent = gameObject.transform;
                    newObject.transform.Rotate(new Vector3(0, 0, -90));
                }

                for (int j = 0; j < instancesZ; j++)
                {
                    GameObject newObject = Instantiate(originalObject) as GameObject;
                    newObject.transform.position = new Vector3(gameObject.transform.position.x + (depth * 3f), gameObject.transform.position.y, gameObject.transform.position.z + (j * offsetZ)) - baseOffset2;
                    newObject.transform.parent = gameObject.transform;
                    newObject.transform.Rotate(new Vector3(0, 0, 90));
                }



            }
        }

 

        public void DeleteSolution()
        {

            foreach (Transform go in gameObject.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }
        }
    }
}
//#endif