
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CScape;
//using UnityEditor;
using UnityEngine.SceneManagement;
using System.Linq;

namespace CScape
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(BuildingModifier))]

    public class CSFloorDetails : MonoBehaviour
    {
        //#if UNITY_EDITOR

        public int instancesX;
        public int instancesZ;
        public Material mat;
        public Mesh mesh;
        public Mesh meshOriginal;
        //  public GameObject originalObject;
        public GameObject[] greebleElements;
        public int offsetX;
        public int offsetZ;
        public bool update;
        public int width;
        public int depth;
        public int maxMeshSize;
        public StreetModifier parentSection;
        public GameObject parentBuilding;
        public GameObject rooftopHolder;
        public GameObject rooftopHolderPrefab;
        public GameObject[] rooftopElements;
        public BuildingModifier bmComponent;
        public CityRandomizer parentCity;
        public int randomSeed;
        bool isPrefabOriginal;
        public MeshRenderer rh;
        public Material greebleMat;
        public LODGroup lodComponent;
        public GameObject roofTopsObject;
        public float lodDistance = 0.2f;
        public bool animateLodFade = true;
        public bool useRooftops = true;
        public bool noMesh = false;
        public Vector3 offsetVec;
        public int skip = 0;
        public bool repeatVertical = false; 


        public void AwakeMe()
        {

            lodDistance = 0.2f;
            if (gameObject.activeInHierarchy)
            {
                if (rooftopElements.Length == 0) System.Array.Resize(ref rooftopElements, rooftopElements.Length + 1);
                bmComponent = gameObject.GetComponent<BuildingModifier>();
                if (rooftopHolder == null)
                {
                    rooftopHolder = new GameObject(gameObject.transform.name + "_rooftop");
#if UNITY_EDITOR
                    if (rooftopHolder.GetComponent<LODGroup>() == null)
                    {
                        lodComponent = rooftopHolder.AddComponent<LODGroup>();
                        UnityEditor.SerializedObject obj = new UnityEditor.SerializedObject(lodComponent);

                        UnityEditor.SerializedProperty valArrProp = obj.FindProperty("m_LODs.Array");
                        for (int i = 0; valArrProp.arraySize > i; i++)
                        {
                            UnityEditor.SerializedProperty sHeight = obj.FindProperty("m_LODs.Array.data[" + i.ToString() + "].screenRelativeHeight");

                            if (i == 0)
                            {
                                sHeight.doubleValue = 0.8;
                            }
                            if (i == 1)
                            {
                                sHeight.doubleValue = 0.5;
                            }
                            if (i == 2)
                            {
                                sHeight.doubleValue = 0.1;
                            }
                        }
                        obj.ApplyModifiedProperties();
                    }
#endif

                }
                UpdateElements();
            }
        }


        public void UpdateElements()
        {



            if (bmComponent.supportSkewing) useRooftops = false;
            if (gameObject.activeInHierarchy && useRooftops)
            {
                if (bmComponent == null) bmComponent = gameObject.GetComponent<BuildingModifier>();
                if (rooftopHolder == null)
                {
                    rooftopHolder = new GameObject(gameObject.transform.name + "_rooftop");
                    if (rooftopHolder.GetComponent<LODGroup>() == null)
                    {
#if UNITY_EDITOR
                        lodComponent = rooftopHolder.AddComponent<LODGroup>();
                        rooftopHolder.AddComponent<MeshFilter>();
                        rooftopHolder.AddComponent<MeshRenderer>();
                        rooftopHolder.transform.parent = gameObject.transform;
                        rooftopHolder.transform.localPosition = new Vector3(0, 0, 0);
                        UnityEditor.SerializedObject obj = new UnityEditor.SerializedObject(lodComponent);

                        UnityEditor.SerializedProperty valArrProp = obj.FindProperty("m_LODs.Array");
                        for (int k = 0; valArrProp.arraySize > k; k++)
                        {
                            UnityEditor.SerializedProperty sHeight = obj.FindProperty("m_LODs.Array.data[" + k.ToString() + "].screenRelativeHeight");

                            if (k == 0)
                            {
                                sHeight.doubleValue = 0.8;
                            }
                            if (k == 1)
                            {
                                sHeight.doubleValue = 0.5;
                            }
                            if (k == 2)
                            {
                                sHeight.doubleValue = 0.1;
                            }
                        }
                        obj.ApplyModifiedProperties();
#endif
                    }
                }
                Random.InitState(randomSeed);
                int i = 0;

                if (rooftopElements.Length == 0) System.Array.Resize(ref rooftopElements, rooftopElements.Length + 1);
#if UNITY_EDITOR
                isPrefabOriginal = UnityEditor.PrefabUtility.GetPrefabParent(gameObject) == null && UnityEditor.PrefabUtility.GetPrefabObject(gameObject.transform) != null;
#endif
                if (!isPrefabOriginal)
                {
                    if (rooftopHolder == null)
                    {
                        rooftopHolder = new GameObject();
                        rooftopHolder.transform.position = gameObject.transform.position;
                        
                        rooftopHolder.transform.parent = gameObject.transform;
                        rooftopHolder.AddComponent<MeshFilter>();
                        rooftopHolder.transform.name = "ShopDetails_" + gameObject.transform.name;
                    }
                    DeleteSolution();

                    int skipCounter = 0;
                    if (!repeatVertical)
                    {
                        for (int j = 0; j < bmComponent.buildingWidth; j++)
                        {

                            if (skipCounter == 0)
                            {

                                GameObject newObject = Instantiate(rooftopElements[Random.Range(0, rooftopElements.Length)]) as GameObject;
                                newObject.transform.parent = rooftopHolder.transform;
                                newObject.transform.Rotate(0, 0, 180);
                                newObject.transform.position = new Vector3(j * 3f, 0, 0) + offsetVec;
                            }
                            skipCounter++;
                            if (skipCounter >= skip)
                                skipCounter = 0;
                        }

                    }

                    else
                    {
                        for (int j = 0; j < bmComponent.buildingWidth; j++)
                        {
                            for (int k = 0; k < bmComponent.buildingWidth; k++) { 
                                if (skipCounter == 0)
                            {

                                GameObject newObject = Instantiate(rooftopElements[Random.Range(0, rooftopElements.Length)]) as GameObject;
                                newObject.transform.parent = rooftopHolder.transform;
                                newObject.transform.Rotate(0, 0, 180);
                                newObject.transform.position = new Vector3(j * 3f, k * 3f, 0) + offsetVec;
                            }
                            skipCounter++;
                                if (skipCounter >= skip)
                                    skipCounter = 0;
                            }
                        }



                    }
                    if (rooftopHolder.GetComponentsInChildren<Renderer>().Length > 1)

                        MergeMeshes();
                    else
                    {
                        DestroyImmediate(rooftopHolder);
                        return;
                    }



                    MeshCollider mColl = rooftopHolder.GetComponent<MeshCollider>();
                    if (!rooftopHolder.GetComponent<MeshCollider>())
                    {
                        mColl = rooftopHolder.AddComponent<MeshCollider>();


                    }
                    mColl.convex = false;
                    MeshFilter meshF = rooftopHolder.transform.GetComponent<MeshFilter>();
                    mColl.sharedMesh = meshF.sharedMesh;

                    if (roofTopsObject == null && bmComponent.isInRoot)
                    {
                        rooftopHolder.transform.parent = bmComponent.cityRandomizerParent.transform.Find("RooftopDetails");

                    }
                    else rooftopHolder.transform.parent = bmComponent.gameObject.transform;

                    rooftopHolder.transform.position = gameObject.transform.position;
                    rooftopHolder.transform.rotation = gameObject.transform.rotation;
                    rooftopHolder.transform.localScale = new Vector3(1, bmComponent.scale, 1); ;


                }
            }
            if ((!useRooftops && rooftopHolder)) DeleteSolution();

        }
        public void MergeMeshes()
        {


            // else Debug.LogWarning("Please assign rooftop material in CS Rooftops Component");


            MeshFilter[] meshFilters = rooftopHolder.GetComponentsInChildren<MeshFilter>();



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
            MeshFilter meshF = rooftopHolder.transform.GetComponent<MeshFilter>();
            meshF.sharedMesh = new Mesh();
            meshF.sharedMesh.CombineMeshes(combine);

            ///assign new color


            //    Mesh newMesh = new Mesh();
            //    newMesh.vertices = meshF.sharedMesh.vertices;
            //    newMesh.normals = meshF.sharedMesh.normals;
            //    newMesh.uv = meshF.sharedMesh.uv;
            //    newMesh.colors = new Color[meshF.sharedMesh.vertices.Length];
            //    newMesh.triangles = meshF.sharedMesh.triangles;
            //    for (int v = 0; v < newMesh.colors.Length; v++)
            //    {
            //        newMesh.colors[v] = new Color(0, 0, 1, 0);
            //    }

            //meshF.sharedMesh = newMesh;



            meshF.sharedMesh.RecalculateBounds();

            if (lodComponent != null) lodComponent.RecalculateBounds();

            foreach (Transform go in rooftopHolder.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }

            if (rooftopHolder.GetComponent<MeshRenderer>() == null)
            {
                rh = rooftopHolder.AddComponent<MeshRenderer>() as MeshRenderer;
                rh = rooftopHolder.GetComponent<MeshRenderer>();
                rh.sharedMaterial = greebleMat;

            }
            rh = rooftopHolder.GetComponent<MeshRenderer>();
            rh.sharedMaterial = greebleMat;
            Renderer[] renderers = new Renderer[1];
            LOD[] lod = new LOD[1];
            renderers[0] = rooftopHolder.GetComponent<Renderer>();
            lod[0] = new LOD(lodDistance, renderers);
            lodComponent.SetLODs(lod);
            if (animateLodFade)
                lodComponent.fadeMode = LODFadeMode.CrossFade;
            else lodComponent.fadeMode = LODFadeMode.None;
            lodComponent.animateCrossFading = animateLodFade;

            //if (meshF.sharedMesh.vertices.Length < 3)
            //{
            //    DestroyImmediate(rooftopHolder);
            //    noMesh = true;
            //}
            //else noMesh = false;

        }


        public void DeleteSolution()
        {
            if (rooftopHolder)
            {
                foreach (Transform go in rooftopHolder.transform.Cast<Transform>().Reverse())
                {
                    DestroyImmediate(go.gameObject);
                }
                if (rooftopHolder.GetComponent<MeshFilter>())
                    DestroyImmediate(rooftopHolder.GetComponent<MeshFilter>().sharedMesh);
            }
        }

        void OnDestroy()
        {
            foreach (Transform go in rooftopHolder.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }
            DestroyImmediate(rooftopHolder.GetComponent<MeshFilter>().sharedMesh);
            DestroyImmediate(rooftopHolder);
        }

        //void OnDrawGizmosSelected()
        //{
        //    Gizmos.color = Color.yellow;
        //    Gizmos.DrawWireCube(greebleArea.center, greebleArea.extents);
        //    Gizmos.color = Color.red;
        //    Gizmos.DrawWireCube(greebleAreaX.center, greebleAreaX.extents);
        //    Gizmos.DrawWireCube(greebleAreaZ.center, greebleAreaZ.extents);
        //}
        //#endif
    }

}

//#endif