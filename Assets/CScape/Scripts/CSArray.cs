
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

    public class CSArray : MonoBehaviour
    {
        //#if UNITY_EDITOR

        public int instancesX;
        public int instancesZ;
        public Material mat;
        public Mesh mesh;
        public Mesh meshOriginal;
        //  public GameObject originalObject;
        //     public GameObject[] greebleElements;
        public int offsetX;
        public int offsetZ;
        public bool update;
        public int width;
        public int depth;
        public int maxMeshSize;
        public StreetModifier parentSection;
        public GameObject parentBuilding;
        public GameObject[] rooftopHolder = new GameObject[10];
        //     public GameObject rooftopHolderPrefab;
        public GameObject[] rooftopElements = new GameObject[10];
        public BuildingModifier bmComponent;
        public int randomSeed;
        //    private Bounds greebleArea;
        //    private Bounds greebleAreaX;
        //    private Bounds greebleAreaZ;
        bool isPrefabOriginal;
        public MeshRenderer rh;
        public Material[] greebleMat = new Material[10];
        public LODGroup lodComponent;
        public GameObject roofTopsObject;
        public float lodDistance = 0.7f;
        public bool useAdvertising = true;
        public bool animateLodFade = true;
        public int[] maxFloors = new int[10];
        public int[] leftSideStart = new int[10];
        public int[] rightSideStart = new int[10];
        public int[] downStart = new int[10];
        public int[] upStart = new int[10];
        public int[] skipX = new int[10];
        public int[] skipY = new int[10];
        public float[] placingDepth = new float[10];
        public bool[] projectDepth = new bool[10];
        // int i = 0;
        int iteration = 1;
        public int numberOfModifiers = 1;
        public enum ModifierType { FrontSide, BackSide, LeftSide, RightSide };
        public ModifierType[] modifierType = new ModifierType[10];
        public enum Alignement { BottomLeft, TopLeft, Not_Implemented };
        public Alignement[] alignTo = new Alignement[10];
        public bool useProjection = false;
        public bool[] sparseRemove = new bool[10];
        public int[] sparseRandom = new int[10];
        public bool strechable = false;





        public void AwakeMe()
        {
            if (gameObject.activeInHierarchy)
            {
                if (sparseRemove.Length < 10) System.Array.Resize(ref sparseRemove, 10);
                if (sparseRandom.Length < 10) System.Array.Resize(ref sparseRandom, 10);

                for (int i = 0; i < numberOfModifiers; i++)
                {
                    iteration = i;
                    if (rooftopElements[0] != null)
                    {
                        if (useAdvertising)
                        {


                            bmComponent = gameObject.GetComponent<BuildingModifier>();
                            roofTopsObject = null;
                            // rooftopHolder = null;
                            if (rooftopHolder[iteration] == null)
                            {
                                rooftopHolder[iteration] = new GameObject(gameObject.transform.name + "_Detail");
#if UNITY_EDITOR
                                if (rooftopHolder[iteration].GetComponent<LODGroup>() == null)
                                {
                                    lodComponent = rooftopHolder[iteration].AddComponent<LODGroup>();
                                    UnityEditor.SerializedObject obj = new UnityEditor.SerializedObject(lodComponent);

                                    UnityEditor.SerializedProperty valArrProp = obj.FindProperty("m_LODs.Array");
                                    for (int j = 0; valArrProp.arraySize > j; j++)
                                    {
                                        UnityEditor.SerializedProperty sHeight = obj.FindProperty("m_LODs.Array.data[" + i.ToString() + "].screenRelativeHeight");

                                        if (j == 0)
                                        {
                                            sHeight.doubleValue = 0.8;
                                        }
                                        if (j == 1)
                                        {
                                            sHeight.doubleValue = 0.5;
                                        }
                                        if (j == 2)
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
                }
            }

        }
        public void UpdateElements()
        {
            if (gameObject.activeInHierarchy)
            {
                for (int i = 0; i < numberOfModifiers; i++)
                {
                    iteration = i;
                    if (rooftopElements[i] != null)
                    {
                        if (bmComponent.useAdvertising)
                        {
                            if (rooftopHolder[iteration] == null)
                            {
                                rooftopHolder[iteration] = new GameObject(gameObject.transform.name + "_Detail");
#if UNITY_EDITOR
                                if (rooftopHolder[iteration].GetComponent<LODGroup>() == null)
                                {
                                    lodComponent = rooftopHolder[iteration].AddComponent<LODGroup>();
                                    rooftopHolder[iteration].AddComponent<MeshFilter>();
                                    rooftopHolder[iteration].AddComponent<MeshRenderer>();
                                    rooftopHolder[iteration].transform.parent = gameObject.transform;
                                    rooftopHolder[iteration].transform.localPosition = new Vector3(0, 0, 0);
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
                            int z = 0;

                            Vector3 minLineX = new Vector3(gameObject.transform.position.x + bmComponent.buildingWidth * 3 + bmComponent.advertOffsetX[z].x, gameObject.transform.position.y + bmComponent.advertOffsetY[z], gameObject.transform.position.z + placingDepth[i]);
                            Vector3 maxLineX = new Vector3(gameObject.transform.position.x + bmComponent.buildingWidth * 3 + bmComponent.advertOffsetX[z].x, gameObject.transform.position.y + bmComponent.floorNumber, gameObject.transform.position.z + placingDepth[i]);
                            Vector3 minX = new Vector3(gameObject.transform.position.x + bmComponent.buildingWidth * 3 + bmComponent.roofOffsetX[z].x, gameObject.transform.position.y + bmComponent.floorNumber * 3 + bmComponent.roofOffsetY[z], gameObject.transform.position.z + bmComponent.buildingDepth * 3 + bmComponent.roofOffsetX[z].y);
                            Vector3 maxX = new Vector3(gameObject.transform.position.x + bmComponent.buildingWidth * 3 + bmComponent.roofOffsetX[z].x, gameObject.transform.position.y + bmComponent.floorNumber * 3 + bmComponent.roofOffsetY[z], gameObject.transform.position.z + bmComponent.roofOffsetZ[z].y);
                            Vector3 minZ = new Vector3(gameObject.transform.position.x + bmComponent.roofOffsetZ[z].x, gameObject.transform.position.y + bmComponent.floorNumber * 3 + bmComponent.roofOffsetY[z], gameObject.transform.position.z + bmComponent.roofOffsetZ[z].y);
                            //   Vector3 maxZ = new Vector3(gameObject.transform.position.x + bmComponent.roofOffsetZ[z].x, gameObject.transform.position.y + bmComponent.floorNumber * 3 + bmComponent.roofOffsetY[z], gameObject.transform.position.z + bmComponent.buildingDepth * 3 + bmComponent.roofOffsetX[z].y);

                            float surfacedensity = (minX.z - maxX.z) * (minX.x - minZ.x) * instancesX / 10000;
                            if (surfacedensity > 50) surfacedensity = 50;


#if UNITY_EDITOR
                            isPrefabOriginal = UnityEditor.PrefabUtility.GetPrefabParent(gameObject) == null && UnityEditor.PrefabUtility.GetPrefabObject(gameObject.transform) != null;
#endif
                            if (!isPrefabOriginal)
                            {
                                if (rooftopHolder == null)
                                {
                                    rooftopHolder[iteration] = new GameObject();
                                    rooftopHolder[iteration].transform.position = gameObject.transform.position;
                                    rooftopHolder[iteration].transform.parent = gameObject.transform;
                                    rooftopHolder[iteration].AddComponent<MeshFilter>();
                                    rooftopHolder[iteration].transform.name = "Rooftop_" + gameObject.transform.name;
                                }
                                DeleteSolution();

                                //    greebleArea = new Bounds(Vector3.Lerp(minX, minZ, 0.5f) - rooftopHolder[i].transform.position, new Vector3(minX.x - minZ.x, 100, minX.z - maxX.z) * 2);
                                //    greebleAreaX = new Bounds(minLineZ - rooftopHolder[i].transform.position, new Vector3(0.1f, 200, 500));
                                //    greebleAreaZ = new Bounds(maxZ - rooftopHolder[i].transform.position, new Vector3(500, 200, 0.2f));


                                int buildingWidth = Mathf.Clamp(bmComponent.buildingWidth, 0, instancesX);
                                int floorNumber = Mathf.Clamp(bmComponent.floorNumber, 0, instancesX);
                                Random.InitState(sparseRandom[0]);

                                if (alignTo[i] == Alignement.BottomLeft)
                                {
                                    for (int j = 0; j < Mathf.FloorToInt((buildingWidth - rightSideStart[i]) / skipX[i]); j++)
                                    {
                                        for (int k = 0; k < Mathf.FloorToInt((floorNumber - upStart[i]) / skipY[i]); k++)
                                        {
                                            bool boolValue = (Random.Range(0, 2) == 0);
                                            if (!sparseRemove[i]) boolValue = true;
                                            if (boolValue)
                                            {
                                                GameObject newObject = Instantiate(rooftopElements[i]) as GameObject;
                                                newObject.transform.parent = rooftopHolder[iteration].transform;




                                                newObject.transform.localPosition = new Vector3(leftSideStart[i] * 3 + rooftopHolder[iteration].transform.position.x + j * skipX[i] * 3, downStart[i] * 3 + minLineX.y + k * skipY[i] * 3, minLineX.z) - rooftopHolder[iteration].transform.position;
                                                newObject.transform.rotation = rooftopHolder[iteration].transform.rotation;
                                                newObject.transform.Rotate(-90, 0, 180);
                                            }
                                        }
                                    }
                                }

                                if (alignTo[i] == Alignement.TopLeft)
                                {
                                    for (int j = 0; j < Mathf.FloorToInt((buildingWidth - rightSideStart[i]) / skipX[i]); j++)
                                    {
                                        for (int k = 0; k < Mathf.FloorToInt((floorNumber - upStart[i]) / skipY[i]); k++)
                                        {
                                            bool boolValue = (Random.Range(0, 2) == 0);
                                            if (!sparseRemove[i]) boolValue = true;
                                            if (boolValue)
                                            {
                                                GameObject newObject = Instantiate(rooftopElements[i]) as GameObject;
                                                newObject.transform.parent = rooftopHolder[iteration].transform;


                                                newObject.transform.localPosition = new Vector3(leftSideStart[i] * 3 + rooftopHolder[iteration].transform.position.x + j * skipX[i] * 3, k * skipY[i] * 3 + maxLineX.y - upStart[i] * 3, minLineX.z) - rooftopHolder[iteration].transform.position;
                                                newObject.transform.rotation = rooftopHolder[iteration].transform.rotation;
                                                newObject.transform.Rotate(-90, 0, 180);
                                            }
                                        }
                                    }
                                }

                                //     rooftopHolder[iteration].transform.position = gameObject.transform.position;
                                //     rooftopHolder[iteration].transform.rotation = gameObject.transform.rotation;
                                if (useProjection)
                                {
                                    foreach (Transform go in rooftopHolder[iteration].transform.Cast<Transform>().Reverse())
                                    {
                                        RaycastHit hit;
                                        Vector3 fwd = go.transform.TransformDirection(new Vector3(0, 1, 0));
                                        Vector3 pos = go.transform.TransformPoint(new Vector3(1.5f, 0, 0));

                                        if (Physics.Raycast(pos, fwd, out hit))
                                        {
                                            if (hit.distance < 10)
                                            {
                                                go.transform.Translate(new Vector3(0, hit.distance, 0));
                                            }
                                            else DestroyImmediate(go);

                                        }
                                        else DestroyImmediate(go.gameObject);
                                        // go.transform.localPosition = go.transform.position;
                                    }
                                }



                                //     if (modifierType[i] == ModifierType.BackSide)




                                if (roofTopsObject == null && bmComponent.isInRoot)
                                {
                                    //    roofTopsObject = gameObject.transform.Find("CSBuildingDetails");
                                    //     rooftopHolder.transform.parent = bmComponent.transform.Find("CSBuildingDetails");
                                    //    CityRandomizer cityRandomizerParent = bmComponent.cityRandomizerParent;
                                    rooftopHolder[iteration].transform.parent = bmComponent.cityRandomizerParent.transform.Find("CSBuildingDetails");
                                }
                                else rooftopHolder[iteration].transform.parent = bmComponent.gameObject.transform;


                                rooftopHolder[iteration].transform.position = new Vector3(0, 0, 0);
                                rooftopHolder[iteration].transform.rotation = Quaternion.identity;

                                if (rooftopHolder[iteration].GetComponentsInChildren<Renderer>().Length > 2)

                                    MergeMeshes();
                                else
                                {
                                    DestroyImmediate(rooftopHolder[iteration]);
                                    return;
                                }
                                //MergeMeshes();

                                #region setStrechable


                                #endregion
                                rooftopHolder[iteration].transform.position = gameObject.transform.position;
                                //   else DeleteSolution();

                            }
                            
                        }
                        

                        else
                        {
                            DeleteSolution();

                        }

                    }
                }
            }

        }

        public void MergeMeshes()
        {


            // else Debug.LogWarning("Please assign rooftop material in CS Rooftops Component");
            rooftopHolder[iteration].transform.localScale = new Vector3(1, 1, 1);
            MeshFilter[] meshFilters = rooftopHolder[iteration].GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];

            int index = 0;
            for (int x = 0; x < meshFilters.Length; x++)
            {
                if (meshFilters[x].sharedMesh != null)
                {
                    // if (meshFilters[i].sharedMesh == null) continue;
                    combine[index].mesh = meshFilters[x].sharedMesh;
                    combine[index++].transform = meshFilters[x].transform.localToWorldMatrix;
                }
            }
            MeshFilter meshF = rooftopHolder[iteration].transform.GetComponent<MeshFilter>();
            meshF.sharedMesh = new Mesh();
            meshF.sharedMesh.CombineMeshes(combine);
            meshF.sharedMesh.RecalculateBounds();

            if (lodComponent != null) lodComponent.RecalculateBounds();

            foreach (Transform go in rooftopHolder[iteration].transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }

            if (rooftopHolder[iteration].GetComponent<MeshRenderer>() == null)
            {
                rh = rooftopHolder[iteration].AddComponent<MeshRenderer>() as MeshRenderer;
                rh = rooftopHolder[iteration].GetComponent<MeshRenderer>();
                rh.sharedMaterial = greebleMat[iteration];

            }
            rh = rooftopHolder[iteration].GetComponent<MeshRenderer>();
            rh.sharedMaterial = greebleMat[iteration];
            Renderer[] renderers = new Renderer[1];
            LOD[] lod = new LOD[1];
            renderers[0] = rooftopHolder[iteration].GetComponent<Renderer>();
            lod[0] = new LOD(lodDistance, renderers);
            lodComponent.SetLODs(lod);
            if (animateLodFade)
                lodComponent.fadeMode = LODFadeMode.CrossFade;
            else lodComponent.fadeMode = LODFadeMode.None;
            lodComponent.animateCrossFading = animateLodFade;

            rooftopHolder[iteration].transform.position = gameObject.transform.position;
            rooftopHolder[iteration].transform.rotation = gameObject.transform.rotation;
            rooftopHolder[iteration].transform.localScale = new Vector3(1, bmComponent.scale, 1);
            
        }



        public void DeleteSolution()
        {
            for (int x = 0; x < numberOfModifiers; x++)
            {
                if (rooftopHolder[iteration] != null)
                {
                    foreach (Transform go in rooftopHolder[iteration].transform.Cast<Transform>().Reverse())
                    {
                        DestroyImmediate(go.gameObject);

                    }

                    if (rooftopHolder[iteration].GetComponent<MeshFilter>() != null)
                        DestroyImmediate(rooftopHolder[iteration].GetComponent<MeshFilter>().sharedMesh);

                }
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