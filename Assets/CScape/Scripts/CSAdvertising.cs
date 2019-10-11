
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CScape;
//using UnityEditor;
//using UnityEditor;
using UnityEngine.SceneManagement;
using System.Linq;

namespace CScape
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(BuildingModifier))]

    public class CSAdvertising : MonoBehaviour
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
   //     private Bounds greebleArea;
        private Bounds greebleAreaX;
        private Bounds greebleAreaZ;
        bool isPrefabOriginal;
        public MeshRenderer rh;
        public Material greebleMat;
        public LODGroup lodComponent;
        public GameObject roofTopsObject;
        public float lodDistance = 0.7f;
        public Vector3[] advertOffsetX;
        public Vector3[] advertOffsetZ;
        public float randomScaleMin = 1f;
        public float randomScaleMax = 1.5f;
        public bool useAdvertising = true;
        public bool animateLodFade = true;
        public bool noMesh = false;



        public void AwakeMe()
        {
            
            if (useAdvertising && gameObject.activeInHierarchy) { 
            if (rooftopElements.Length == 0) System.Array.Resize(ref rooftopElements, rooftopElements.Length + 1);
            bmComponent = gameObject.GetComponent<BuildingModifier>();
            if (rooftopHolder == null)
            {
                    rooftopHolder = new GameObject(gameObject.transform.name + "_Advertising");
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

            if (bmComponent.useAdvertising && gameObject.activeInHierarchy)
            {
                if (rooftopHolder == null)
                {
                    rooftopHolder = new GameObject(gameObject.transform.name + "_Advertising");
#if UNITY_EDITOR
                    if (rooftopHolder.GetComponent<LODGroup>() == null)
                    {
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
                    }
#endif
                }
                Random.InitState(randomSeed);
                int i = 0;

                Vector3 minLineX = new Vector3(gameObject.transform.position.x + bmComponent.buildingWidth * 3 + bmComponent.advertOffsetX[i].x, gameObject.transform.position.y + bmComponent.advertOffsetY[i], gameObject.transform.position.z + bmComponent.advertOffsetX[i].y);
                Vector3 minLineZ = new Vector3(gameObject.transform.position.x + bmComponent.advertOffsetZ[i].x, gameObject.transform.position.y + bmComponent.advertOffsetY[i], gameObject.transform.position.z + bmComponent.advertOffsetX[i].y);
                Vector3 minX = new Vector3(gameObject.transform.position.x + bmComponent.buildingWidth * 3 + bmComponent.roofOffsetX[i].x, gameObject.transform.position.y + bmComponent.floorNumber * 3 + bmComponent.roofOffsetY[i], gameObject.transform.position.z + bmComponent.buildingWidth * 3 + bmComponent.roofOffsetX[i].y);
                Vector3 maxX = new Vector3(gameObject.transform.position.x + bmComponent.buildingWidth * 3 + bmComponent.roofOffsetX[i].x, gameObject.transform.position.y + bmComponent.floorNumber * 3 + bmComponent.roofOffsetY[i], gameObject.transform.position.z + bmComponent.roofOffsetZ[i].y);
                Vector3 minZ = new Vector3(gameObject.transform.position.x + bmComponent.roofOffsetZ[i].x, gameObject.transform.position.y + bmComponent.floorNumber * 3 + bmComponent.roofOffsetY[i], gameObject.transform.position.z + bmComponent.roofOffsetZ[i].y);
                Vector3 maxZ = new Vector3(gameObject.transform.position.x + bmComponent.roofOffsetZ[i].x, gameObject.transform.position.y + bmComponent.floorNumber * 3 + bmComponent.roofOffsetY[i], gameObject.transform.position.z + bmComponent.buildingWidth * 3 + bmComponent.roofOffsetX[i].y);

                float surfacedensity = (minX.z - maxX.z) * (minX.x - minZ.x) * instancesX / 10000;
                if (surfacedensity > 50) surfacedensity = 50;

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
                        rooftopHolder.transform.name = "Rooftop_" + gameObject.transform.name;
                    }
                    DeleteSolution();

                 //   greebleArea = new Bounds(Vector3.Lerp(minX, minZ, 0.5f) - rooftopHolder.transform.position, new Vector3(minX.x - minZ.x, 100, minX.z - maxX.z) * 2);
                    greebleAreaX = new Bounds(minLineZ - rooftopHolder.transform.position, new Vector3(0.1f, 200, 500));
                    greebleAreaZ = new Bounds(maxZ - rooftopHolder.transform.position, new Vector3(500, 200, 0.2f));

                    int maxIterations = 200;
                    for (int j = 0; j < surfacedensity && maxIterations > 0; j++)
                    {



                        GameObject newObject = Instantiate(rooftopElements[Random.Range(0, rooftopElements.Length)]) as GameObject;
                        newObject.transform.parent = rooftopHolder.transform;
                        /////
                        newObject.transform.position = new Vector3(Random.Range(minLineX.x, minLineZ.x), minLineX.y, Random.Range(minLineX.z, minLineX.z)) - rooftopHolder.transform.position;
                        newObject.transform.Rotate(0, 180, 0);
                        float rndScl = Random.Range(randomScaleMin, randomScaleMax);
                        newObject.transform.localScale = Vector3.Scale(newObject.transform.localScale, new Vector3 (rndScl, rndScl, 1));
                        //             newObject.transform.Rotate(new Vector3 (0, 0, 1), Random.Range(0, 4)*90);

                        Bounds newRendererBounds = newObject.GetComponent<Renderer>().bounds;

                        //Check Intersections

                        foreach (Transform go in rooftopHolder.transform.Cast<Transform>().Reverse())
                        {
                            if (newRendererBounds.Intersects(go.gameObject.GetComponent<Renderer>().bounds) && go != newObject.transform)
                            {

                                DestroyImmediate(newObject);
                                j = j - 1;
                                maxIterations = maxIterations - 1;
                                break;
                            }

                        }
                        if (newRendererBounds.Intersects(greebleAreaX) || newRendererBounds.Intersects(greebleAreaZ))
                        {
                            DestroyImmediate(newObject);
                        }
                    }
                    if (rooftopHolder.GetComponentsInChildren<Renderer>().Length > 1)

                        MergeMeshes();
                    else
                    {
                        DestroyImmediate(rooftopHolder);
                        return;
                    }

                   // MergeMeshes();

                    if (!noMesh)
                    {
                        if (roofTopsObject == null && bmComponent.isInRoot)
                        {
                            rooftopHolder.transform.parent = bmComponent.cityRandomizerParent.transform.Find("AdvPanels");
                        }
                        else rooftopHolder.transform.parent = bmComponent.gameObject.transform;
                    }
                    //   else DeleteSolution();

                }
            }
            else
            {
                DeleteSolution();
                
            }

        }
        public void MergeMeshes()
        {


            // else Debug.LogWarning("Please assign rooftop material in CS Rooftops Component");
         //   rooftopHolder.transform.localScale = new Vector3(1, 1, 1);
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
            rooftopHolder.transform.position = gameObject.transform.position;
            rooftopHolder.transform.rotation = gameObject.transform.rotation;
            rooftopHolder.transform.localScale = new Vector3 (1, bmComponent.scale, 1);

            //if (meshF.sharedMesh.vertices.Length == 0)
            //{
            //    DestroyImmediate(rooftopHolder);
            //    noMesh = true;
            //}
            //else noMesh = false;
        }

        

        public void DeleteSolution()
        {

            if (rooftopHolder != null) {
                foreach (Transform go in rooftopHolder.transform.Cast<Transform>().Reverse())
                {
                    DestroyImmediate(go.gameObject);

                }
                if (rooftopHolder.GetComponent<MeshFilter>() != null)
            DestroyImmediate(rooftopHolder.GetComponent<MeshFilter>().sharedMesh);
}
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