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

    public class CSInstantiator : MonoBehaviour
    {


        public int instancesX;
        public int instancesZ;
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
        public LODGroup lodGroup;
        bool isPrefabOriginal;
        public bool useSkewing = false;
        public float skewAngleFront;
        public float skewAngleBack;
        public float skewAngleLeft;
        public float skewAngleRight;
        public float baseOffset = 0.5f;
        public int slitFront;
        public int slitBack;
        public int slitLeft;
        public int slitRight;
        public GameObject streetParent;
        public float searchOffset = 30f;
        public StreetModifier sm;

        // Use this for initialization
        public void AwakeMe()
        {
            lodGroup = GetComponent<LODGroup>();
            UpdateElements();
        }

        // Update is called once per frame

        public void UpdateElements()
        {
#if UNITY_EDITOR
            isPrefabOriginal = UnityEditor.PrefabUtility.GetPrefabParent(gameObject) == null && UnityEditor.PrefabUtility.GetPrefabObject(gameObject.transform) != null;
#endif

            sm = streetParent.GetComponent<StreetModifier>();

            if (!isPrefabOriginal)
            {
                if (streetParent)
                {
                    
                    if (sm)
                    {
                        slitFront = sm.slitRB;
                        slitBack = sm.slitLF;
                    }

                }
                else
                {
                    skewAngleFront = 0;
                    skewAngleBack = 180;
                    skewAngleRight = -90;
                    skewAngleLeft = 90;

                }

                instancesX = ((depth - 1) * 3 / offsetX);
                instancesZ = ((width - 1) * 3 / offsetZ);
                DeleteSolution();

                maxMeshSize = Mathf.CeilToInt(65000f / (originalObject.GetComponent<MeshFilter>().sharedMesh.vertices.Length * 2));
                //Debug.Log(maxMeshSize + ", " + originalObject.GetComponent<MeshFilter>().sharedMesh.vertices.Length);

                if (instancesX > maxMeshSize) instancesX = maxMeshSize;
                if (instancesX < 1) instancesX = 1;

                Vector3 baseOffset2 = new Vector3(0.5f, 0, -0.5f);
                Vector3 baseOffsetSymetry = new Vector3(0.5f, 0, -0.5f);

                NotSlatedBehaviour();

                foreach (Transform go in gameObject.transform.Cast<Transform>().Reverse())
                {
                    go.transform.position = new Vector3(go.transform.position.x + gameObject.transform.position.x, go.transform.position.y, go.transform.position.z + gameObject.transform.position.z);
                    RaycastHit hit;
                    Vector3 fwd = go.transform.TransformDirection(new Vector3(0, 0, -1));
                    Vector3 pos = go.transform.TransformPoint(new Vector3(0, 0, 0));

                    if (Physics.Raycast(pos, fwd, out hit))
                    {
                        if (hit.distance < 30)
                        {
                            go.transform.Translate(new Vector3(0, 0, -hit.distance));
                        }
                        else DestroyImmediate(go);

                    }
                    go.transform.position = new Vector3(go.transform.position.x - gameObject.transform.position.x, go.transform.position.y, go.transform.position.z - gameObject.transform.position.z);

                    // else DestroyImmediate(go.gameObject);
                }



               MergeMeshes();

                // MeshCollider colliderMesh = GetComponent(typeof(MeshCollider)) as MeshCollider;



                //mesh.vertices = vertices;
                //mesh.colors = vColors;
                //mesh.uv = uV;
                //mesh.RecalculateBounds();

            }
        }

        private void NotSlatedBehaviour()
        {
            sm.CalculateReferencePoints();



            for (int j = 0; j < instancesX - 1; j++)
            {
                GameObject newObject = Instantiate(originalObject) as GameObject;
                // newObject.GetComponent<MeshFilter>().mesh = meshOriginal;
                newObject.transform.localPosition = sm.frontStartPoint + new Vector3(baseOffset + 3, searchOffset, baseOffset);
                if (sm.useSkewFB) newObject.transform.Rotate(new Vector3(0, 0, sm.skewRotationFront));
                newObject.transform.Translate(new Vector3(j * offsetX, 0, 0));
                newObject.transform.parent = gameObject.transform;
                newObject.transform.Rotate(new Vector3(0, 0, 180));

            }

            for (int j = 0; j < instancesX - 1; j++)
            {
                GameObject newObject = Instantiate(originalObject) as GameObject;
                // newObject.GetComponent<MeshFilter>().mesh = meshOriginal;
                newObject.transform.localPosition = sm.backStartPoint + new Vector3(-baseOffset - 3, searchOffset, -baseOffset);
                if (sm.useSkewFB) newObject.transform.Rotate(new Vector3(0, 0, sm.skewRotationBack + 180));
                else newObject.transform.Rotate(new Vector3(0, 0, 0));
                newObject.transform.Translate(new Vector3(j * -offsetX, 0, 0));
                newObject.transform.parent = gameObject.transform;
                newObject.transform.Rotate(new Vector3(0, 0, 0));
            }

            for (int j = 0; j < instancesZ - 1; j++)
            {
                GameObject newObject = Instantiate(originalObject) as GameObject;
                // newObject.GetComponent<MeshFilter>().mesh = meshOriginal;
                
                newObject.transform.localPosition = sm.rightStartPoint + new Vector3(-baseOffset, searchOffset, baseOffset + 3);
                if (sm.useSkewLR) newObject.transform.Rotate(new Vector3(0, 0, sm.skewRotationRight));
                else newObject.transform.Rotate(new Vector3(0, 0, -90));

                newObject.transform.Translate(new Vector3(j * offsetZ, 0, 0));
                newObject.transform.parent = gameObject.transform;
                newObject.transform.Rotate(new Vector3(0, 0, 180));
            }

            for (int j = 0; j < instancesZ - 1; j++)
            {
                GameObject newObject = Instantiate(originalObject) as GameObject;
                // newObject.GetComponent<MeshFilter>().mesh = meshOriginal;
                newObject.transform.localPosition = sm.leftStartPoint + new Vector3(baseOffset, searchOffset, -baseOffset - 3);
                
                if (sm.useSkewLR) newObject.transform.Rotate(new Vector3(0, 0, sm.skewRotationLeft));
                else newObject.transform.Rotate(new Vector3(0, 0, 90));

                newObject.transform.Translate(new Vector3(j * offsetZ, 0, 0));
                newObject.transform.parent = gameObject.transform;
                newObject.transform.Rotate(new Vector3(0, 0, 180));
            }



            
        }

        public void MergeMeshes()
        {

            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combine = new CombineInstance[meshFilters.Length - 1];

            int index = 0;
            for (int i = 0; i < meshFilters.Length; i++)
            {
                if (meshFilters[i].sharedMesh != null)
                {
                    // if (meshFilters[i].sharedMesh == null) continue;
                    combine[index].mesh = meshFilters[i].sharedMesh;
                    combine[index++].transform = meshFilters[i].transform.localToWorldMatrix;
                }
            }
            MeshFilter meshF = transform.GetComponent<MeshFilter>();
            meshF.sharedMesh = new Mesh();
            meshF.sharedMesh.name = "lightpoles";
            meshF.sharedMesh.CombineMeshes(combine);
            meshF.sharedMesh.RecalculateBounds();
            if (lodGroup != null) lodGroup.RecalculateBounds();

            //    transform.gameObject.SetActive(true);
            foreach (Transform go in gameObject.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }

            MeshCollider mColl = gameObject.GetComponent<MeshCollider>();
            mColl.sharedMesh = meshF.sharedMesh;
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