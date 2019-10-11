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
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]

    public class CSFoliageInstantiator : MonoBehaviour
    {


        public int instancesX;
        public int instancesZ;
    //    private Vector3[] vecW;
    //    private Vector3[] vecH;
        public Material mat;
        public Mesh mesh;
        public Mesh meshOriginal;
        public GameObject originalObject;
        public int offsetX;
        public int offsetZ;
        public bool update;
        public int width;
        public int depth;
        public int maxMeshSize;
        public StreetModifier parentSection;
        public float enterOffset = 0.5f;
        bool isPrefabOriginal;

        // Use this for initialization
        public void Awake()
        {
            //originalVertices = meshOriginal.vertices;
            //originalUVs = meshOriginal.uv;
            //originalColors = meshOriginal.colors;

            //mesh = Instantiate(meshOriginal) as Mesh;
            //MeshFilter meshFilter = GetComponent<MeshFilter>();
            //meshFilter.mesh = mesh;
  //          parentSection = gameObject.transform.parent.gameObject.GetComponent<StreetModifier>();
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

                //  maxMeshSize = Mathf.CeilToInt(65000f / (originalObject.GetComponent<MeshFilter>().sharedMesh.vertices.Length * 2));
                //Debug.Log(maxMeshSize + ", " + originalObject.GetComponent<MeshFilter>().sharedMesh.vertices.Length);

                //  if (instancesX > maxMeshSize) instancesX = maxMeshSize;
                if (instancesX < 1) instancesX = 1;
                Vector3 baseOffset = new Vector3(enterOffset, 0, enterOffset);
                Vector3 baseOffset2 = new Vector3(enterOffset, 0, -enterOffset);
                Vector3 baseOffsetSymetry = new Vector3(enterOffset, 0, -enterOffset);

                for (int j = 0; j < instancesX; j++)
                {
                    GameObject newObject = Instantiate(originalObject) as GameObject;

                    newObject.transform.parent = gameObject.transform;
                    newObject.transform.localPosition = new Vector3((j * offsetX), 0, 0) - baseOffset;

                    newObject.transform.Rotate(new Vector3(0, Random.Range(-360, 360), 0));
                }

                for (int j = 0; j < instancesX; j++)
                {
                    GameObject newObject = Instantiate(originalObject) as GameObject;
                    newObject.transform.parent = gameObject.transform;
                    newObject.transform.localPosition = new Vector3((j * offsetX), 0, (width) * 3) - baseOffsetSymetry;

                    newObject.transform.Rotate(new Vector3(0, Random.Range(-360, 360), 0));
                }

                for (int j = 0; j < instancesZ; j++)
                {
                    GameObject newObject = Instantiate(originalObject) as GameObject;
                    newObject.transform.parent = gameObject.transform;
                    newObject.transform.localPosition = new Vector3(0, 0, (j * offsetZ)) - baseOffset;

                    newObject.transform.Rotate(new Vector3(0, Random.Range(-360, 360), 0));
                }

                for (int j = 0; j < instancesZ; j++)
                {
                    GameObject newObject = Instantiate(originalObject) as GameObject;
                    newObject.transform.parent = gameObject.transform;
                    newObject.transform.localPosition = new Vector3((depth * 3f), 0, (j * offsetZ)) + baseOffset2;

                    newObject.transform.Rotate(new Vector3(0, Random.Range(-360, 360), 0));
                }

                //for (int j = 0; j < instancesZ; j++)
                //{
                //    GameObject newObject = Instantiate(originalObject) as GameObject;
                //    // newObject.GetComponent<MeshFilter>().mesh = meshOriginal;
                //    newObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, gameObject.transform.position.z  +(depth - 1) * 3) - gameObject.transform.position - baseOffset;
                //    newObject.transform.parent = gameObject.transform;
                //    newObject.transform.Rotate(new Vector3(0, 0, 90));
                //}

                // MergeMeshes();

                //mesh.vertices = vertices;
                //mesh.colors = vColors;
                //mesh.uv = uV;
                // mesh.RecalculateBounds();
            }
        }

        public void MergeMeshes()
        {

            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];
            int i = 0;
            while (i < meshFilters.Length)
            {
                
                combine[i].mesh = meshFilters[i].sharedMesh;
                  combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
               // combine[i].transform = meshFilters[i].transform.worldToLocalMatrix;
             //  DestroyImmediate(meshFilters[i].gameObject);
             //   meshFilters[i].gameObject.SetActive(false);
                i++;
            }
            transform.GetComponent<MeshFilter>().sharedMesh = new Mesh();

            transform.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine);

            transform.gameObject.SetActive(true);
            foreach (Transform go in gameObject.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }
        }

        public void DeleteSolution()
        {

            foreach (Transform go in gameObject.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }
            DestroyImmediate(transform.GetComponent<MeshFilter>().sharedMesh);
        }
    }
}
//#endif