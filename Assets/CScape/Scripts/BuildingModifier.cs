using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using CScape;
using System.Threading;
//using UnityEditor;

namespace CScape
{
    [RequireComponent(typeof(MeshCollider))]
    [ExecuteInEditMode]
    public class BuildingModifier : MonoBehaviour
    {


        public Vector3 lowFloorBound;
        public int prefabFloors;
        public int prefabDepth;
        public int prefabWidth;
        public Vector3 size;
        private Vector3[] originalVertices;
        private Vector2[] originalUVs;
        private Color[] originalColors;
        private Vector4[] vColors;
        //  private Vector2[] secondUVs;
        public float scale = 1;
        public Mesh meshOriginal;
        public Mesh mesh;
        public int floorNumber;
        public float floorHeight;
        public int uniqueMapping;
        public int buildingWidth;
        public int buildingDepth;
        public float normalThreshold = 0.1f;
        public Vector2 id1;
        public Vector2 id2;
        public Vector2 id3;
        public Vector2 id4;
        public int materialId1;
        public int materialId2;
        public int materialId3;
        public int materialId4;
        public int materialId5;
        public int divisionId4;
        public int lightnessFront;
        public int lightnessSide;
        public float windowOpen;
        public GameObject rooftopObject;
        public bool generateLightmappingUV = false;
        public float pattern;
        public Vector4 colorVariation;
        public Vector4 colorVariation2;
        public Vector4 colorVariation3;
        public Vector4 colorVariation4;
        public Vector4 colorVariation5;
        public float lightsOnOff;
        public float lightsOnOff2;

        public bool prefabHasVertexInfo = false;
        public Vector4 advertisingPanelCoord;
        public float lightVec;
        public float lightVec2;
        public float lightVec3;
        public float lightVec4;
        public float lightVec5;
        public float lightVec6;
        public float lightVec7;
        public float lightVec8;
        public CityRandomizer cityRandomizerParent;
        public StreetModifier[] parentStreets; //center, front, right, back, left
        public BuildingModifier[] connectedSections; //self, front, right, back, left
        public Vector2[] roofOffsetX;
        public Vector2[] roofOffsetZ;
        public Vector2[] advertOffsetX;
        public Vector2[] advertOffsetZ;
        public Vector3 advertSide; //advertising on face
        public float[] roofOffsetY;
        public float[] advertOffsetY;
        public bool hasRooftops = false;
        public bool hasAdvertising = false;
        public bool hasBalcony = false;
        public CSRooftops rooftops;
        public CSAdvertising advertising;
        public CSArray balcony;
        public CSFloorDetails floorDetails;
        public bool distort = false;
        public float distortXZ = 0;
        public bool useAdvertising = true;
        public int borderCol;
        public float thresholdResizeX = 0;
        public float thresholdResizeZ = 0;
        public float thresholdResizeY = 0;
        public bool supportSkewing = false;
        public float skew = 0;
        Color32[] vertexColor;
        public bool useGraffiti = false;
        public bool autoMap = false;
        public bool scaleFrom1to3 = false;
        public float extendFoundations = 1f;
        public float extFoundationsTreshhold = 0;
        public bool isInRoot = false;

        public bool hasFloorDetails = false;
        public int rooftopID = 0;
        public int customData1 = 0;
        public bool customBool2 = false;
        public int buildingPositionOnStreet = 0;
        public bool useFloorLimiting = false;
        public int minFloorNumber;
        public int maxFloorNumber;
        public bool dontDestroyChildren = false;
        public bool hasNoFloorLevel = false;
        public Vector4 slantedRoofsVal;
        public bool useSlantedRoofsResize = false;

        //  public MeshCollider collider;



        private void Awake()
        {
            if (!Application.isPlaying)
            {
                advertising = null;
                rooftops = null;
            }
        }

        public void AwakeCity()
        {
            if (useFloorLimiting) LimitFloors();

            if (gameObject.activeInHierarchy)
            {
                CheckParents();

                if (cityRandomizerParent == null && isInRoot) cityRandomizerParent = gameObject.transform.parent.transform.parent.GetComponent<CityRandomizer>() as CityRandomizer;

                rooftops = gameObject.GetComponent<CSRooftops>();
                if (gameObject.GetComponent<CSRooftops>() != null)
                {
                    hasRooftops = true;
                }
                else hasRooftops = false;

                advertising = gameObject.GetComponent<CSAdvertising>();
                if (advertising != null)
                {
                    hasAdvertising = true;
                    advertising.useAdvertising = useAdvertising;
                }
                else hasAdvertising = false;

                balcony = gameObject.GetComponent<CSArray>();
                if (balcony != null)
                {
                    hasBalcony = true;
                    balcony.useAdvertising = useAdvertising;
                }
                else hasFloorDetails = false;

                floorDetails = gameObject.GetComponent<CSFloorDetails>();
                if (floorDetails != null)
                {
                    hasFloorDetails = true;
                }
                else hasFloorDetails = false;




                parentStreets = new StreetModifier[5];
                connectedSections = new BuildingModifier[5];

                //meshOriginal = GetComponent<MeshFilter>().sharedMesh;
                originalVertices = meshOriginal.vertices;
                if (scaleFrom1to3)
                {
                    for (int i = 0; i < meshOriginal.vertices.Length; i++)
                    {
                        originalVertices[i] = meshOriginal.vertices[i] * 3f;

                    }

                }
                originalUVs = meshOriginal.uv;

                if (meshOriginal.colors.Length > 0)
                    originalColors = meshOriginal.colors;
                else originalColors = new Color[meshOriginal.vertices.Length];

                vColors = new Vector4[originalVertices.Length];
                mesh = Instantiate(meshOriginal) as Mesh;
                MeshFilter meshFilter = GetComponent<MeshFilter>();
                meshFilter.mesh = mesh;
                //		gameObject.transform.GetChild (0);
                //      lightVec = (Mathf.FloorToInt(colorVariation.x * 0.1f) * 10f) + (Mathf.FloorToInt(colorVariation.y) * 0.1f) + (Mathf.FloorToInt(colorVariation.z) * 0.01f) + (Mathf.FloorToInt(colorVariation.w) * 0.001f + 0.00001f);
                //   
                ModifyBuilding();
                //  Thread olix = new Thread (ModifyBuilding);





            }
        }

        private void LimitFloors()
        {
            
            if (floorNumber > maxFloorNumber) floorNumber = maxFloorNumber;
            if (floorNumber < minFloorNumber) floorNumber = minFloorNumber;
        }

        private void CheckParents()
        {
            Transform parent1 = gameObject.transform.parent;
            Transform parent2 = null;
            if (parent1 != null) { 
                parent2 = gameObject.transform.parent.transform.parent;
                if ((parent1.name == "Buildings")) isInRoot = true;
                else isInRoot = false;
            }


            if ((parent2 != null))
            {
                if ((gameObject.transform.parent.transform.parent.name == "Buildings")) isInRoot = true;
            }
            else isInRoot = false;

            // if (cityRandomizerParent == null) cityRandomizerParent = GameObject.Find("CScape City").GetComponent<CityRandomizer>() as CityRandomizer;

        }

        public void UpdateCity()
        {
            if (useFloorLimiting) LimitFloors();
            if (gameObject.activeInHierarchy)
            {
                CheckParents();

                // if (cityRandomizerParent == null) cityRandomizerParent = GameObject.Find("CScape City").GetComponent<CityRandomizer>() as CityRandomizer;
                if (cityRandomizerParent == null && isInRoot) cityRandomizerParent = gameObject.transform.parent.transform.parent.GetComponent<CityRandomizer>() as CityRandomizer;
                if (gameObject.GetComponent<CSRooftops>() != null)
                {
                    hasRooftops = true;
                }

                advertising = gameObject.GetComponent<CSAdvertising>();
                if (advertising != null)
                {
                    hasAdvertising = true;
                    advertising.useAdvertising = useAdvertising;
                }

                balcony = gameObject.GetComponent<CSArray>();
                if (balcony != null)
                {
                    hasBalcony = true;
                    balcony.useAdvertising = useAdvertising;
                }
                else hasBalcony = false;

                floorDetails = gameObject.GetComponent<CSFloorDetails>();
                if (floorDetails != null)
                {
                    hasFloorDetails = true;

                }
                else hasFloorDetails = false;

                ModifyBuilding();
                //  Thread olix = new Thread(ModifyBuilding);




            }
        }




        public void ModifyBuilding()
        {
            
            if (gameObject.activeInHierarchy)
            {


                if (gameObject.GetComponent<CSRooftops>() != null)
                {
                    hasRooftops = true;
                }
                else hasRooftops = false;

                if (gameObject.GetComponent<CSAdvertising>() != null)
                {
                    hasAdvertising = true;
                }
                else hasAdvertising = false;

                balcony = gameObject.GetComponent<CSArray>();
                if (balcony != null)
                {
                    hasBalcony = true;
                    balcony.useAdvertising = useAdvertising;
                }
                else hasBalcony = false;

                floorDetails = gameObject.GetComponent<CSFloorDetails>();
                if (floorDetails != null)
                {
                    hasFloorDetails = true;
                }
                else hasFloorDetails = false;

                id1.x = CompressIDs(materialId1);
                id2.x = CompressIDs(materialId2);
                id3.x = CompressIDs(materialId3);
                id4.x = CompressIDs(materialId4);


                //originalVertices = meshOriginal.vertices;
                //originalColors = meshOriginal.colors;
                //originalUVs = meshOriginal.uv;
                transform.localScale = new Vector3(1, 1, 1);
                //mesh = GetComponent<MeshFilter>().mesh;
                Vector4[] vColorsFloat = new Vector4[mesh.uv.Length];
                Vector3[] vertices = mesh.vertices;
                Vector2[] uV = mesh.uv;
                vertexColor = mesh.colors32;

                Vector3[] transformVertices = mesh.vertices;
                Vector2[] transformUV = mesh.uv;

                Vector3[] normals = mesh.normals;

                lightVec = Mathf.FloorToInt(colorVariation.x) + Mathf.FloorToInt(colorVariation.y) * 0.1f + Mathf.FloorToInt(colorVariation.z) * 0.01f + Mathf.FloorToInt(colorVariation.w) * 0.001f + Mathf.FloorToInt(lightsOnOff) * 0.0001f + 0.00002f;
                lightVec2 = Mathf.FloorToInt(colorVariation2.x) + Mathf.FloorToInt(colorVariation2.y) * 0.1f + Mathf.FloorToInt(colorVariation2.z) * 0.01f + Mathf.FloorToInt(colorVariation2.w) * 0.001f + Mathf.FloorToInt(lightsOnOff2) * 0.0001f + 0.00002f;
                lightVec3 = Mathf.FloorToInt(colorVariation3.x) * 0.01f + Mathf.FloorToInt(colorVariation3.y) * 0.001f + lightnessFront * 0.0001f + 0.00002f;
                lightVec6 = Mathf.FloorToInt(colorVariation3.x) * 0.01f + Mathf.FloorToInt(colorVariation3.y) * 0.001f + lightnessFront * 0.0001f + 0.00002f;

                lightVec4 = Mathf.FloorToInt(windowOpen) + Mathf.FloorToInt(colorVariation4.x) * 0.1f + (materialId5) * 0.001f + borderCol * 0.0001f + 0.00002f;
                lightVec5 = Mathf.FloorToInt(windowOpen) + Mathf.FloorToInt(colorVariation4.x) * 0.1f + (0) * 0.001f + borderCol * 0.0001f + 0.00002f;

                lightVec7 = Mathf.FloorToInt(colorVariation5.x) * 0.01f + Mathf.FloorToInt(colorVariation5.y) * 0.001f + lightnessSide * 0.0001f + 0.00002f;
                lightVec8 = Mathf.FloorToInt(colorVariation5.x) * 0.01f + Mathf.FloorToInt(colorVariation5.y) * 0.001f + lightnessSide * 0.0001f + 0.00002f;


                //   Debug.Log(lightVec);
                int i = 0;
                while (i < vertices.Length)
                {
                    transformVertices[i] = new Vector3(0, 0, 0);
                    transformUV[i] = new Vector2(0, 0);
                    vColors[i] = new Vector4(id1.x + lightVec3, id1.y, lightVec2, lightVec) * 0.1f;
                    Vector3 invNormal = normals[i] * -1;

                    if (originalVertices[i].y > lowFloorBound.y)
                    {
                        if (floorNumber < prefabFloors) floorNumber = prefabFloors;
                        transformVertices[i].y = (floorNumber - prefabFloors) * floorHeight;
                        transformUV[i].y = floorNumber - prefabFloors;

                    }
                    ////
                    if (thresholdResizeY != 0 && (lowFloorBound.y - (thresholdResizeY / 2f)) < originalVertices[i].y && (lowFloorBound.y + (thresholdResizeY / 2f)) > originalVertices[i].y)
                    {
                        if (floorNumber < prefabFloors) floorNumber = prefabFloors;
                        transformVertices[i].y = (floorNumber / 2 - prefabFloors / 2) * floorHeight;
                        transformUV[i].y = floorNumber / 2 - prefabFloors / 2;

                    }

                    if (originalVertices[i].x > lowFloorBound.x)
                    {
                        if (buildingWidth < prefabWidth) buildingWidth = prefabWidth;

                        transformVertices[i].x = (buildingWidth - prefabWidth) * floorHeight;


                        if (invNormal.z < 1 + normalThreshold && invNormal.z > 1 - normalThreshold)
                        {
                            transformUV[i].x = buildingWidth - prefabWidth;
                        }
                        if (normals[i].z < 1 + normalThreshold && normals[i].z > 1 - normalThreshold)
                        {
                            transformUV[i].x = -(buildingWidth - prefabWidth);
                        }


                    }
                    //New resize function
                    if (thresholdResizeX != 0 && (lowFloorBound.x + (thresholdResizeX / 2f)) > originalVertices[i].x && originalVertices[i].x > (lowFloorBound.x - (thresholdResizeX / 2f)))
                    {
                        if (buildingWidth < prefabWidth) buildingWidth = prefabWidth;

                        transformVertices[i].x = (buildingWidth / 2 - prefabWidth / 2) * floorHeight;


                        if (invNormal.z < 1 + normalThreshold && invNormal.z > 1 - normalThreshold)
                        {
                            transformUV[i].x = buildingWidth / 2 - prefabWidth / 2;
                        }
                        if (normals[i].z < 1 + normalThreshold && normals[i].z > 1 - normalThreshold)
                        {
                            transformUV[i].x = -(buildingWidth / 2 - prefabWidth / 2);
                        }


                    }


                    if (originalVertices[i].z > lowFloorBound.z)
                    {
                        if (buildingDepth < prefabDepth) buildingDepth = prefabDepth;
                        transformVertices[i].z = (buildingDepth - prefabDepth) * floorHeight;

                        if (normals[i].x < 1 + normalThreshold && normals[i].x > 1 - normalThreshold)
                        {
                            transformUV[i].x = (buildingDepth - prefabDepth);
                        }
                        if (invNormal.x < 1 + normalThreshold && invNormal.x > 1 - normalThreshold)
                        {
                            transformUV[i].x = -(buildingDepth - prefabDepth);
                        }



                    }

                    if (thresholdResizeZ != 0 && (lowFloorBound.z + (thresholdResizeZ / 2f)) > originalVertices[i].z && originalVertices[i].z > (lowFloorBound.z - (thresholdResizeZ / 2f)))
                    {
                        if (buildingDepth < prefabDepth) buildingDepth = prefabDepth;
                        transformVertices[i].z = (buildingDepth / 2 - prefabDepth / 2) * floorHeight;

                        if (normals[i].x < 1 + normalThreshold && normals[i].x > 1 - normalThreshold)
                        {
                            transformUV[i].x = (buildingDepth / 2 - prefabDepth / 2);
                        }
                        if (invNormal.x < 1 + normalThreshold && invNormal.x > 1 - normalThreshold)
                        {
                            transformUV[i].x = -(buildingDepth / 2 - prefabDepth / 2);
                        }
                    }
                    ////Extend foundations for supporting tererain

                    if (extendFoundations != 0f)
                    {
                        if (originalVertices[i].y <= extFoundationsTreshhold)
                        {
                            transformVertices[i].y = transformVertices[i].y - extendFoundations;
                            transformUV[i].y = transformUV[i].y - extendFoundations / 3;
                        }

                    }


                    if (useSlantedRoofsResize)
                    {
                        if (originalVertices[i].y >= slantedRoofsVal.y)
                        {
                            if (slantedRoofsVal.w != 0)
                                transformVertices[i].y = transformVertices[i].y + (buildingDepth - 1) * slantedRoofsVal.w;
                            if (slantedRoofsVal.z != 0)
                                transformVertices[i].y = transformVertices[i].y + (buildingWidth - 1) * slantedRoofsVal.z;
                            //                          transformUV[i].y = transformUV[i].y - extendFoundations / 3;
                        }

                    }

                    ///////// manipulate Vertex colors




                    if (prefabHasVertexInfo)
                    {
                        if (originalColors[i].r < 0.2f)
                        {

                            if (invNormal.z < 1 + normalThreshold && invNormal.z > 1 - normalThreshold)
                            {
                                vColors[i] = new Vector4(id2.x + lightVec3, lightVec4, lightVec2, lightVec) * 0.1f;
                            }
                            if (normals[i].z < 1 + normalThreshold && normals[i].z > 1 - normalThreshold)
                            {
                                vColors[i] = new Vector4(id2.x + lightVec6, lightVec4, lightVec2, lightVec) * 0.1f;
                            }
                            if (normals[i].x < 1 + normalThreshold && normals[i].x > 1 - normalThreshold)
                            {
                                vColors[i] = new Vector4(id3.x + lightVec7, lightVec4, lightVec2, lightVec) * 0.1f;
                            }
                            if (invNormal.x < 1 + normalThreshold && invNormal.x > 1 - normalThreshold)
                            {
                                vColors[i] = new Vector4(id3.x + lightVec8, lightVec4, lightVec2, lightVec) * 0.1f;
                            }


                        }
                        else
                        {
                            vColors[i] = new Vector4(id1.x + lightVec3, lightVec5, lightVec2, lightVec) * 0.1f;


                        }


                    }
                    else
                    {
                        if (invNormal.z < 1 + normalThreshold && invNormal.z > 1 - normalThreshold)
                        {
                            vColors[i] = new Vector4(id2.x + lightVec3, lightVec4, lightVec2, lightVec) * 0.1f;
                        }
                        if (normals[i].z < 1 + normalThreshold && normals[i].z > 1 - normalThreshold)
                        {
                            vColors[i] = new Vector4(id2.x + lightVec6, lightVec4, lightVec2, lightVec) * 0.1f;
                        }
                        if (normals[i].x < 1 + normalThreshold && normals[i].x > 1 - normalThreshold)
                        {
                            vColors[i] = new Vector4(id3.x + lightVec7, lightVec4, lightVec2, lightVec) * 0.1f;
                        }

                        if (invNormal.x < 1 + normalThreshold && invNormal.x > 1 - normalThreshold)
                        {
                            vColors[i] = new Vector4(id3.x + lightVec8, lightVec4, lightVec2, lightVec) * 0.1f;
                        }

                    }
                    //     Debug.Log((id2.x + lightVec3) * 0.1f);

                    vertices[i] = Vector3.Scale(originalVertices[i] + transformVertices[i],  new Vector3(1, scale, 1));



                    uV[i] = new Vector2(originalUVs[i].x, originalUVs[i].y) + transformUV[i]   + new Vector2(uniqueMapping, 0);



                    if (normals[i].y > 0.9 || normals[i].y < -0.9)
                    {
                        uV[i].x = vertices[i].x * 0.5f;
                        uV[i].y = vertices[i].z * 0.5f + 10;
                        vColors[i] = new Vector4(id4.x + lightVec3, lightVec4, lightVec2, lightVec) * 0.1f;
                    }


                    if (autoMap)
                    {
                        if (invNormal.z < 1 + normalThreshold && invNormal.z > 1 - normalThreshold)
                        {
                            uV[i] = new Vector2(vertices[i].x / floorHeight + uniqueMapping, (vertices[i].y / (floorHeight * scale * Mathf.Abs(1 - Mathf.Sin (normals[i].y)))));
                        }

                        if (normals[i].z < 1 + normalThreshold && normals[i].z > 1 - normalThreshold)
                        {
                            uV[i] = new Vector2(vertices[i].x / -floorHeight + uniqueMapping, (vertices[i].y / (floorHeight * scale * Mathf.Abs(1 - Mathf.Sin(normals[i].y)))));
                        }

                        if (normals[i].x < 1 + normalThreshold && normals[i].x > 1 - normalThreshold)
                        {
                            uV[i] = new Vector2(vertices[i].z / floorHeight + uniqueMapping, (vertices[i].y / (floorHeight * scale * Mathf.Abs(1 - Mathf.Sin(normals[i].y)))));
                        }

                        if (invNormal.x < 1 + normalThreshold && invNormal.x > 1 - normalThreshold)
                        {
                            uV[i] = new Vector2(vertices[i].z / -floorHeight + uniqueMapping, (vertices[i].y / (floorHeight * scale * Mathf.Abs(1 - Mathf.Sin(normals[i].y)))));
                        }
                        
                        

                    }

                    if (supportSkewing)
                        vertices[i] = new Vector3(vertices[i].x + (1 / Mathf.Tan(Mathf.Deg2Rad * ((skew - 90))) * vertices[i].z), vertices[i].y, vertices[i].z);
                    vColorsFloat[i] = vColors[i];



                    if (prefabHasVertexInfo)
                    {
                        float boolValue = 0;
                        if (customBool2) boolValue = 0.1f;

                        if (useGraffiti && vertices[i].y < floorNumber * 3 - 1)
                            //graffiti written into channel A
                            //Rooftop Value into channel B 
                            vertexColor[i] = new Color(vertexColor[i].r, vertexColor[i].g, rooftopID * 0.01f + boolValue, 255);
                        else vertexColor[i] = new Color(vertexColor[i].r, 255, rooftopID * 0.01f + boolValue, 0);
                    }
                    if (hasNoFloorLevel)
                        uV[i] = new Vector2(uV[i].x, uV[i].y + 2);

                    i++;
                }




                var list = new List<Vector4>(vColors);

                //if (supportSkewing)
                //{
                //    i = 0;
                //    while (i < vertices.Length)
                //    {
                //        vertices[i] = new Vector3(vertices[i].x + (skew * vertices[i].z), vertices[i].y, vertices[i].z);
                //        i++;
                //    }
                //}

                mesh.vertices = vertices;
                mesh.uv = uV;
                mesh.SetUVs(3, list);
                mesh.colors32 = vertexColor;
                vertices = null;
                uV = null;
                list = null;
                vertexColor = null;
                transformVertices = null;
                transformUV = null;
                //originalVertices = null;
                //originalColors = null;
                //originalUVs = null;


        mesh.RecalculateNormals();
                mesh.RecalculateTangents();

                mesh.RecalculateBounds();
                MeshCollider mColl = gameObject.GetComponent<MeshCollider>();
                if (!gameObject.GetComponent<MeshCollider>())
                {
                    mColl = gameObject.AddComponent<MeshCollider>();


                }
                mColl.convex = false;
                mColl.sharedMesh = mesh;






                if (hasRooftops) rooftops.UpdateElements();
                if (hasAdvertising)
                {
                    //advertising.Awake();
                    advertising.UpdateElements();
                }

                if (hasBalcony)
                {

                    balcony.UpdateElements();
                }
                if (hasFloorDetails) floorDetails.UpdateElements();
            }
        }

        float CompressIDs(int iDInput)
        {

            float idOut = ((iDInput) * 0.1f);
            //float idOut = 

            return idOut;


        }


        public void OnDestroy()
        {
            if (!dontDestroyChildren) {

                if (Application.isEditor && !Application.isPlaying)
                {

                    if (useAdvertising)
                    {
                        if (advertising != null)
                            DestroyImmediate(advertising.rooftopHolder);
                    }

                    if (hasBalcony)
                    {
                        CSArray CSArray = balcony;
                        for (int x = 0; x < CSArray.numberOfModifiers; x++)
                        {

                            DestroyImmediate(CSArray.rooftopHolder[x]);
                        }

                    }
                    if (hasRooftops)
                    {

                        CSRooftops csRooftops = rooftops;
                        if (csRooftops && csRooftops.rooftopHolder)
                            DestroyImmediate(csRooftops.rooftopHolder);

                    }

                    if (hasFloorDetails)
                    {

                        CSFloorDetails csRooftops = floorDetails;
                        if (floorDetails && floorDetails.rooftopHolder)
                            DestroyImmediate(floorDetails.rooftopHolder);

                    }


                }
            }
        }


        public void GuessPrefabSize()
        {
            Bounds bounds = meshOriginal.bounds;
            if (scaleFrom1to3)
            {
                prefabFloors = Mathf.FloorToInt((bounds.size.y));
                prefabDepth = Mathf.FloorToInt((bounds.size.z));
                prefabWidth = Mathf.FloorToInt((bounds.size.x));
                lowFloorBound = bounds.size / 1f;

            }
            else
            {
                prefabFloors = Mathf.FloorToInt((bounds.size.y) / 3f);
                prefabDepth = Mathf.FloorToInt((bounds.size.z) / 3f);
                prefabWidth = Mathf.FloorToInt((bounds.size.x) / 3f);
                lowFloorBound = bounds.size / 2f;
            }
        }


        float CastAO(Vector3 vPosition, Vector3 vNormal)
        {
            float vOut = 0;
            float vOutTemp = 0;
            RaycastHit hit;
            Debug.Log(vNormal + " " + vPosition);
            for (int i = 0; i < CityRandomizer.aoSteps; i++)
            {
                if (Physics.Raycast(transform.TransformPoint(vPosition), transform.TransformPoint(vNormal + vPosition) + new Vector3(UnityEngine.Random.Range(vNormal.x - CityRandomizer.aoAngle, vNormal.x + CityRandomizer.aoAngle), UnityEngine.Random.Range(vNormal.y - CityRandomizer.aoAngle, vNormal.y + CityRandomizer.aoAngle), UnityEngine.Random.Range(vNormal.z - CityRandomizer.aoAngle, vNormal.z + CityRandomizer.aoAngle)), out hit, 100f))
                {
                    vOutTemp = hit.distance / 50000;
                }

                else vOutTemp = 1;
                vOut = vOut + vOutTemp;

            }
            vOut = vOut / CityRandomizer.aoSteps;
            return vOut;

        }


    }
}
