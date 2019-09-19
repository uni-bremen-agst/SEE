using UnityEngine;
using System.Collections;
using CScape;
//using UnityEditor;


namespace CScape
{

    //[ExecuteInEditMode]
    public class StreetModifier : MonoBehaviour
    {


        public Vector3 lowFloorBound;
        public Vector3 lowFloorBoundSecond;
        public int prefabFloors;
        public int prefabDepth;
        public int prefabWidth;
        public int prefabCenterSectionDepth;
        public int prefabCenterSectionWidth;
        public Vector3 size;
        private Vector3[] originalVertices;
        //      private Vector2[] originalUVs;
        private Color[] originalColors;
        private Color[] vColors;
        public float scale = 1;
        public Mesh meshOriginal;
        public Mesh mesh;
        //  public int floorNumber;
        public float floorHeight = 3f;
        // public int uniqueMapping;
        public int blockWidth;
        public int sectionWidth;
        public int blockDepth;
        public int sectionDepth;
        public float normalThreshold = 0.1f;
        public Vector2 id1;
        public Vector2 id2;
        public Vector2 id3;
        public int materialId1;
        public int materialId2;
        public int materialId3;
        public int sidewalkID;
        public int streetID;
        public float windowOpen;
        public GameObject rooftopObject;
        public GameObject streetDetailObj;
        public float pattern;
        public Vector2 colorVariation;
        // public float idimidodjimi;
        public bool prefabHasVertexInfo = true;
        public StreetModifier[] parentStreets; //self, front, right, back, left
        public BuildingModifier[] connectedSections; //childBuilding, front, right, back, left
        public enum CScapeStreetType { Street, River, Park };
        public enum CScapeStreetStyle { Business, OldTown, Residential, Industrial };
        public CScapeStreetType streetType;
        public bool markToDelete = false;
        public bool useSkewLR = false;
        public bool useSkewFB = false;
        public int slitRB = 0;
        public int slitLF = 0;
        public float skewRotationFront;
        public float skewRotationLeft;
        public float skewRotationBack;
        public float skewRotationRight;
        public float lenghtFront;
        public float lenghtLeft;
        public float lenghtBack;
        public float lenghtRight;
        public CityRandomizer cityRandomizerParent;
        public GameObject[] childrenBuildings;
        public int sidewalkSize = 2;
        public int averageBuildingSizeMin;
        public int averageBuildingSizeMax;
        public int averageBuildingDepthMax;
        public int randomSeed;
        public GameObject[] frontBuildings;
        public GameObject[] backBuildings;
        public GameObject[] leftBuildings;
        public GameObject[] rightBuildings;
        public int[] frontBuildingPositions;
        public int[] backBuildingPositions;
        public int[] leftBuildingPositions;
        public int[] rightBuildingPositions;

        public bool protect = false;
        public bool protectL = false;
        public bool protectR = false;
        public int distantiateBuildings = 0;
        public bool useGraffiti = false;
        public Vector3 frontStartPoint;
        public Vector3 rightStartPoint;
        public Vector3 backStartPoint;
        public Vector3 leftStartPoint;
        public DistrictStyle districtStyle;






        public void AwakeCity()
        {

            //meshOriginal = GetComponent<MeshFilter>().sharedMesh;
            if (cityRandomizerParent == null)
                cityRandomizerParent = GameObject.Find("CScape City").GetComponent<CityRandomizer>();
            originalVertices = meshOriginal.vertices;
            //   originalUVs = meshOriginal.uv;
            originalColors = meshOriginal.colors;
            vColors = new Color[originalVertices.Length];
            mesh = Instantiate(meshOriginal) as Mesh;
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            //		gameObject.transform.GetChild (0);
            ModifyStreet();

        }


        public void UpdateCity()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                this.enabled = false;
            }
            else ModifyStreet();
#else
        this.enabled = false;
#endif

            //		ModifyNormals();

        }




        public void ModifyStreet()
        {

            id1.x = CompressIDs(materialId1);
            id2.x = CompressIDs(materialId2);
            id3.x = CompressIDs(materialId3);



            transform.localScale = new Vector3(scale, 1, 1);

            Vector3[] vertices = mesh.vertices;
            Vector2[] uV = mesh.uv;

            Vector3[] transformVertices = mesh.vertices;
            Vector2[] transformUV = mesh.uv;

            Vector3[] normals = mesh.normals;
            Vector2[] uV3 = mesh.uv;

            int i = 0;
            while (i < vertices.Length)
            {
                transformVertices[i] = new Vector3(0, 0, 0);
                transformUV[i] = new Vector2(0, 0);

                //  vColors[i] = new Color(id1.x, id1.y, pattern, CompressVector2(colorVariation)) / 10;
                //      Vector3 invNormal = normals[i] * -1;





                if (originalVertices[i].x > lowFloorBound.x && originalVertices[i].x < lowFloorBoundSecond.x)
                {
                    if (blockWidth < prefabWidth) blockWidth = prefabWidth;
                    transformVertices[i].x = (sectionWidth - prefabWidth + 1) * floorHeight;
                }


                if (originalVertices[i].z > lowFloorBound.z && originalVertices[i].z < lowFloorBoundSecond.z)
                {
                    if (blockDepth < prefabDepth) blockDepth = prefabDepth;
                    transformVertices[i].z = (sectionDepth - prefabDepth + 1) * floorHeight;
                }

                if (originalVertices[i].x > lowFloorBoundSecond.x)
                {
                    if (blockWidth < prefabWidth) blockWidth = prefabWidth;
                    transformVertices[i].x = (blockWidth - prefabWidth) * floorHeight;
                }


                if (originalVertices[i].z > lowFloorBoundSecond.z)
                {
                    if (blockDepth < prefabDepth) blockDepth = prefabDepth;
                    transformVertices[i].z = (blockDepth - prefabDepth) * floorHeight;

                    transformUV[i].x = transformVertices[i].x / 3f;
                    transformUV[i].y = transformVertices[i].z / 3f;
                }

                transformUV[i].x = originalVertices[i].x + transformVertices[i].x / 3f;
                transformUV[i].y = originalVertices[i].z + transformVertices[i].z / 3f;
                Vector3 invNormal = normals[i] * -1;

                ///////// manipulate Vertex colors
                if (prefabHasVertexInfo)
                {
                    if (originalColors[i].r == 1f && originalColors[i].g == 0f && originalColors[i].b == 0f)
                    {

                        vColors[i] = new Color((materialId1 * 0.1f) + 0.00001f, 0, 0, 0);

                    }

                    else if (originalColors[i].r == 1f && originalColors[i].g == 1f && originalColors[i].b == 0f)
                    {

                        vColors[i] = new Color((materialId2 * 0.1f) + 0.00001f, 0, 0, 0);

                    }

                    else if (originalColors[i].r == 0f && originalColors[i].g == 1f && originalColors[i].b == 0f)
                    {

                        vColors[i] = new Color(0, (materialId3 * 0.1f) + 0.00001f, 0, 0);

                    }

                    else if (originalColors[i].r == 1f && originalColors[i].g == 0f && originalColors[i].b == 1f)
                    {

                        vColors[i] = new Color((materialId3 * 0.1f) + 0.00001f, 0, 0, 0);

                    }

                    else if (originalColors[i].r == 1f && originalColors[i].g == 1f && originalColors[i].b == 1f)
                    {

                        vColors[i] = new Color(((materialId3 + 1) * 0.1f) + 0.00001f, 0, 0, 0);

                    }
                    else vColors[i] = new Color(0, 0, 0, 0);

                    //Set IDS

                    if (originalColors[i].r == 0f && originalColors[i].g == 0f && originalColors[i].b == 0f)
                    {
                        uV3[i] = new Vector2(sidewalkID, streetID);
                    }
                    else uV3[i] = new Vector2(streetID, 0f);

                }


                ////////

                vertices[i] = originalVertices[i] + transformVertices[i];
                if (invNormal.z < 1 + normalThreshold && invNormal.z > 1 - normalThreshold)
                {
                    uV[i] = new Vector2(transformVertices[i].y, transformVertices[i].z);
                }

                else if (normals[i].z < 1 + normalThreshold && normals[i].z > 1 - normalThreshold)
                {
                    uV[i] = new Vector2(originalVertices[i].x + transformVertices[i].x, originalVertices[i].z + transformVertices[i].z) / 3f;
                }
                else if (normals[i].x < 1 + normalThreshold && normals[i].x > 1 - normalThreshold)
                {
                    uV[i] = new Vector2(originalVertices[i].x + transformVertices[i].x, originalVertices[i].z + transformVertices[i].z) / 3f;
                }
                else if (invNormal.x < 1 + normalThreshold && invNormal.x > 1 - normalThreshold)
                {
                    uV[i] = new Vector2(originalVertices[i].x + transformVertices[i].x, originalVertices[i].z + transformVertices[i].z) / 3f;
                }

                else uV[i] = new Vector2(originalVertices[i].x + transformVertices[i].x, originalVertices[i].z + transformVertices[i].z) / 3f;

                //////////USe Skewing
                if (useSkewLR)
                {
                    if (vertices[i].z > lowFloorBound.z && vertices[i].x > lowFloorBound.x)
                    {
                        vertices[i].x = vertices[i].x - slitRB * 3;
                    }
                    if (vertices[i].z > lowFloorBound.z && vertices[i].x < lowFloorBound.x)
                    {
                        vertices[i].x = vertices[i].x - slitLF * 3;
                    }
                }

                if (useSkewFB)
                {
                    if (vertices[i].x > lowFloorBound.x && vertices[i].z > lowFloorBound.z)
                    {
                        vertices[i].z = vertices[i].z - slitRB * 3;
                    }
                    if (vertices[i].x > lowFloorBound.x && vertices[i].z < lowFloorBound.z)
                    {
                        vertices[i].z = vertices[i].z - slitLF * 3;
                    }
                }


                i++;
            }

            ////Calculate Skew angle
            int sign;
            if (slitRB <= 0) sign = 1;
            else sign = -1;
            Vector2 vec1 = new Vector2(0, sectionDepth - 2);
            Vector2 vec2 = new Vector2(slitRB, sectionDepth - 2);
            skewRotationRight = Vector2.Angle(vec2, vec1) * sign - 90;


            if (useSkewFB)
            {
                vec1 = new Vector2(0 + sidewalkSize, 0 + sidewalkSize);
                vec2 = new Vector2(slitRB + sidewalkSize, sectionDepth - sidewalkSize + slitLF - slitRB);
            }
            else
            {
                vec1 = new Vector2(sectionWidth - sidewalkSize, sidewalkSize);
                vec2 = new Vector2(sectionWidth - sidewalkSize + slitRB, sectionDepth - sidewalkSize);
            }
            lenghtRight = Vector2.Distance(vec2, vec1);

            if (slitLF <= 0) sign = 1;
            else sign = -1;
            vec1 = new Vector2(0, sectionDepth - 2);
            vec2 = new Vector2(slitLF, sectionDepth - 2);
            skewRotationLeft = Vector2.Angle(vec2, vec1) * sign + 90;




            if (useSkewLR)
            {
                vec1 = new Vector2(0 + sidewalkSize, 0 + sidewalkSize);
                vec2 = new Vector2(slitLF + sidewalkSize, sectionDepth - sidewalkSize);
            }
            else
            {
                vec1 = new Vector2(0 + sidewalkSize, 0 + sidewalkSize);
                vec2 = new Vector2(sidewalkSize, sectionDepth - sidewalkSize);
            }

            lenghtLeft = Vector2.Distance(vec2, vec1);

            if (slitLF >= 0) sign = 1;
            else sign = -1;
            vec1 = new Vector2(0, sectionWidth - 2);
            vec2 = new Vector2(slitLF, sectionWidth - 2);
            skewRotationFront = Vector2.Angle(vec2, vec1) * sign;



            if (useSkewLR)
            {
                vec1 = new Vector2(sidewalkSize, 0 + sidewalkSize);
                vec2 = new Vector2(sidewalkSize, sectionWidth - sidewalkSize);
            }
            else
            {
                vec1 = new Vector2(sidewalkSize, 0 + sidewalkSize);
                vec2 = new Vector2(sectionWidth - sidewalkSize, sidewalkSize - slitLF);

            }
            lenghtFront = Vector2.Distance(vec2, vec1);

            if (slitRB >= 0) sign = 1;
            else sign = -1;
            vec1 = new Vector2(0, sectionWidth - 2);
            vec2 = new Vector2(slitRB, sectionWidth - 2);
            skewRotationBack = (Vector2.Angle(vec2, vec1) * sign) + 180;
            if (useSkewLR)
            {
                vec1 = new Vector2(0 + sidewalkSize + slitRB, sectionDepth - sidewalkSize);
                vec2 = new Vector2(sectionWidth - sidewalkSize + slitLF, sectionDepth - sidewalkSize);
            }
            else
            {
                vec1 = new Vector2(sidewalkSize, sectionDepth - sidewalkSize);
                vec2 = new Vector2(sectionWidth - sidewalkSize, sectionDepth - sidewalkSize - slitRB);
            }
            lenghtBack = Vector2.Distance(vec2, vec1);
            if (useSkewFB)
            {
                skewRotationLeft = 90;
                skewRotationRight = -90;
            }
            if (useSkewLR)
            {
                skewRotationFront = 0;
                skewRotationBack = 180;
            }

            //vec1 = new Vector2(blockWidth * 3f, blockDepth * 3f);
            //vec2 = new Vector2(slitXL * 3f, blockDepth * 3f);
            //skewRotationFront = Vector2.Angle(vec2, vec1);
            // Debug.Log(skewRotation);
            //////

            mesh.vertices = vertices;
            mesh.colors = vColors;
            // mesh.colors = originalColors;
            mesh.uv = uV;
            mesh.uv4 = uV3;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            MeshCollider mColl = gameObject.GetComponent<MeshCollider>();
            if (!gameObject.GetComponent<MeshCollider>())
            {
                mColl = gameObject.AddComponent<MeshCollider>();


            }
            mColl.convex = false;
            mColl.sharedMesh = mesh;



        }

        public void CalculateReferencePoints()
        {
            frontStartPoint = new Vector3(0, 0, 0);
            if (useSkewFB) rightStartPoint = new Vector3(sectionWidth * 3, 0, -slitLF * 3);
            else rightStartPoint = new Vector3(sectionWidth * 3, 0, 0);

            if (useSkewLR) leftStartPoint = new Vector3(-slitLF * 3, 0, sectionDepth * 3);
            else leftStartPoint = new Vector3(0, 0, sectionDepth * 3);

            if (useSkewLR) backStartPoint = new Vector3((sectionWidth - slitRB) * 3, 0, sectionDepth * 3);
            else if (useSkewFB) backStartPoint = new Vector3(sectionWidth * 3, 0, (sectionDepth - slitRB) * 3);
            else backStartPoint = new Vector3(sectionWidth * 3, 0, sectionDepth * 3);

        }

        public void GenerateBuildings()
        {
            if (cityRandomizerParent == null)
                cityRandomizerParent = GameObject.Find("CScape City").GetComponent<CityRandomizer>();

            if (streetType == CScapeStreetType.Street)
            {
                System.Array.Resize(ref frontBuildingPositions, 0);
                System.Array.Resize(ref backBuildingPositions, 0);
                System.Array.Resize(ref leftBuildingPositions, 0);
                System.Array.Resize(ref rightBuildingPositions, 0);

                Random.InitState(randomSeed);
                int rndIteration = randomSeed;
                int accumulateDistanceFront = Mathf.FloorToInt(lenghtFront);
                int accumulateDistanceBack = Mathf.FloorToInt(lenghtBack);
                int accumulateDistanceRight = Mathf.FloorToInt(lenghtRight);
                int accumulateDistanceLeft = Mathf.FloorToInt(lenghtLeft);

                ///Calculate average sizes




                int averageBSizeFront = Mathf.FloorToInt(accumulateDistanceFront / ((averageBuildingSizeMin + averageBuildingSizeMax) / 2));
                System.Array.Resize(ref frontBuildingPositions, averageBSizeFront);





                if (useSkewFB) accumulateDistanceLeft = Mathf.FloorToInt(lenghtLeft);
                for (int b = 0; b < frontBuildings.Length; b++)
                {
                    if (frontBuildings[b] != null)
                        DestroyImmediate(frontBuildings[b]);
                }
                for (int b = 0; b < backBuildings.Length; b++)
                {
                    if (backBuildings[b] != null)
                        DestroyImmediate(backBuildings[b]);
                }
                for (int b = 0; b < leftBuildings.Length; b++)
                {
                    if (leftBuildings[b] != null)
                        DestroyImmediate(leftBuildings[b]);
                }
                for (int b = 0; b < rightBuildings.Length; b++)
                {
                    if (rightBuildings[b] != null)
                        DestroyImmediate(rightBuildings[b]);
                }


                System.Array.Resize(ref frontBuildings, 0);
                System.Array.Resize(ref backBuildings, 0);
                System.Array.Resize(ref leftBuildings, 0);
                System.Array.Resize(ref rightBuildings, 0);
                //// calculate max extrusion
                int maxBuildingDepth = Mathf.Min(sectionDepth, sectionWidth);
                bool maxSideIsFront = false;
                if (sectionDepth < sectionWidth) maxSideIsFront = true;

                ///////////// Calculate Front Buildings
                #region calculate front side
                int i = 0;
                while (i < accumulateDistanceFront)
                {
                    rndIteration = rndIteration + i;
                    Random.InitState(rndIteration);
                    bool validate = false;
                    GameObject prefabToInstantiate = null;
                    int iterations = 0;
                    bool start = true;
                    if (districtStyle == null) districtStyle = cityRandomizerParent.buildingStyles[0];
                    int prefabChoice = Random.Range(0, districtStyle.prefabs.Length - 1);


                    while (!validate && iterations < districtStyle.prefabs.Length)
                    {
                        rndIteration = rndIteration + i;
                        Random.InitState(rndIteration);
                        if (start == true)
                        {
                            prefabToInstantiate = districtStyle.prefabs[prefabChoice];
                            start = false;
                        }
                        else
                        {
                            if (prefabChoice < districtStyle.prefabs.Length)
                                prefabChoice++;
                            else prefabChoice = 0;
                            prefabToInstantiate = districtStyle.prefabs[prefabChoice];
                        }
                        validate = true;
                    }
                    if (iterations == districtStyle.prefabs.Length) prefabToInstantiate = districtStyle.prefabs[0];

                    GameObject cloneH = Instantiate(prefabToInstantiate, new Vector3(gameObject.transform.position.x + sidewalkSize * 3, gameObject.transform.position.y, gameObject.transform.position.z + sidewalkSize * 3), transform.rotation) as GameObject;

                    cloneH.transform.parent = cityRandomizerParent.buildings.transform;
                    cloneH.layer = 19;
                    BuildingModifier bm = cloneH.GetComponent(typeof(BuildingModifier)) as BuildingModifier;

                    if (i == 0)
                    {
                        bm.supportSkewing = true;
                        if (useSkewFB)
                            bm.skew = skewRotationFront;
                        else bm.skew = 90 - skewRotationLeft;
                    }
                    bm.buildingWidth = Random.Range(averageBuildingSizeMin, averageBuildingSizeMax);
                    //   if (bm.gameObject != )
                    if (maxSideIsFront)
                        bm.buildingDepth = Random.Range(bm.prefabDepth, sectionDepth / 2);
                    else bm.buildingDepth = Random.Range(bm.prefabDepth, maxBuildingDepth / 2 - sidewalkSize * 2);


                    System.Array.Resize(ref frontBuildings, frontBuildings.Length + 1);
                    frontBuildings[frontBuildings.Length - 1] = cloneH;

                    if (useSkewFB)
                    {
                        cloneH.transform.Rotate(new Vector3(0, skewRotationFront, 0));
                    }
                    cloneH.transform.Translate(new Vector3(i * 3, 0, Random.Range(-0.03f, 0.03f)));
                    if (bm.buildingWidth > bm.prefabWidth)
                    {

                        i = i + bm.buildingWidth;
                    }

                    else
                    {
                        bm.buildingWidth = bm.prefabWidth;
                        i = i + bm.buildingWidth;
                    }
                    // bm.buildingDepth = Random.Range(averageBuildingSizeMin, averageBuildingSizeMax);
                    //      bm.buildingDepth = Random.Range(0, averageBuildingDepthMax);

                    ////randomize  only surfaces           
                    int rnd = Random.Range(0, cityRandomizerParent.nightColors.Length - 1);
                    bm.colorVariation.x = Random.Range(0, 9);
                    bm.colorVariation.y = Mathf.Floor(Random.Range(0, 9));
                    bm.colorVariation.z = Mathf.Floor(Random.Range(0, 9));
                    bm.colorVariation.w = Mathf.Floor(Random.Range(0, 9));

                    bm.colorVariation2.x = Random.Range(0, 9);
                    bm.colorVariation2.y = Random.Range(0, 9);
                    bm.colorVariation2.z = Random.Range(0, 9);
                    bm.colorVariation2.w = Random.Range(2, 9);

                    bm.colorVariation3.x = Random.Range(0, 10);
                    bm.colorVariation3.y = Random.Range(0, 10);
                    bm.colorVariation3.z = Random.Range(0, 10);
                    bm.colorVariation3.w = Random.Range(0, 10);

                    bm.colorVariation4.x = Random.Range(0, 10);
                    bm.colorVariation4.y = Random.Range(0, 10);
                    bm.colorVariation4.z = Random.Range(0, 10);
                    bm.colorVariation4.w = Random.Range(0, 10);
                    bm.lightnessFront = Random.Range(0, 10);
                    bm.lightnessSide = Random.Range(0, 10);
                    bm.colorVariation5.x = Random.Range(0, 10);
                    bm.colorVariation5.y = Random.Range(0, 10);
                    bm.borderCol = Random.Range(0, 10);
                    bm.lightsOnOff = Random.Range(0, 10);
                    bm.floorNumber = Random.Range(cityRandomizerParent.minFloors, cityRandomizerParent.maxFloors);
                    bm.rooftopID = Random.Range(0, 10);
                    bm.lightsOnOff2 = Random.Range(0, 10);
                    bm.scale = Random.Range(0.9F, 1.25F);
                    int customBool = Random.Range(0, 3);
                    if (customBool > 0) bm.customBool2 = false;
                    else bm.customBool2 = true;
                    if (useGraffiti)
                        bm.useGraffiti = true;



                    //          if (height)
                    //             bm.floorNumber = Random.Range(cityRandomizerParent.minFloors, cityRandomizerParent.maxFloors + Mathf.CeilToInt(cityRandomizerParent.cityCurve.Evaluate(dist / (blockDistances * 3f * numberOfBuildingsX / 2)) * 200f));
                    bm.uniqueMapping = Random.Range(-160, 160);
                    if (cityRandomizerParent.faccadeStyles)
                    {
                        bm.colorVariation2.y = Random.Range(0, 9);
                        bm.colorVariation2.z = Random.Range(0, 9);
                        bm.materialId1 = 21;
                        bm.materialId2 = Random.Range(cityRandomizerParent.minMatIndex1, cityRandomizerParent.maxMatIndex1);
                        bm.materialId3 = Random.Range(cityRandomizerParent.minMatIndex2, cityRandomizerParent.maxMatIndex2);
                        bm.materialId4 = Random.Range(cityRandomizerParent.minMatIndex4, cityRandomizerParent.maxMatIndex4);
                        bm.materialId5 = Random.Range(0, 22);
                    }
                    if (cityRandomizerParent.openWindow)
                        bm.windowOpen = Random.Range(cityRandomizerParent.minWindowOpen, cityRandomizerParent.maxWindowOpen);
                    if (cityRandomizerParent.rndPatternHorizontal)
                        bm.pattern = Random.Range(0f, 1f);
                    if (distantiateBuildings > 0)
                        i = i + Mathf.RoundToInt(Random.Range(0, distantiateBuildings));
                    bm.AwakeCity();
                    bm.UpdateCity();





                    if (i > sectionWidth - (2 * sidewalkSize))
                    {
                        DestroyImmediate(cloneH);
                    }

                }
                #endregion
                ///////// Calculate Back Builldings
                #region calculate back side
                i = 0;
                while (i + sidewalkSize * 2 < accumulateDistanceBack)
                {
                    rndIteration = rndIteration + i;
                    Random.InitState(rndIteration);
                    bool validate = false;
                    GameObject prefabToInstantiate = null;
                    int iterations = 0;
                    bool start = true;
                    int prefabChoice = Random.Range(0, districtStyle.prefabs.Length - 1);


                    while (!validate && iterations < districtStyle.prefabs.Length)
                    {
                        rndIteration = rndIteration + i;
                        Random.InitState(rndIteration);
                        if (start == true)
                        {
                            prefabToInstantiate = districtStyle.prefabs[prefabChoice];
                            start = false;
                        }
                        else
                        {
                            if (prefabChoice < districtStyle.prefabs.Length)
                                prefabChoice++;
                            else prefabChoice = 0;
                            prefabToInstantiate = districtStyle.prefabs[prefabChoice];
                        }
                        validate = true;
                    }
                    if (iterations == districtStyle.prefabs.Length) prefabToInstantiate = districtStyle.prefabs[0];

                    int slit = 0;
                    if (useSkewLR) slit = slitRB;
                    GameObject cloneH;

                    if (useSkewLR)
                    {
                        cloneH = Instantiate(prefabToInstantiate, new Vector3(gameObject.transform.position.x + (sectionWidth * 3) - sidewalkSize * 3 - slit * 3, gameObject.transform.position.y, gameObject.transform.position.z + (sectionDepth * 3) - sidewalkSize * 3), transform.rotation) as GameObject;
                        cloneH.layer = 19;
                    }

                    else cloneH = Instantiate(prefabToInstantiate, new Vector3(gameObject.transform.position.x + (sectionWidth * 3) - sidewalkSize * 3 - slit * 3, gameObject.transform.position.y, gameObject.transform.position.z + (sectionDepth * 3 - slitRB * 3) - sidewalkSize * 3), transform.rotation) as GameObject;
                    cloneH.transform.parent = cityRandomizerParent.buildings.transform;
                    cloneH.layer = 19;

                    BuildingModifier bm = cloneH.GetComponent(typeof(BuildingModifier)) as BuildingModifier;
                    if (i == 0)
                    {
                        bm.supportSkewing = true;

                        bm.skew = skewRotationBack;
                        if (useSkewLR) bm.skew = 90 - skewRotationRight;

                    }
                    bm.buildingWidth = Random.Range(averageBuildingSizeMin, averageBuildingSizeMax);
                    System.Array.Resize(ref backBuildings, backBuildings.Length + 1);
                    backBuildings[backBuildings.Length - 1] = cloneH;


                    if (useSkewFB) cloneH.transform.Rotate(new Vector3(0, skewRotationBack, 0));
                    else cloneH.transform.Rotate(new Vector3(0, 180, 0));
                    cloneH.transform.Translate(new Vector3(i * 3, 0, Random.Range(-0.03f, 0.03f)));
                    if (bm.buildingWidth > bm.prefabWidth)
                    {

                        i = i + bm.buildingWidth;
                    }

                    else
                    {
                        bm.buildingWidth = bm.prefabWidth;
                        i = i + bm.buildingWidth;
                    }

                    if (maxSideIsFront)
                        bm.buildingDepth = Random.Range(bm.prefabDepth, sectionDepth / 2);
                    else bm.buildingDepth = Random.Range(bm.prefabDepth, maxBuildingDepth / 2 - sidewalkSize * 2);


                    ////randomize  only surfaces           
                    int rnd = Random.Range(0, cityRandomizerParent.nightColors.Length - 1);
                    bm.colorVariation.x = Random.Range(0, 9);
                    bm.colorVariation.y = Mathf.Floor(Random.Range(0, 9));
                    bm.colorVariation.z = Mathf.Floor(Random.Range(0, 9));
                    bm.colorVariation.w = Mathf.Floor(Random.Range(0, 9));

                    bm.colorVariation2.x = Random.Range(0, 9);
                    bm.colorVariation2.y = Random.Range(0, 9);
                    bm.colorVariation2.z = Random.Range(0, 9);
                    bm.colorVariation2.w = Random.Range(2, 9);

                    bm.colorVariation3.x = Random.Range(0, 10);
                    bm.colorVariation3.y = Random.Range(0, 10);
                    bm.colorVariation3.z = Random.Range(0, 10);
                    bm.colorVariation3.w = Random.Range(0, 10);

                    bm.colorVariation4.x = Random.Range(0, 10);
                    bm.colorVariation4.y = Random.Range(0, 10);
                    bm.colorVariation4.z = Random.Range(0, 10);
                    bm.colorVariation4.w = Random.Range(0, 10);
                    bm.lightnessFront = Random.Range(0, 10);
                    bm.lightnessSide = Random.Range(0, 10);
                    bm.colorVariation5.x = Random.Range(0, 10);
                    bm.colorVariation5.y = Random.Range(0, 10);
                    bm.borderCol = Random.Range(0, 10);
                    bm.lightsOnOff = Random.Range(0, 10);
                    bm.floorNumber = Random.Range(cityRandomizerParent.minFloors, cityRandomizerParent.maxFloors);
                    bm.rooftopID = Random.Range(0, 10);
                    bm.lightsOnOff2 = Random.Range(0, 10);
                    bm.scale = Random.Range(0.9F, 1.25F);
                    int customBool = Random.Range(0, 2);
                    if (customBool > 0) bm.customBool2 = false;
                    else bm.customBool2 = true;
                    if (useGraffiti)
                        bm.useGraffiti = true;



                    //          if (height)
                    //             bm.floorNumber = Random.Range(cityRandomizerParent.minFloors, cityRandomizerParent.maxFloors + Mathf.CeilToInt(cityRandomizerParent.cityCurve.Evaluate(dist / (blockDistances * 3f * numberOfBuildingsX / 2)) * 200f));
                    bm.uniqueMapping = Random.Range(-160, 160);
                    if (cityRandomizerParent.faccadeStyles)
                    {
                        bm.colorVariation2.y = Random.Range(0, 9);
                        bm.colorVariation2.z = Random.Range(0, 9);
                        bm.materialId1 = 21;
                        bm.materialId2 = Random.Range(cityRandomizerParent.minMatIndex1, cityRandomizerParent.maxMatIndex1);
                        bm.materialId3 = Random.Range(cityRandomizerParent.minMatIndex2, cityRandomizerParent.maxMatIndex2);
                        bm.materialId4 = Random.Range(cityRandomizerParent.minMatIndex4, cityRandomizerParent.maxMatIndex4);
                        bm.materialId5 = Random.Range(0, 22);
                    }
                    if (cityRandomizerParent.openWindow)
                        bm.windowOpen = Random.Range(cityRandomizerParent.minWindowOpen, cityRandomizerParent.maxWindowOpen);
                    if (cityRandomizerParent.rndPatternHorizontal)
                        bm.pattern = Random.Range(0f, 1f);

                    if (distantiateBuildings > 0)
                        i = i + Mathf.RoundToInt(Random.Range(0, distantiateBuildings));

                    bm.AwakeCity();
                    bm.UpdateCity();
                    if (i > accumulateDistanceBack - 6) DestroyImmediate(cloneH);

                }
                #endregion
                ///////// Calculate Back Side
                #region calculate right side

                i = 0;
                while (i + sidewalkSize < accumulateDistanceRight)
                {
                    rndIteration = rndIteration + i;
                    Random.InitState(rndIteration);
                    bool validate = false;
                    GameObject prefabToInstantiate = null;
                    int iterations = 0;
                    bool start = true;
                    int prefabChoice = Random.Range(0, districtStyle.prefabs.Length - 1);


                    while (!validate && iterations < districtStyle.prefabs.Length)
                    {
                        rndIteration = rndIteration + i;
                        Random.InitState(rndIteration);
                        if (start == true)
                        {
                            prefabToInstantiate = districtStyle.prefabs[prefabChoice];
                            start = false;
                        }
                        else
                        {
                            if (prefabChoice < districtStyle.prefabs.Length)
                                prefabChoice++;
                            else prefabChoice = 0;
                            prefabToInstantiate = districtStyle.prefabs[prefabChoice];
                        }
                        validate = true;
                    }
                    if (iterations == districtStyle.prefabs.Length) prefabToInstantiate = districtStyle.prefabs[0];

                    int slit = 0;
                    if (useSkewFB) slit = slitLF;
                    GameObject cloneH;


                    if (useSkewFB) cloneH = Instantiate(prefabToInstantiate, new Vector3(gameObject.transform.position.x + (sectionWidth * 3) - sidewalkSize * 3, gameObject.transform.position.y, gameObject.transform.position.z + sidewalkSize * 3 - slit * 3), transform.rotation) as GameObject;
                    else cloneH = Instantiate(prefabToInstantiate, new Vector3(gameObject.transform.position.x + (sectionWidth * 3) - sidewalkSize * 3, gameObject.transform.position.y, gameObject.transform.position.z + sidewalkSize * 3), transform.rotation) as GameObject;
                    cloneH.transform.parent = cityRandomizerParent.buildings.transform;
                    cloneH.layer = 19;

                    BuildingModifier bm = cloneH.GetComponent(typeof(BuildingModifier)) as BuildingModifier;
                    if (i == 0)
                    {
                        bm.supportSkewing = true;
                        if (useSkewFB)
                            bm.skew = (skewRotationFront) * -1;
                        else bm.skew = (skewRotationRight) - 90;
                    }
                    bm.buildingWidth = Random.Range(averageBuildingSizeMin, averageBuildingSizeMax);
                    System.Array.Resize(ref rightBuildings, rightBuildings.Length + 1);
                    rightBuildings[rightBuildings.Length - 1] = cloneH;


                    if (useSkewLR) cloneH.transform.Rotate(new Vector3(0, skewRotationRight, 0));
                    else cloneH.transform.Rotate(new Vector3(0, -90, 0));
                    cloneH.transform.Translate(new Vector3(i * 3, 0, Random.Range(-0.03f, 0.03f)));
                    if (bm.buildingWidth > bm.prefabWidth)
                    {

                        i = i + bm.buildingWidth;
                    }

                    else
                    {
                        bm.buildingWidth = bm.prefabWidth;
                        i = i + bm.buildingWidth;
                    }
                    //   bm.buildingDepth = Random.Range(averageBuildingSizeMin, averageBuildingSizeMax);

                    if (maxSideIsFront)
                        bm.buildingDepth = Random.Range(bm.prefabDepth, maxBuildingDepth / 3);
                    else bm.buildingDepth = Random.Range(bm.prefabDepth, sectionWidth / 2 - sidewalkSize * 2);

                    ////randomize  only surfaces           
                    int rnd = Random.Range(0, cityRandomizerParent.nightColors.Length - 1);
                    bm.colorVariation.x = Random.Range(0, 10);
                    bm.colorVariation.y = Random.Range(0, 10);
                    bm.colorVariation.z = Random.Range(0, 10);
                    bm.colorVariation.w = Random.Range(0, 10);

                    bm.colorVariation2.x = Random.Range(0, 10);
                    bm.colorVariation2.y = Random.Range(0, 10);
                    bm.colorVariation2.z = Random.Range(0, 10);
                    bm.colorVariation2.w = Random.Range(2, 10);

                    bm.colorVariation3.x = Random.Range(0, 10);
                    bm.colorVariation3.y = Random.Range(0, 10);
                    bm.colorVariation3.z = Random.Range(0, 10);
                    bm.colorVariation3.w = Random.Range(0, 10);

                    bm.colorVariation4.x = Random.Range(0, 10);
                    bm.colorVariation4.y = Random.Range(0, 10);
                    bm.colorVariation4.z = Random.Range(0, 10);
                    bm.colorVariation4.w = Random.Range(0, 10);
                    bm.lightnessFront = Random.Range(0, 10);
                    bm.lightnessSide = Random.Range(0, 10);
                    bm.colorVariation5.x = Random.Range(0, 10);
                    bm.colorVariation5.y = Random.Range(0, 10);
                    bm.borderCol = Random.Range(0, 10);
                    bm.lightsOnOff = Random.Range(0, 10);
                    bm.floorNumber = Random.Range(cityRandomizerParent.minFloors, cityRandomizerParent.maxFloors);
                    bm.rooftopID = Random.Range(0, 10);
                    bm.lightsOnOff2 = Random.Range(0, 10);
                    bm.scale = Random.Range(0.9F, 1.25F);
                    int customBool = Random.Range(0, 3);
                    if (customBool > 0) bm.customBool2 = false;
                    else bm.customBool2 = true;
                    if (useGraffiti)
                        bm.useGraffiti = true;


                    //          if (height)
                    //             bm.floorNumber = Random.Range(cityRandomizerParent.minFloors, cityRandomizerParent.maxFloors + Mathf.CeilToInt(cityRandomizerParent.cityCurve.Evaluate(dist / (blockDistances * 3f * numberOfBuildingsX / 2)) * 200f));
                    bm.uniqueMapping = Random.Range(-160, 160);
                    if (cityRandomizerParent.faccadeStyles)
                    {
                        bm.colorVariation2.y = Random.Range(0, 9);
                        bm.colorVariation2.z = Random.Range(0, 9);
                        bm.materialId1 = 21;
                        bm.materialId2 = Random.Range(cityRandomizerParent.minMatIndex1, cityRandomizerParent.maxMatIndex1);
                        bm.materialId3 = Random.Range(cityRandomizerParent.minMatIndex2, cityRandomizerParent.maxMatIndex2);
                        bm.materialId4 = Random.Range(cityRandomizerParent.minMatIndex4, cityRandomizerParent.maxMatIndex4);
                        bm.materialId5 = Random.Range(0, 22);
                    }
                    if (cityRandomizerParent.openWindow)
                        bm.windowOpen = Random.Range(cityRandomizerParent.minWindowOpen, cityRandomizerParent.maxWindowOpen);
                    if (cityRandomizerParent.rndPatternHorizontal)
                        bm.pattern = Random.Range(0f, 1f);

                    if (distantiateBuildings > 0)
                        i = i + Mathf.RoundToInt(Random.Range(0, distantiateBuildings));

                    bm.AwakeCity();
                    bm.UpdateCity();
                    if (i > accumulateDistanceRight - 6) DestroyImmediate(cloneH);

                }
                #endregion

                #region calculate left side

                i = 0;
                while (i + sidewalkSize < accumulateDistanceLeft)
                {

                    rndIteration = rndIteration + i;
                    Random.InitState(rndIteration);

                    bool validate = false;
                    GameObject prefabToInstantiate = null;
                    int iterations = 0;
                    bool start = true;
                    int prefabChoice = Random.Range(0, districtStyle.prefabs.Length - 1);


                    while (!validate && iterations < districtStyle.prefabs.Length)
                    {
                        rndIteration = rndIteration + i;
                        Random.InitState(rndIteration);

                        if (start == true)
                        {
                            prefabToInstantiate = districtStyle.prefabs[prefabChoice];
                            start = false;
                        }
                        else
                        {
                            if (prefabChoice < districtStyle.prefabs.Length)
                                prefabChoice++;
                            else prefabChoice = 0;
                            prefabToInstantiate = districtStyle.prefabs[prefabChoice];
                        }
                        validate = true;
                    }
                    if (iterations == districtStyle.prefabs.Length) prefabToInstantiate = districtStyle.prefabs[0];

                    int slit = 0;
                    if (useSkewFB) slit = slitRB;
                    GameObject cloneH;
                    if (useSkewFB) cloneH = Instantiate(prefabToInstantiate, new Vector3(gameObject.transform.position.x + sidewalkSize * 3, gameObject.transform.position.y, gameObject.transform.position.z + sectionDepth * 3 - sidewalkSize * 3), transform.rotation) as GameObject;
                    else cloneH = Instantiate(prefabToInstantiate, new Vector3(gameObject.transform.position.x + sidewalkSize * 3 - slitLF * 3, gameObject.transform.position.y, gameObject.transform.position.z + sectionDepth * 3 - sidewalkSize * 3), transform.rotation) as GameObject;
                    cloneH.transform.parent = cityRandomizerParent.buildings.transform;
                    cloneH.layer = 19;

                    BuildingModifier bm = cloneH.GetComponent(typeof(BuildingModifier)) as BuildingModifier;
                    if (i == 0)
                    {
                        bm.supportSkewing = true;
                        if (useSkewFB)
                            bm.skew = (skewRotationBack) * -1;
                        else bm.skew = (90 + skewRotationLeft);
                    }

                    bm.buildingWidth = Random.Range(averageBuildingSizeMin, averageBuildingSizeMax);
                    System.Array.Resize(ref leftBuildings, leftBuildings.Length + 1);
                    leftBuildings[leftBuildings.Length - 1] = cloneH;


                    if (useSkewLR) cloneH.transform.Rotate(new Vector3(0, skewRotationLeft, 0));
                    else cloneH.transform.Rotate(new Vector3(0, 90, 0));
                    cloneH.transform.Translate(new Vector3(i * 3, 0, Random.Range(-0.03f, 0.03f)));
                    if (bm.buildingWidth > bm.prefabWidth)
                    {

                        i = i + bm.buildingWidth;
                    }

                    else
                    {
                        bm.buildingWidth = bm.prefabWidth;
                        i = i + bm.buildingWidth;
                    }
                    //    bm.buildingDepth = Random.Range(averageBuildingSizeMin, averageBuildingSizeMax);
                    if (maxSideIsFront)
                        bm.buildingDepth = Random.Range(bm.prefabDepth, maxBuildingDepth / 3);
                    else bm.buildingDepth = Random.Range(bm.prefabDepth, sectionWidth / 2 - sidewalkSize * 2);
                    ////randomize  only surfaces           
                    int rnd = Random.Range(0, cityRandomizerParent.nightColors.Length - 1);
                    bm.colorVariation.x = Random.Range(0, 9);
                    bm.colorVariation.y = Random.Range(0, 10);
                    bm.colorVariation.z = Random.Range(0, 10);
                    bm.colorVariation.w = Random.Range(0, 10);

                    bm.colorVariation2.x = Random.Range(0, 9);
                    bm.colorVariation2.y = Random.Range(0, 9);
                    bm.colorVariation2.z = Random.Range(0, 9);
                    bm.colorVariation2.w = Random.Range(2, 9);

                    bm.colorVariation3.x = Random.Range(0, 10);
                    bm.colorVariation3.y = Random.Range(0, 10);
                    bm.colorVariation3.z = Random.Range(0, 10);
                    bm.colorVariation3.w = Random.Range(0, 10);

                    bm.colorVariation4.x = Random.Range(0, 10);
                    bm.colorVariation4.y = Random.Range(0, 10);
                    bm.colorVariation4.z = Random.Range(0, 10);
                    bm.colorVariation4.w = Random.Range(0, 10);
                    bm.lightnessFront = Random.Range(0, 10);
                    bm.lightnessSide = Random.Range(0, 10);
                    bm.colorVariation5.x = Random.Range(0, 10);
                    bm.colorVariation5.y = Random.Range(0, 10);
                    bm.borderCol = Random.Range(0, 10);
                    bm.lightsOnOff = Random.Range(0, 10);
                    bm.rooftopID = Random.Range(0, 10);
                    bm.lightsOnOff2 = Random.Range(0, 10);
                    bm.scale = Random.Range(0.9F, 1.25F);
                    int customBool = Random.Range(0, 3);
                    if (customBool > 0) bm.customBool2 = false;
                    else bm.customBool2 = true;
                    if (useGraffiti)
                        bm.useGraffiti = true;



                    //          if (height)
                    //             bm.floorNumber = Random.Range(cityRandomizerParent.minFloors, cityRandomizerParent.maxFloors + Mathf.CeilToInt(cityRandomizerParent.cityCurve.Evaluate(dist / (blockDistances * 3f * numberOfBuildingsX / 2)) * 200f));
                    bm.uniqueMapping = Random.Range(-160, 160);
                    if (cityRandomizerParent.faccadeStyles)
                    {
                        bm.colorVariation2.y = Random.Range(0, 9);
                        bm.colorVariation2.z = Random.Range(0, 9);
                        bm.materialId1 = 21;
                        bm.materialId2 = Random.Range(cityRandomizerParent.minMatIndex1, cityRandomizerParent.maxMatIndex1);
                        bm.materialId3 = Random.Range(cityRandomizerParent.minMatIndex2, cityRandomizerParent.maxMatIndex2);
                        bm.materialId4 = Random.Range(cityRandomizerParent.minMatIndex4, cityRandomizerParent.maxMatIndex4);
                        bm.materialId5 = Random.Range(0, 22);
                    }
                    if (cityRandomizerParent.openWindow)
                        bm.windowOpen = Random.Range(cityRandomizerParent.minWindowOpen, cityRandomizerParent.maxWindowOpen);
                    if (cityRandomizerParent.rndPatternHorizontal)
                        bm.pattern = Random.Range(0f, 1f);

                    if (distantiateBuildings > 0)
                        i = i + Mathf.RoundToInt(Random.Range(0, distantiateBuildings));

                    bm.AwakeCity();
                    bm.UpdateCity();
                    if (i > accumulateDistanceLeft) DestroyImmediate(cloneH);

                }
                #endregion
                bool deb = true;
                /////check array consistency
                int count = 0;
                foreach (GameObject arGo in frontBuildings)
                {
                    if (arGo != null) count++;
                }
                System.Array.Resize(ref frontBuildings, count);
                count = 0;
                foreach (GameObject arGo in rightBuildings)
                {
                    if (arGo != null) count++;
                }
                System.Array.Resize(ref rightBuildings, count);
                count = 0;
                foreach (GameObject arGo in backBuildings)
                {
                    if (arGo != null) count++;
                }
                System.Array.Resize(ref backBuildings, count);
                count = 0;
                foreach (GameObject arGo in leftBuildings)
                {
                    if (arGo != null) count++;
                }
                System.Array.Resize(ref leftBuildings, count);
                /////

                if (distantiateBuildings == 0 && deb)
                {
                    int fill = 0;
                    foreach (GameObject go in leftBuildings)
                    {
                        if (go != null)
                            fill = fill + go.GetComponent<BuildingModifier>().buildingWidth;
                    }
                    BuildingModifier fb = frontBuildings[0].GetComponent<BuildingModifier>();
                    BuildingModifier lb;
                    if (leftBuildings[leftBuildings.Length - 1] != null)
                        lb = leftBuildings[leftBuildings.Length - 1].GetComponent<BuildingModifier>();
                    else lb = leftBuildings[leftBuildings.Length - 2].GetComponent<BuildingModifier>();

                    if (fb.prefabDepth > ((sectionDepth - (2 * sidewalkSize)) - fill))
                    {
                        fb.buildingDepth = fb.prefabDepth;
                        lb.buildingWidth = lb.buildingWidth - (fb.prefabDepth - ((Mathf.FloorToInt(lenghtLeft) - (2 * sidewalkSize)) - fill));
                    }
                    else fb.buildingDepth = (sectionDepth - (2 * sidewalkSize)) - fill;

                    fb.AwakeCity();
                    fb.UpdateCity();
                    lb.AwakeCity();
                    lb.UpdateCity();

                    fill = 0;
                    foreach (GameObject go in frontBuildings)
                    {
                        if (go != null)
                            fill = fill + go.GetComponent<BuildingModifier>().buildingWidth;
                    }
                    if (rightBuildings.Length < 0)
                    {
                        fb = rightBuildings[0].GetComponent<BuildingModifier>();

                        if (frontBuildings[frontBuildings.Length - 1] != null)
                            lb = frontBuildings[frontBuildings.Length - 1].GetComponent<BuildingModifier>();
                        else lb = frontBuildings[frontBuildings.Length - 2].GetComponent<BuildingModifier>();
                    }
                    if (fb.prefabDepth > ((sectionWidth - (2 * sidewalkSize)) - fill))
                    {
                        fb.buildingDepth = fb.prefabDepth;
                        lb.buildingWidth = lb.buildingWidth - (fb.prefabDepth - ((sectionWidth - (2 * sidewalkSize)) - fill));
                    }
                    else fb.buildingDepth = (sectionWidth - (2 * sidewalkSize)) - fill;

                    fb.AwakeCity();
                    fb.UpdateCity();
                    lb.AwakeCity();
                    lb.UpdateCity();

                    fill = 0;
                    foreach (GameObject go in rightBuildings)
                    {
                        if (go != null)
                            fill = fill + go.GetComponent<BuildingModifier>().buildingWidth;
                    }
                    if (backBuildings.Length < 0)
                    {
                        fb = backBuildings[0].GetComponent<BuildingModifier>();

                        if (rightBuildings[rightBuildings.Length - 1] != null)
                            lb = rightBuildings[rightBuildings.Length - 1].GetComponent<BuildingModifier>();
                        else
                            lb = rightBuildings[rightBuildings.Length - 3].GetComponent<BuildingModifier>();
                    }

                    if (fb.prefabDepth > ((sectionDepth - (2 * sidewalkSize)) - fill))
                    {
                        fb.buildingDepth = fb.prefabDepth;
                        lb.buildingWidth = lb.buildingWidth - (fb.prefabDepth - ((sectionDepth - (2 * sidewalkSize)) - fill));
                    }
                    else fb.buildingDepth = (sectionDepth - (2 * sidewalkSize)) - fill;

                    fb.AwakeCity();
                    fb.UpdateCity();
                    lb.AwakeCity();
                    lb.UpdateCity();

                    fill = 0;
                    foreach (GameObject go in backBuildings)
                    {
                        if (go != null)
                            fill = fill + go.GetComponent<BuildingModifier>().buildingWidth;
                    }
                    if (leftBuildings.Length < 0)
                    {
                        fb = leftBuildings[0].GetComponent<BuildingModifier>();

                        if (backBuildings[backBuildings.Length - 1] != null)
                            lb = backBuildings[backBuildings.Length - 1].GetComponent<BuildingModifier>();
                        else lb = backBuildings[backBuildings.Length - 2].GetComponent<BuildingModifier>();
                    }
                    if (fb.prefabDepth > ((sectionDepth - (2 * sidewalkSize)) - fill))
                    {
                        fb.buildingDepth = fb.prefabDepth;
                        lb.buildingWidth = lb.buildingWidth - (fb.prefabDepth - ((sectionWidth - (2 * sidewalkSize)) - fill));
                    }
                    else fb.buildingDepth = (sectionWidth - (2 * sidewalkSize)) - fill;



                    fb.AwakeCity();
                    fb.UpdateCity();
                    lb.AwakeCity();
                    lb.UpdateCity();
                }
            }
        }

        public void GenerateBuildingsOld()
        {
            if (cityRandomizerParent == null)
                cityRandomizerParent = GameObject.Find("CScape City").GetComponent<CityRandomizer>();

            if (streetType == CScapeStreetType.Street)
            {
                Random.InitState(randomSeed);
                int rndIteration = randomSeed;
                int accumulateDistanceFront = Mathf.FloorToInt(lenghtFront);
                int accumulateDistanceBack = Mathf.FloorToInt(lenghtBack);
                int accumulateDistanceRight = Mathf.FloorToInt(lenghtRight);
                int accumulateDistanceLeft = Mathf.FloorToInt(lenghtLeft);

                ///Calculate average sizes


                System.Array.Resize(ref frontBuildingPositions, 0);
                System.Array.Resize(ref backBuildingPositions, 0);
                System.Array.Resize(ref leftBuildingPositions, 0);
                System.Array.Resize(ref rightBuildingPositions, 0);

                int averageBSizeFront = Mathf.FloorToInt(accumulateDistanceFront / ((averageBuildingSizeMin + averageBuildingSizeMax) / 2));
                System.Array.Resize(ref frontBuildingPositions, averageBSizeFront);





                if (useSkewFB) accumulateDistanceLeft = Mathf.FloorToInt(lenghtLeft);
                for (int b = 0; b < frontBuildings.Length; b++)
                {
                    if (frontBuildings[b] != null)
                        DestroyImmediate(frontBuildings[b]);
                }
                for (int b = 0; b < backBuildings.Length; b++)
                {
                    if (backBuildings[b] != null)
                        DestroyImmediate(backBuildings[b]);
                }
                for (int b = 0; b < leftBuildings.Length; b++)
                {
                    if (leftBuildings[b] != null)
                        DestroyImmediate(leftBuildings[b]);
                }
                for (int b = 0; b < rightBuildings.Length; b++)
                {
                    if (rightBuildings[b] != null)
                        DestroyImmediate(rightBuildings[b]);
                }


                System.Array.Resize(ref frontBuildings, 0);
                System.Array.Resize(ref backBuildings, 0);
                System.Array.Resize(ref leftBuildings, 0);
                System.Array.Resize(ref rightBuildings, 0);
                //// calculate max extrusion
                int maxBuildingDepth = Mathf.Min(sectionDepth, sectionWidth);
                bool maxSideIsFront = false;
                if (sectionDepth < sectionWidth) maxSideIsFront = true;

                ///////////// Calculate Front Buildings
                #region calculate front side
                int i = 0;
                while (i < accumulateDistanceFront)
                {
                    rndIteration = rndIteration + i;
                    Random.InitState(rndIteration);
                    bool validate = false;
                    GameObject prefabToInstantiate = null;
                    int iterations = 0;
                    bool start = true;
                    int prefabChoice = Random.Range(0, districtStyle.prefabs.Length - 1);


                    while (!validate && iterations < districtStyle.prefabs.Length)
                    {
                        rndIteration = rndIteration + i;
                        Random.InitState(rndIteration);
                        if (start == true)
                        {
                            prefabToInstantiate = districtStyle.prefabs[prefabChoice];
                            start = false;
                        }
                        else
                        {
                            if (prefabChoice < districtStyle.prefabs.Length)
                                prefabChoice++;
                            else prefabChoice = 0;
                            prefabToInstantiate = districtStyle.prefabs[prefabChoice];
                        }
                        validate = true;
                    }
                    if (iterations == districtStyle.prefabs.Length) prefabToInstantiate = districtStyle.prefabs[0];

                    GameObject cloneH = Instantiate(prefabToInstantiate, new Vector3(gameObject.transform.position.x + sidewalkSize * 3, gameObject.transform.position.y, gameObject.transform.position.z + sidewalkSize * 3), transform.rotation) as GameObject;

                    cloneH.transform.parent = cityRandomizerParent.buildings.transform;
                    cloneH.layer = 19;
                    BuildingModifier bm = cloneH.GetComponent(typeof(BuildingModifier)) as BuildingModifier;

                    if (i == 0)
                    {
                        bm.supportSkewing = true;
                        if (useSkewFB)
                            bm.skew = skewRotationFront;
                        else bm.skew = 90 - skewRotationLeft;
                    }
                    bm.buildingWidth = Random.Range(averageBuildingSizeMin, averageBuildingSizeMax);
                    //   if (bm.gameObject != )
                    if (maxSideIsFront)
                        bm.buildingDepth = Random.Range(bm.prefabDepth, sectionDepth / 2);
                    else bm.buildingDepth = Random.Range(bm.prefabDepth, maxBuildingDepth / 2 - sidewalkSize * 2);


                    System.Array.Resize(ref frontBuildings, frontBuildings.Length + 1);
                    frontBuildings[frontBuildings.Length - 1] = cloneH;

                    if (useSkewFB)
                    {
                        cloneH.transform.Rotate(new Vector3(0, skewRotationFront, 0));
                    }
                    cloneH.transform.Translate(new Vector3(i * 3, 0, Random.Range(-0.03f, 0.03f)));
                    if (bm.buildingWidth > bm.prefabWidth)
                    {

                        i = i + bm.buildingWidth;
                    }

                    else
                    {
                        bm.buildingWidth = bm.prefabWidth;
                        i = i + bm.buildingWidth;
                    }
                    // bm.buildingDepth = Random.Range(averageBuildingSizeMin, averageBuildingSizeMax);
                    //      bm.buildingDepth = Random.Range(0, averageBuildingDepthMax);

                    ////randomize  only surfaces           
                    int rnd = Random.Range(0, cityRandomizerParent.nightColors.Length - 1);
                    bm.colorVariation.x = Random.Range(0, 9);
                    bm.colorVariation.y = Mathf.Floor(Random.Range(0, 9));
                    bm.colorVariation.z = Mathf.Floor(Random.Range(0, 9));
                    bm.colorVariation.w = Mathf.Floor(Random.Range(0, 9));

                    bm.colorVariation2.x = Random.Range(0, 9);
                    bm.colorVariation2.y = Random.Range(0, 9);
                    bm.colorVariation2.z = Random.Range(0, 9);
                    bm.colorVariation2.w = Random.Range(2, 9);

                    bm.colorVariation3.x = Random.Range(0, 10);
                    bm.colorVariation3.y = Random.Range(0, 10);
                    bm.colorVariation3.z = Random.Range(0, 10);
                    bm.colorVariation3.w = Random.Range(0, 10);

                    bm.colorVariation4.x = Random.Range(0, 10);
                    bm.colorVariation4.y = Random.Range(0, 10);
                    bm.colorVariation4.z = Random.Range(0, 10);
                    bm.colorVariation4.w = Random.Range(0, 10);
                    bm.lightnessFront = Random.Range(0, 10);
                    bm.lightnessSide = Random.Range(0, 10);
                    bm.colorVariation5.x = Random.Range(0, 10);
                    bm.colorVariation5.y = Random.Range(0, 10);
                    bm.borderCol = Random.Range(0, 10);
                    bm.lightsOnOff = Random.Range(0, 10);
                    bm.floorNumber = Random.Range(cityRandomizerParent.minFloors, cityRandomizerParent.maxFloors);
                    bm.rooftopID = Random.Range(0, 10);
                    bm.scale = Random.Range(0.9F, 1.25F);
                    if (useGraffiti)
                        bm.useGraffiti = true;



                    //          if (height)
                    //             bm.floorNumber = Random.Range(cityRandomizerParent.minFloors, cityRandomizerParent.maxFloors + Mathf.CeilToInt(cityRandomizerParent.cityCurve.Evaluate(dist / (blockDistances * 3f * numberOfBuildingsX / 2)) * 200f));
                    bm.uniqueMapping = Random.Range(-160, 160);
                    if (cityRandomizerParent.faccadeStyles)
                    {
                        bm.colorVariation2.y = Random.Range(0, 9);
                        bm.colorVariation2.z = Random.Range(0, 9);
                        bm.materialId1 = Random.Range(cityRandomizerParent.minMatIndex, cityRandomizerParent.maxMatIndex);
                        bm.materialId2 = Random.Range(cityRandomizerParent.minMatIndex1, cityRandomizerParent.maxMatIndex1);
                        bm.materialId3 = Random.Range(cityRandomizerParent.minMatIndex2, cityRandomizerParent.maxMatIndex2);
                        bm.materialId4 = Random.Range(cityRandomizerParent.minMatIndex4, cityRandomizerParent.maxMatIndex4);
                        bm.materialId5 = Random.Range(0, 22);
                    }
                    if (cityRandomizerParent.openWindow)
                        bm.windowOpen = Random.Range(cityRandomizerParent.minWindowOpen, cityRandomizerParent.maxWindowOpen);
                    if (cityRandomizerParent.rndPatternHorizontal)
                        bm.pattern = Random.Range(0f, 1f);
                    if (distantiateBuildings > 0)
                        i = i + Mathf.RoundToInt(Random.Range(0, distantiateBuildings));
                    bm.AwakeCity();
                    bm.UpdateCity();





                    if (i > sectionWidth - (2 * sidewalkSize))
                    {
                        DestroyImmediate(cloneH);
                    }

                }
                #endregion
                ///////// Calculate Back Builldings
                #region calculate back side
                i = 0;
                while (i + sidewalkSize * 2 < accumulateDistanceBack)
                {
                    rndIteration = rndIteration + i;
                    Random.InitState(rndIteration);
                    bool validate = false;
                    GameObject prefabToInstantiate = null;
                    int iterations = 0;
                    bool start = true;
                    int prefabChoice = Random.Range(0, districtStyle.prefabs.Length - 1);


                    while (!validate && iterations < districtStyle.prefabs.Length)
                    {
                        rndIteration = rndIteration + i;
                        Random.InitState(rndIteration);
                        if (start == true)
                        {
                            prefabToInstantiate = districtStyle.prefabs[prefabChoice];
                            start = false;
                        }
                        else
                        {
                            if (prefabChoice < districtStyle.prefabs.Length)
                                prefabChoice++;
                            else prefabChoice = 0;
                            prefabToInstantiate = districtStyle.prefabs[prefabChoice];
                        }
                        validate = true;
                    }
                    if (iterations == districtStyle.prefabs.Length) prefabToInstantiate = districtStyle.prefabs[0];

                    int slit = 0;
                    if (useSkewLR) slit = slitRB;
                    GameObject cloneH;

                    if (useSkewLR) cloneH = Instantiate(prefabToInstantiate, new Vector3(gameObject.transform.position.x + (sectionWidth * 3) - sidewalkSize * 3 - slit * 3, gameObject.transform.position.y, gameObject.transform.position.z + (sectionDepth * 3) - sidewalkSize * 3), transform.rotation) as GameObject;
                    else cloneH = Instantiate(prefabToInstantiate, new Vector3(gameObject.transform.position.x + (sectionWidth * 3) - sidewalkSize * 3 - slit * 3, gameObject.transform.position.y, gameObject.transform.position.z + (sectionDepth * 3 - slitRB * 3) - sidewalkSize * 3), transform.rotation) as GameObject;
                    cloneH.transform.parent = cityRandomizerParent.buildings.transform;
                    cloneH.layer = 19;

                    BuildingModifier bm = cloneH.GetComponent(typeof(BuildingModifier)) as BuildingModifier;
                    if (i == 0)
                    {
                        bm.supportSkewing = true;

                        bm.skew = skewRotationBack;
                        if (useSkewLR) bm.skew = 90 - skewRotationRight;

                    }
                    bm.buildingWidth = Random.Range(averageBuildingSizeMin, averageBuildingSizeMax);
                    System.Array.Resize(ref backBuildings, backBuildings.Length + 1);
                    backBuildings[backBuildings.Length - 1] = cloneH;


                    if (useSkewFB) cloneH.transform.Rotate(new Vector3(0, skewRotationBack, 0));
                    else cloneH.transform.Rotate(new Vector3(0, 180, 0));
                    cloneH.transform.Translate(new Vector3(i * 3, 0, Random.Range(-0.03f, 0.03f)));
                    if (bm.buildingWidth > bm.prefabWidth)
                    {

                        i = i + bm.buildingWidth;
                    }

                    else
                    {
                        bm.buildingWidth = bm.prefabWidth;
                        i = i + bm.buildingWidth;
                    }

                    if (maxSideIsFront)
                        bm.buildingDepth = Random.Range(bm.prefabDepth, sectionDepth / 2);
                    else bm.buildingDepth = Random.Range(bm.prefabDepth, maxBuildingDepth / 2 - sidewalkSize * 2);


                    ////randomize  only surfaces           
                    int rnd = Random.Range(0, cityRandomizerParent.nightColors.Length - 1);
                    bm.colorVariation.x = Random.Range(0, 9);
                    bm.colorVariation.y = Mathf.Floor(Random.Range(0, 10));
                    bm.colorVariation.z = Mathf.Floor(Random.Range(0, 10));
                    bm.colorVariation.w = Mathf.Floor(Random.Range(0, 10));

                    bm.colorVariation2.x = Random.Range(0, 9);
                    bm.colorVariation2.y = Random.Range(0, 9);
                    bm.colorVariation2.z = Random.Range(0, 9);
                    bm.colorVariation2.w = Random.Range(2, 9);

                    bm.colorVariation3.x = Random.Range(0, 10);
                    bm.colorVariation3.y = Random.Range(0, 10);
                    bm.colorVariation3.z = Random.Range(0, 10);
                    bm.colorVariation3.w = Random.Range(0, 10);

                    bm.colorVariation4.x = Random.Range(0, 10);
                    bm.colorVariation4.y = Random.Range(0, 10);
                    bm.colorVariation4.z = Random.Range(0, 10);
                    bm.colorVariation4.w = Random.Range(0, 10);
                    bm.lightnessFront = Random.Range(0, 10);
                    bm.lightnessSide = Random.Range(0, 10);
                    bm.colorVariation5.x = Random.Range(0, 10);
                    bm.colorVariation5.y = Random.Range(0, 10);
                    bm.borderCol = Random.Range(0, 10);
                    bm.lightsOnOff = Random.Range(0, 10);
                    bm.floorNumber = Random.Range(cityRandomizerParent.minFloors, cityRandomizerParent.maxFloors);
                    bm.rooftopID = Random.Range(0, 10);
                    bm.scale = Random.Range(0.9F, 1.25F);
                    if (useGraffiti)
                        bm.useGraffiti = true;



                    //          if (height)
                    //             bm.floorNumber = Random.Range(cityRandomizerParent.minFloors, cityRandomizerParent.maxFloors + Mathf.CeilToInt(cityRandomizerParent.cityCurve.Evaluate(dist / (blockDistances * 3f * numberOfBuildingsX / 2)) * 200f));
                    bm.uniqueMapping = Random.Range(-160, 160);
                    if (cityRandomizerParent.faccadeStyles)
                    {
                        bm.colorVariation2.y = Random.Range(0, 9);
                        bm.colorVariation2.z = Random.Range(0, 9);
                        bm.materialId1 = Random.Range(cityRandomizerParent.minMatIndex, cityRandomizerParent.maxMatIndex);
                        bm.materialId2 = Random.Range(cityRandomizerParent.minMatIndex1, cityRandomizerParent.maxMatIndex1);
                        bm.materialId3 = Random.Range(cityRandomizerParent.minMatIndex2, cityRandomizerParent.maxMatIndex2);
                        bm.materialId4 = Random.Range(cityRandomizerParent.minMatIndex4, cityRandomizerParent.maxMatIndex4);
                        bm.materialId5 = Random.Range(0, 22);
                    }
                    if (cityRandomizerParent.openWindow)
                        bm.windowOpen = Random.Range(cityRandomizerParent.minWindowOpen, cityRandomizerParent.maxWindowOpen);
                    if (cityRandomizerParent.rndPatternHorizontal)
                        bm.pattern = Random.Range(0f, 1f);

                    if (distantiateBuildings > 0)
                        i = i + Mathf.RoundToInt(Random.Range(0, distantiateBuildings));

                    bm.AwakeCity();
                    bm.UpdateCity();
                    if (i > accumulateDistanceBack - 6) DestroyImmediate(cloneH);

                }
                #endregion
                ///////// Calculate Back Side
                #region calculate right side

                i = 0;
                while (i + sidewalkSize < accumulateDistanceRight)
                {
                    rndIteration = rndIteration + i;
                    Random.InitState(rndIteration);
                    bool validate = false;
                    GameObject prefabToInstantiate = null;
                    int iterations = 0;
                    bool start = true;
                    int prefabChoice = Random.Range(0, districtStyle.prefabs.Length - 1);


                    while (!validate && iterations < districtStyle.prefabs.Length)
                    {
                        rndIteration = rndIteration + i;
                        Random.InitState(rndIteration);
                        if (start == true)
                        {
                            prefabToInstantiate = districtStyle.prefabs[prefabChoice];
                            start = false;
                        }
                        else
                        {
                            if (prefabChoice < districtStyle.prefabs.Length)
                                prefabChoice++;
                            else prefabChoice = 0;
                            prefabToInstantiate = districtStyle.prefabs[prefabChoice];
                        }
                        validate = true;
                    }
                    if (iterations == districtStyle.prefabs.Length) prefabToInstantiate = districtStyle.prefabs[0];

                    int slit = 0;
                    if (useSkewFB) slit = slitLF;
                    GameObject cloneH;


                    if (useSkewFB) cloneH = Instantiate(prefabToInstantiate, new Vector3(gameObject.transform.position.x + (sectionWidth * 3) - sidewalkSize * 3, gameObject.transform.position.y, gameObject.transform.position.z + sidewalkSize * 3 - slit * 3), transform.rotation) as GameObject;
                    else cloneH = Instantiate(prefabToInstantiate, new Vector3(gameObject.transform.position.x + (sectionWidth * 3) - sidewalkSize * 3, gameObject.transform.position.y, gameObject.transform.position.z + sidewalkSize * 3), transform.rotation) as GameObject;
                    cloneH.transform.parent = cityRandomizerParent.buildings.transform;
                    cloneH.layer = 19;

                    BuildingModifier bm = cloneH.GetComponent(typeof(BuildingModifier)) as BuildingModifier;
                    if (i == 0)
                    {
                        bm.supportSkewing = true;
                        if (useSkewFB)
                            bm.skew = (skewRotationFront) * -1;
                        else bm.skew = (skewRotationRight) - 90;
                    }
                    bm.buildingWidth = Random.Range(averageBuildingSizeMin, averageBuildingSizeMax);
                    System.Array.Resize(ref rightBuildings, rightBuildings.Length + 1);
                    rightBuildings[rightBuildings.Length - 1] = cloneH;


                    if (useSkewLR) cloneH.transform.Rotate(new Vector3(0, skewRotationRight, 0));
                    else cloneH.transform.Rotate(new Vector3(0, -90, 0));
                    cloneH.transform.Translate(new Vector3(i * 3, 0, Random.Range(-0.03f, 0.03f)));
                    if (bm.buildingWidth > bm.prefabWidth)
                    {

                        i = i + bm.buildingWidth;
                    }

                    else
                    {
                        bm.buildingWidth = bm.prefabWidth;
                        i = i + bm.buildingWidth;
                    }
                    //   bm.buildingDepth = Random.Range(averageBuildingSizeMin, averageBuildingSizeMax);

                    if (maxSideIsFront)
                        bm.buildingDepth = Random.Range(bm.prefabDepth, maxBuildingDepth / 3);
                    else bm.buildingDepth = Random.Range(bm.prefabDepth, sectionWidth / 2 - sidewalkSize * 2);

                    ////randomize  only surfaces           
                    int rnd = Random.Range(0, cityRandomizerParent.nightColors.Length - 1);
                    bm.colorVariation.x = Random.Range(0, 9);
                    bm.colorVariation.y = Random.Range(0, 10);
                    bm.colorVariation.z = Random.Range(0, 10);
                    bm.colorVariation.w = Random.Range(0, 10);

                    bm.colorVariation2.x = Random.Range(0, 9);
                    bm.colorVariation2.y = Random.Range(0, 9);
                    bm.colorVariation2.z = Random.Range(0, 9);
                    bm.colorVariation2.w = Random.Range(2, 9);

                    bm.colorVariation3.x = Random.Range(0, 10);
                    bm.colorVariation3.y = Random.Range(0, 10);
                    bm.colorVariation3.z = Random.Range(0, 10);
                    bm.colorVariation3.w = Random.Range(0, 10);

                    bm.colorVariation4.x = Random.Range(0, 10);
                    bm.colorVariation4.y = Random.Range(0, 10);
                    bm.colorVariation4.z = Random.Range(0, 10);
                    bm.colorVariation4.w = Random.Range(0, 10);
                    bm.lightnessFront = Random.Range(0, 10);
                    bm.lightnessSide = Random.Range(0, 10);
                    bm.colorVariation5.x = Random.Range(0, 10);
                    bm.colorVariation5.y = Random.Range(0, 10);
                    bm.borderCol = Random.Range(0, 10);
                    bm.lightsOnOff = Random.Range(0, 10);
                    bm.floorNumber = Random.Range(cityRandomizerParent.minFloors, cityRandomizerParent.maxFloors);
                    bm.rooftopID = Random.Range(0, 10);
                    bm.scale = Random.Range(0.9F, 1.25F);
                    if (useGraffiti)
                        bm.useGraffiti = true;


                    //          if (height)
                    //             bm.floorNumber = Random.Range(cityRandomizerParent.minFloors, cityRandomizerParent.maxFloors + Mathf.CeilToInt(cityRandomizerParent.cityCurve.Evaluate(dist / (blockDistances * 3f * numberOfBuildingsX / 2)) * 200f));
                    bm.uniqueMapping = Random.Range(-160, 160);
                    if (cityRandomizerParent.faccadeStyles)
                    {
                        bm.colorVariation2.y = Random.Range(0, 9);
                        bm.colorVariation2.z = Random.Range(0, 9);
                        bm.materialId1 = Random.Range(cityRandomizerParent.minMatIndex, cityRandomizerParent.maxMatIndex);
                        bm.materialId2 = Random.Range(cityRandomizerParent.minMatIndex1, cityRandomizerParent.maxMatIndex1);
                        bm.materialId3 = Random.Range(cityRandomizerParent.minMatIndex2, cityRandomizerParent.maxMatIndex2);
                        bm.materialId4 = Random.Range(cityRandomizerParent.minMatIndex4, cityRandomizerParent.maxMatIndex4);
                        bm.materialId5 = Random.Range(0, 22);
                    }
                    if (cityRandomizerParent.openWindow)
                        bm.windowOpen = Random.Range(cityRandomizerParent.minWindowOpen, cityRandomizerParent.maxWindowOpen);
                    if (cityRandomizerParent.rndPatternHorizontal)
                        bm.pattern = Random.Range(0f, 1f);

                    if (distantiateBuildings > 0)
                        i = i + Mathf.RoundToInt(Random.Range(0, distantiateBuildings));

                    bm.AwakeCity();
                    bm.UpdateCity();
                    if (i > accumulateDistanceRight - 6) DestroyImmediate(cloneH);

                }
                #endregion

                #region calculate left side

                i = 0;
                while (i + sidewalkSize < accumulateDistanceLeft)
                {

                    rndIteration = rndIteration + i;
                    Random.InitState(rndIteration);

                    bool validate = false;
                    GameObject prefabToInstantiate = null;
                    int iterations = 0;
                    bool start = true;
                    int prefabChoice = Random.Range(0, districtStyle.prefabs.Length - 1);


                    while (!validate && iterations < districtStyle.prefabs.Length)
                    {
                        rndIteration = rndIteration + i;
                        Random.InitState(rndIteration);

                        if (start == true)
                        {
                            prefabToInstantiate = districtStyle.prefabs[prefabChoice];
                            start = false;
                        }
                        else
                        {
                            if (prefabChoice < districtStyle.prefabs.Length)
                                prefabChoice++;
                            else prefabChoice = 0;
                            prefabToInstantiate = districtStyle.prefabs[prefabChoice];
                        }
                        validate = true;
                    }
                    if (iterations == districtStyle.prefabs.Length) prefabToInstantiate = districtStyle.prefabs[0];

                    int slit = 0;
                    if (useSkewFB) slit = slitRB;
                    GameObject cloneH;
                    if (useSkewFB) cloneH = Instantiate(prefabToInstantiate, new Vector3(gameObject.transform.position.x + sidewalkSize * 3, gameObject.transform.position.y, gameObject.transform.position.z + sectionDepth * 3 - sidewalkSize * 3), transform.rotation) as GameObject;
                    else cloneH = Instantiate(prefabToInstantiate, new Vector3(gameObject.transform.position.x + sidewalkSize * 3 - slitLF * 3, gameObject.transform.position.y, gameObject.transform.position.z + sectionDepth * 3 - sidewalkSize * 3), transform.rotation) as GameObject;
                    cloneH.transform.parent = cityRandomizerParent.buildings.transform;
                    cloneH.layer = 19;

                    BuildingModifier bm = cloneH.GetComponent(typeof(BuildingModifier)) as BuildingModifier;
                    if (i == 0)
                    {
                        bm.supportSkewing = true;
                        if (useSkewFB)
                            bm.skew = (skewRotationBack) * -1;
                        else bm.skew = (90 + skewRotationLeft);
                    }

                    bm.buildingWidth = Random.Range(averageBuildingSizeMin, averageBuildingSizeMax);
                    System.Array.Resize(ref leftBuildings, leftBuildings.Length + 1);
                    leftBuildings[leftBuildings.Length - 1] = cloneH;


                    if (useSkewLR) cloneH.transform.Rotate(new Vector3(0, skewRotationLeft, 0));
                    else cloneH.transform.Rotate(new Vector3(0, 90, 0));
                    cloneH.transform.Translate(new Vector3(i * 3, 0, Random.Range(-0.03f, 0.03f)));
                    if (bm.buildingWidth > bm.prefabWidth)
                    {

                        i = i + bm.buildingWidth;
                    }

                    else
                    {
                        bm.buildingWidth = bm.prefabWidth;
                        i = i + bm.buildingWidth;
                    }
                    //    bm.buildingDepth = Random.Range(averageBuildingSizeMin, averageBuildingSizeMax);
                    if (maxSideIsFront)
                        bm.buildingDepth = Random.Range(bm.prefabDepth, maxBuildingDepth / 3);
                    else bm.buildingDepth = Random.Range(bm.prefabDepth, sectionWidth / 2 - sidewalkSize * 2);
                    ////randomize  only surfaces           
                    int rnd = Random.Range(0, cityRandomizerParent.nightColors.Length - 1);
                    bm.colorVariation.x = Random.Range(0, 9);
                    bm.colorVariation.y = Random.Range(0, 10);
                    bm.colorVariation.z = Random.Range(0, 10);
                    bm.colorVariation.w = Random.Range(0, 10);

                    bm.colorVariation2.x = Random.Range(0, 9);
                    bm.colorVariation2.y = Random.Range(0, 9);
                    bm.colorVariation2.z = Random.Range(0, 9);
                    bm.colorVariation2.w = Random.Range(2, 9);

                    bm.colorVariation3.x = Random.Range(0, 10);
                    bm.colorVariation3.y = Random.Range(0, 10);
                    bm.colorVariation3.z = Random.Range(0, 10);
                    bm.colorVariation3.w = Random.Range(0, 10);

                    bm.colorVariation4.x = Random.Range(0, 10);
                    bm.colorVariation4.y = Random.Range(0, 10);
                    bm.colorVariation4.z = Random.Range(0, 10);
                    bm.colorVariation4.w = Random.Range(0, 10);
                    bm.lightnessFront = Random.Range(0, 10);
                    bm.lightnessSide = Random.Range(0, 10);
                    bm.colorVariation5.x = Random.Range(0, 10);
                    bm.colorVariation5.y = Random.Range(0, 10);
                    bm.borderCol = Random.Range(0, 10);
                    bm.lightsOnOff = Random.Range(0, 10);
                    bm.rooftopID = Random.Range(0, 10);
                    bm.scale = Random.Range(0.9F, 1.25F);
                    if (useGraffiti)
                        bm.useGraffiti = true;



                    //          if (height)
                    //             bm.floorNumber = Random.Range(cityRandomizerParent.minFloors, cityRandomizerParent.maxFloors + Mathf.CeilToInt(cityRandomizerParent.cityCurve.Evaluate(dist / (blockDistances * 3f * numberOfBuildingsX / 2)) * 200f));
                    bm.uniqueMapping = Random.Range(-160, 160);
                    if (cityRandomizerParent.faccadeStyles)
                    {
                        bm.colorVariation2.y = Random.Range(0, 9);
                        bm.colorVariation2.z = Random.Range(0, 9);
                        bm.materialId1 = Random.Range(cityRandomizerParent.minMatIndex, cityRandomizerParent.maxMatIndex);
                        bm.materialId2 = Random.Range(cityRandomizerParent.minMatIndex1, cityRandomizerParent.maxMatIndex1);
                        bm.materialId3 = Random.Range(cityRandomizerParent.minMatIndex2, cityRandomizerParent.maxMatIndex2);
                        bm.materialId4 = Random.Range(cityRandomizerParent.minMatIndex4, cityRandomizerParent.maxMatIndex4);
                        bm.materialId5 = Random.Range(0, 22);
                    }
                    if (cityRandomizerParent.openWindow)
                        bm.windowOpen = Random.Range(cityRandomizerParent.minWindowOpen, cityRandomizerParent.maxWindowOpen);
                    if (cityRandomizerParent.rndPatternHorizontal)
                        bm.pattern = Random.Range(0f, 1f);

                    if (distantiateBuildings > 0)
                        i = i + Mathf.RoundToInt(Random.Range(0, distantiateBuildings));

                    bm.AwakeCity();
                    bm.UpdateCity();
                    if (i > accumulateDistanceLeft) DestroyImmediate(cloneH);

                }
                #endregion
                bool deb = true;
                /////check array consistency
                int count = 0;
                foreach (GameObject arGo in frontBuildings)
                {
                    if (arGo != null) count++;
                }
                System.Array.Resize(ref frontBuildings, count);
                count = 0;
                foreach (GameObject arGo in rightBuildings)
                {
                    if (arGo != null) count++;
                }
                System.Array.Resize(ref rightBuildings, count);
                count = 0;
                foreach (GameObject arGo in backBuildings)
                {
                    if (arGo != null) count++;
                }
                System.Array.Resize(ref backBuildings, count);
                count = 0;
                foreach (GameObject arGo in leftBuildings)
                {
                    if (arGo != null) count++;
                }
                System.Array.Resize(ref leftBuildings, count);
                /////

                if (distantiateBuildings == 0 && deb)
                {
                    int fill = 0;
                    foreach (GameObject go in leftBuildings)
                    {
                        if (go != null)
                            fill = fill + go.GetComponent<BuildingModifier>().buildingWidth;
                    }
                    BuildingModifier fb = frontBuildings[0].GetComponent<BuildingModifier>();
                    BuildingModifier lb;
                    if (leftBuildings[leftBuildings.Length - 1] != null)
                        lb = leftBuildings[leftBuildings.Length - 1].GetComponent<BuildingModifier>();
                    else lb = leftBuildings[leftBuildings.Length - 2].GetComponent<BuildingModifier>();

                    if (fb.prefabDepth > ((sectionDepth - (2 * sidewalkSize)) - fill))
                    {
                        fb.buildingDepth = fb.prefabDepth;
                        lb.buildingWidth = lb.buildingWidth - (fb.prefabDepth - ((Mathf.FloorToInt(lenghtLeft) - (2 * sidewalkSize)) - fill));
                    }
                    else fb.buildingDepth = (sectionDepth - (2 * sidewalkSize)) - fill;

                    fb.AwakeCity();
                    fb.UpdateCity();
                    lb.AwakeCity();
                    lb.UpdateCity();

                    fill = 0;
                    foreach (GameObject go in frontBuildings)
                    {
                        if (go != null)
                            fill = fill + go.GetComponent<BuildingModifier>().buildingWidth;
                    }
                    if (rightBuildings.Length < 0)
                    {
                        fb = rightBuildings[0].GetComponent<BuildingModifier>();

                        if (frontBuildings[frontBuildings.Length - 1] != null)
                            lb = frontBuildings[frontBuildings.Length - 1].GetComponent<BuildingModifier>();
                        else lb = frontBuildings[frontBuildings.Length - 2].GetComponent<BuildingModifier>();
                    }
                    if (fb.prefabDepth > ((sectionWidth - (2 * sidewalkSize)) - fill))
                    {
                        fb.buildingDepth = fb.prefabDepth;
                        lb.buildingWidth = lb.buildingWidth - (fb.prefabDepth - ((sectionWidth - (2 * sidewalkSize)) - fill));
                    }
                    else fb.buildingDepth = (sectionWidth - (2 * sidewalkSize)) - fill;

                    fb.AwakeCity();
                    fb.UpdateCity();
                    lb.AwakeCity();
                    lb.UpdateCity();

                    fill = 0;
                    foreach (GameObject go in rightBuildings)
                    {
                        if (go != null)
                            fill = fill + go.GetComponent<BuildingModifier>().buildingWidth;
                    }
                    if (backBuildings.Length < 0)
                    {
                        fb = backBuildings[0].GetComponent<BuildingModifier>();

                        if (rightBuildings[rightBuildings.Length - 1] != null)
                            lb = rightBuildings[rightBuildings.Length - 1].GetComponent<BuildingModifier>();
                        else
                            lb = rightBuildings[rightBuildings.Length - 3].GetComponent<BuildingModifier>();
                    }

                    if (fb.prefabDepth > ((sectionDepth - (2 * sidewalkSize)) - fill))
                    {
                        fb.buildingDepth = fb.prefabDepth;
                        lb.buildingWidth = lb.buildingWidth - (fb.prefabDepth - ((sectionDepth - (2 * sidewalkSize)) - fill));
                    }
                    else fb.buildingDepth = (sectionDepth - (2 * sidewalkSize)) - fill;

                    fb.AwakeCity();
                    fb.UpdateCity();
                    lb.AwakeCity();
                    lb.UpdateCity();

                    fill = 0;
                    foreach (GameObject go in backBuildings)
                    {
                        if (go != null)
                            fill = fill + go.GetComponent<BuildingModifier>().buildingWidth;
                    }
                    if (leftBuildings.Length < 0)
                    {
                        fb = leftBuildings[0].GetComponent<BuildingModifier>();

                        if (backBuildings[backBuildings.Length - 1] != null)
                            lb = backBuildings[backBuildings.Length - 1].GetComponent<BuildingModifier>();
                        else lb = backBuildings[backBuildings.Length - 2].GetComponent<BuildingModifier>();
                    }
                    if (fb.prefabDepth > ((sectionDepth - (2 * sidewalkSize)) - fill))
                    {
                        fb.buildingDepth = fb.prefabDepth;
                        lb.buildingWidth = lb.buildingWidth - (fb.prefabDepth - ((sectionWidth - (2 * sidewalkSize)) - fill));
                    }
                    else fb.buildingDepth = (sectionWidth - (2 * sidewalkSize)) - fill;



                    fb.AwakeCity();
                    fb.UpdateCity();
                    lb.AwakeCity();
                    lb.UpdateCity();
                }
            }
        }


        public void GenerateDetails()
        {

            if (streetDetailObj)
            {
                CSInstantiator csInst = streetDetailObj.GetComponent<CSInstantiator>();
                csInst.DeleteSolution();
                DestroyImmediate(streetDetailObj);
            }

            streetDetailObj = Instantiate(cityRandomizerParent.streetDetailPrefabs[0], transform.position, transform.rotation) as GameObject;
            streetDetailObj.transform.parent = transform;
            streetDetailObj.transform.name = name + "Street_";
            CSInstantiator streetDetailsModifier = streetDetailObj.GetComponent(typeof(CSInstantiator)) as CSInstantiator;
            streetDetailObj.SetActive(true);

            streetDetailsModifier.depth = sectionWidth;
            streetDetailsModifier.width = sectionDepth;
            if (useSkewFB || useSkewLR)
            {
                streetDetailsModifier.useSkewing = true;
            }
            streetDetailsModifier.streetParent = gameObject;
            streetDetailsModifier.AwakeMe();



        }






        public void DeleteChildrenBuildings()
        {
            for (int i = 0; i < childrenBuildings.Length; i++)
            {
                if (childrenBuildings[i])
                    DestroyImmediate(childrenBuildings[i]);
            }

        }

        float CompressIDs(int iDInput)
        {

            float idOut = (iDInput + 1f) / 10f;


            return idOut;


        }

        float CompressVector2(Vector2 vectorInput)
        {
            ///compress vector2 Index
            float idOut = 0;
            idOut = (Mathf.Floor(vectorInput.x)) + (vectorInput.y % 1);


            return idOut;


        }

        public void SplitX()
        {
            int oldWidth = blockWidth;
            int oldDepth = blockDepth;
            int oldStreetSize = blockDepth - sectionDepth;
            blockDepth = Mathf.FloorToInt(oldDepth / 2f);
            int newBlockDepth = Mathf.CeilToInt(oldDepth / 2f);



            GameObject newStreet = Instantiate(gameObject);
            newStreet.transform.Translate(0, 0, 3 * blockDepth);
            StreetModifier sm = newStreet.GetComponent<StreetModifier>();
            sm.sectionDepth = newBlockDepth - (oldDepth - sm.sectionDepth);
            sectionDepth = blockDepth - (oldDepth - sectionDepth);
            sm.blockDepth = newBlockDepth;
            if (useSkewFB) sm.slitLF = sm.slitRB;
            newStreet.transform.parent = gameObject.transform.parent;
            AwakeCity();
            sm.AwakeCity();

        }

        public void SplitZ()
        {
            int oldWidth = blockWidth;
            int oldDepth = blockDepth;
            int oldStreetSize = blockWidth - sectionWidth;
            blockWidth = Mathf.FloorToInt(oldWidth / 2f);
            int newBlockWidth = Mathf.CeilToInt(oldWidth / 2f);



            GameObject newStreet = Instantiate(gameObject);
            newStreet.transform.Translate(3 * blockWidth, 0, 0);
            StreetModifier sm = newStreet.GetComponent<StreetModifier>();
            sm.sectionWidth = newBlockWidth - (oldWidth - sm.sectionWidth);
            sectionWidth = blockWidth - (oldWidth - sectionWidth);
            sm.blockWidth = newBlockWidth;
            if (useSkewLR) sm.slitLF = sm.slitRB;
            newStreet.transform.parent = gameObject.transform.parent;
            AwakeCity();
            sm.AwakeCity();

        }

    }
}