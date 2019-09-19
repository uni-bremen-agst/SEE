

using UnityEngine;
using System.Collections;
using System.Linq;
using CScape;
//using UnityEditor;

namespace CScape
{

    [ExecuteInEditMode]
    public class CityRandomizer : MonoBehaviour
    {
        public bool randomize = false;
        public bool updateColliders = false;
        public bool createRooftopDetails = false;
        public bool createStreets = false;
        public bool createStreetDetails = false;
        public bool deactivateModifiers = false;
        public bool activateModifiers = true;
        public int minFloors = 5;
        public int maxFloors = 50;
        public float minAdittionalScale = 0.9f;
        public float maxAdittionalScale = 1.1f;
        public int minDepth = 2;
        public int maxDepth = 10;
        public int minWidth = 2;
        public int maxWidth = 30;
        public int minMatIndex;
        public int maxMatIndex;
        public int minMatIndex1;
        public int maxMatIndex1;
        public int minMatIndex2;
        public int maxMatIndex2;
        public int minMatIndex4;
        public int maxMatIndex4;
        public float minWindowOpen = 0.5f;
        public float maxWindowOpen = 100f;
        public float patern;
       // public GameObject[] prefabs;
        public DistrictStyle[] buildingStyles;
        public GameObject[] rooftopPrefabs;
        public GameObject[] streetPrefabs;
        public Color[] nightColors;
        public GameObject[] streetDetailPrefabs;
        public GameObject[] streetLightsPrefabs;
        public GameObject[] streetFoliagePrefabs;
        public GameObject busStopPrefab;
        //  public GameObject[] streetDetailsPrefabs;
        public GameObject fbStreet;
        public GameObject lrStreet;
        public GameObject crossroad;
        public bool width;
        public bool height;
        public bool depth;
        public bool openWindow;
        public bool rndPatternHorizontal;
        public bool faccadeStyles;
        public bool rndColor;
        public bool useMeshCollider;
        public bool useConvexCollider;
        public bool generate = false;
        public bool delete = false;
        public int blockDistances;
        public int maxBlockDistances;
        public int randomSeed;

        public float cityCenterRadius = 200f;
        public int numberOfBuildingsX = 20;
        public int numberOfBuildingsZ = 20;
        public int streetSizeX = 2;
        public int streetSizeZ = 2;
        public int sidewalkSizeX = 2;
        public int sidewalkSizeZ = 2;
        public int streetSizeXmax = 2;
        public int streetSizeZmax = 2;
        public int sidewalkSizeXmax = 2;
        public int sidewalkSizeZmax = 2;
        public GameObject buildings;
        public GameObject rooftopDetails;
        public GameObject streets;
        public GameObject foliage;
        public GameObject streetDetails;
        public GameObject streetLights;
        public GameObject adverts;
        public GameObject balcony;
        public GameObject busStopsHolder;

        public AnimationCurve cityCurve;
        //public int[] streetRuleX;
        // public int[] streetRuleZ;

        public GameObject cityCenterObject;
        public Vector4[] cityRuleArrayX;
        public Vector4[] cityRuleArrayZ;
        float cityCenterWeightX;
        float cityCenterWeightZ;
        public int folliageThreshold;
        public bool subdivide = true;
        public int sectionDivisionMax;
        public int sectionDivisionMin;
        public bool useRivers = true;
        public int riverPosition = 5;
        public GameObject river;
        int citySize;

        public bool savedWithoutMesh = false;
        public bool savedCollapsed = false;
        public bool quadtree = false;
        public bool quadtreeSkew = false;
        public static bool useAo = true;
        public static float aoAngle = 3f;
        public static int aoSteps = 1;
        //mobile optimizations
        public bool optimizeMobile = false;
        public bool useGrafitti = true;
        public bool usePOM = true;
        public GameObject streetColliderTemplate;
        public bool keepStreets = false;
       



        void Start()
        {
            if (savedWithoutMesh) Refresh();
            savedWithoutMesh = false;

            if (usePOM == true)
                Shader.DisableKeyword("_CSCAPE_DESKTOP_ON");
            else Shader.EnableKeyword("_CSCAPE_DESKTOP_ON");
        }

        public void UpdateCity()
        {
            if (cityCenterObject == null) cityCenterObject = gameObject;
            // randomize = true;

            if (generate)
            {
                Generate();
            }




            if (delete)
            {
                Deletesolution();
            }



            if (randomize)
            {
                Randomize();
            }







        }

        void Deletesolution()
        {
            foreach (Transform go in buildings.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }
            if (!keepStreets)
            {
                foreach (Transform go in streets.transform.Cast<Transform>().Reverse())
                {
                    DestroyImmediate(go.gameObject);
                }
            }
            keepStreets = false;

            foreach (Transform go in rooftopDetails.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }

            foreach (Transform go in streetDetails.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }
            foreach (Transform go in foliage.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }
            foreach (Transform go in adverts.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }
            foreach (Transform go in streetLights.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }

            foreach (Transform go in balcony.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }

            foreach (Transform go in busStopsHolder.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }

            delete = false;
        }

        public void DeleteFolliage()
        {
            foreach (Transform go in foliage.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }
        }

        public void DeleteStreets()
        {
            foreach (Transform go in streets.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }
        }

        public void DeleteStreeetDetails()
        {
            foreach (Transform go in streetDetails.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }
        }

        public void DeleteBuildings()
        {
            foreach (Transform go in buildings.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }
        }

        public void DeleteLights()
        {
            foreach (Transform go in streetLights.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }
        }

        public void DeleteAdverts()
        {
            foreach (Transform go in adverts.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }
        }

        public void DeleteBusStops()
        {
            foreach (Transform go in busStopsHolder.transform.Cast<Transform>().Reverse())
            {
                DestroyImmediate(go.gameObject);
            }
        }


        public void Generate()
        {
            Deletesolution();
            //   Random.InitState(randomSeed);

            int iteration = 0;
            cityCenterWeightX = 0;
            cityCenterWeightZ = 0;

            ///Generate Random Rules            
            cityRuleArrayX = new Vector4[numberOfBuildingsX];
            cityRuleArrayZ = new Vector4[numberOfBuildingsZ];
            //GenerateDivisionRuleArray
            var sectionDivision = new Vector4[cityRuleArrayX.Length * cityRuleArrayZ.Length];

            for (int i = 0; i < cityRuleArrayX.Length; i++)
            {
                iteration = iteration + 1;
                Random.InitState(randomSeed + iteration);
                // Blocks Size
                cityRuleArrayX[i].x = Mathf.Ceil(Random.Range(blockDistances, maxBlockDistances));
                // World Position (in CS units)
                if (i == 0) cityRuleArrayX[i].y = 0;
                else cityRuleArrayX[i].y = cityRuleArrayX[i - 1].y + cityRuleArrayX[i - 1].x;
                //SidawalkSize
                cityRuleArrayX[i].z = Mathf.Ceil(Random.Range(sidewalkSizeX, sidewalkSizeXmax));
                //lane Number
                cityRuleArrayX[i].w = Mathf.Ceil(Random.Range(streetSizeX, streetSizeXmax));
                // Debug.Log(cityRuleArrayX[i].w);


            }
            for (int i = 0; i < cityRuleArrayZ.Length; i++)
            {
                iteration = iteration + 1;
                cityRuleArrayZ[i].x = Mathf.Ceil(Random.Range(blockDistances, maxBlockDistances));
                if (i == 0) cityRuleArrayZ[i].y = 0;
                else cityRuleArrayZ[i].y = cityRuleArrayZ[i - 1].y + cityRuleArrayZ[i - 1].x;

                cityRuleArrayZ[i].z = Mathf.Ceil(Random.Range(sidewalkSizeZ, sidewalkSizeZmax));
                cityRuleArrayZ[i].w = Mathf.Ceil(Random.Range(streetSizeZ, streetSizeZmax));


            }
            int size = 0;
            for (int i = 0; i < cityRuleArrayZ.Length; i++)
            {
                size = size + Mathf.FloorToInt(cityRuleArrayZ[i].x);
            }
            citySize = size;
            CityRandomizer cRandomizerThis = GetComponent<CityRandomizer>();

            for (int i = 0; i < numberOfBuildingsX; i++)
            {


                for (int j = 0; j < numberOfBuildingsZ; j++)
                {
                    iteration = iteration + 1;


                    ///divideBlockSections
                    sectionDivision[i * j] = new Vector4(Random.Range(sectionDivisionMin, sectionDivisionMax), Random.Range(sectionDivisionMin, sectionDivisionMax), 0, 0);

                    ///check if our object fits tile
                    int iteration2 = 0;
                    if (subdivide)
                    {

                        int subsX = Mathf.CeilToInt(sectionDivision[j * i].x);
                        int subsZ = Mathf.CeilToInt(sectionDivision[j * i].y);

                        for (int subi = 0; subi < subsX; subi++)
                        {
                            for (int subj = 0; subj < subsZ; subj++)
                            {
                                iteration2 = iteration2 + 1;
                                Random.InitState(randomSeed + iteration2 + iteration);

                                if (subi == 0 || subi == subsX - 1 || subj == 0 || subj == subsZ - 1)
                                {

                                    bool validate = false;
                                    GameObject prefabToInstantiate = null;
                                    int iterations = 0;
                                    bool start = true;
                                    int prefabChoice = Random.Range(0, buildingStyles[Random.Range(0, buildingStyles.Length)].prefabs.Length);
                                    
                                    
                                    

                                    while (!validate && iterations < buildingStyles[Random.Range(0, buildingStyles.Length)].prefabs.Length)
                                    {
                                        if (start == true)
                                        {
                                            prefabToInstantiate = buildingStyles[Random.Range(0, buildingStyles.Length)].prefabs[prefabChoice];
                                            start = false;
                                        }
                                        else
                                        {
                                            if (prefabChoice < buildingStyles[Random.Range(0, buildingStyles.Length)].prefabs.Length)
                                                prefabChoice++;
                                            else prefabChoice = 0;
                                           // Debug.Log(prefabs[prefabChoice].name);
                                            prefabToInstantiate = buildingStyles[Random.Range(0, buildingStyles.Length)].prefabs[prefabChoice];
                                        }

                                        BuildingModifier buildingModifierx = prefabToInstantiate.GetComponent(typeof(BuildingModifier)) as BuildingModifier;



                                        if (subj != subsZ - 1 && subj != 0)
                                        {
                                            if (buildingModifierx.prefabWidth < (Mathf.FloorToInt((cityRuleArrayX[i].x - (((cityRuleArrayX[i].z) * 2) + cityRuleArrayX[i].w)) / subsX))
                                                || buildingModifierx.prefabWidth < Mathf.FloorToInt((cityRuleArrayZ[j].x - (((cityRuleArrayZ[j].z) * 2) + cityRuleArrayZ[j].w)) / subsZ)) validate = true;
                                        }

                                        else if (subj != subsZ - 1 && subj != 0 && subi == 0)
                                        {
                                            if (buildingModifierx.prefabWidth < (Mathf.FloorToInt((cityRuleArrayX[i].x - (((cityRuleArrayX[i].z) * 2) + cityRuleArrayX[i].w)) / subsX))
                                                || buildingModifierx.prefabWidth < Mathf.FloorToInt((cityRuleArrayZ[j].x - (((cityRuleArrayZ[j].z) * 2) + cityRuleArrayZ[j].w)) / subsZ)) validate = true;
                                        }

                                        else if (buildingModifierx.prefabDepth < (Mathf.FloorToInt((cityRuleArrayX[i].x - (((cityRuleArrayX[i].z) * 2) + cityRuleArrayX[i].w)) / subsX)) || buildingModifierx.prefabWidth < Mathf.FloorToInt((cityRuleArrayZ[j].x - (((cityRuleArrayZ[j].z) * 2) + cityRuleArrayZ[j].w)) / subsZ)) validate = true;

                                        iterations++;
                                    }
                                    if (iterations == buildingStyles[Random.Range(0, buildingStyles.Length)].prefabs.Length) prefabToInstantiate = buildingStyles[Random.Range(0, buildingStyles.Length)].prefabs[0];

                                    if (useRivers && riverPosition != i)
                                    {
                                        GameObject cloneH = Instantiate(prefabToInstantiate, new Vector3(((cityRuleArrayX[i].y * 3f) + (cityRuleArrayX[i].z * 3f)) + (3f * subi * (Mathf.FloorToInt((cityRuleArrayX[i].x - (((cityRuleArrayX[i].z) * 2) + cityRuleArrayX[i].w)) / subsX))), 0, ((cityRuleArrayZ[j].y * 3f) + (cityRuleArrayZ[j].z * 3f)) + (3f * subj * (Mathf.FloorToInt((cityRuleArrayZ[j].x - (((cityRuleArrayZ[j].z) * 2) + cityRuleArrayZ[j].w)) / subsZ)))), transform.rotation) as GameObject;
                                        cloneH.transform.parent = buildings.transform;
                                        cloneH.transform.name = "Section_" + (j + numberOfBuildingsZ * (i)) + "_Building_" + ((subi + 1) * (subj + 1));
                                        cloneH.layer = 19;

                                        cloneH.transform.localPosition = cloneH.transform.position - new Vector3(cityCenterWeightX, 0, cityCenterWeightZ);
                                        BuildingModifier buildingModifier = cloneH.GetComponent(typeof(BuildingModifier)) as BuildingModifier;
                                        CSRooftops csRooftopsModifier = cloneH.GetComponent(typeof(CSRooftops)) as CSRooftops;
                                        CSAdvertising csAdverts = cloneH.GetComponent(typeof(CSAdvertising)) as CSAdvertising;
                                        buildingModifier.buildingWidth = Mathf.FloorToInt((cityRuleArrayX[i].x - (((cityRuleArrayX[i].z) * 2) + cityRuleArrayX[i].w)) / subsX);
                                        buildingModifier.buildingDepth = Mathf.FloorToInt((cityRuleArrayZ[j].x - (((cityRuleArrayZ[j].z) * 2) + cityRuleArrayZ[j].w)) / subsZ);
                                        buildingModifier.cityRandomizerParent = cRandomizerThis;

                                        if (subj == subsZ - 1)
                                        {
                                            cloneH.transform.Rotate(0, 180, 0);
                                            cloneH.transform.Translate(buildingModifier.buildingWidth * -3, 0, buildingModifier.buildingDepth * -3);

                                        }

                                        if (subj != subsZ - 1 && subj != 0)
                                        {
                                            cloneH.transform.Rotate(0, -90, 0);
                                            int bd = buildingModifier.buildingDepth;
                                            buildingModifier.buildingDepth = buildingModifier.buildingWidth;
                                            buildingModifier.buildingWidth = bd;
                                            cloneH.transform.Translate(0, 0, buildingModifier.buildingDepth * -3);

                                        }

                                        if (subj != subsZ - 1 && subj != 0 && subi == 0)
                                        {
                                            cloneH.transform.Rotate(0, -180, 0);
                                            //     int bd = buildingModifier.buildingDepth;
                                            //     buildingModifier.buildingDepth = buildingModifier.buildingWidth;
                                            //     buildingModifier.buildingWidth = bd;
                                            cloneH.transform.Translate(buildingModifier.buildingWidth * -3, 0, buildingModifier.buildingDepth * -3);

                                        }


                                        if (csRooftopsModifier != null)
                                        {
                                            csRooftopsModifier.randomSeed = Random.Range(0, 1000000);
                                            csRooftopsModifier.lodDistance = 0.18f;
                                            csRooftopsModifier.instancesX = 150;
                                        }
                                        if (csAdverts != null)
                                        {
                                            csAdverts.randomSeed = Random.Range(0, 1000000);
                                        }


                                        buildingModifier.AwakeCity();
                                        buildingModifier.UpdateCity();
                                    }

                                }


                            }


                        }
                    }
                }

            }

            Randomize();
            generate = false;
        }

        public void GenerateDetails()
        {

            foreach (Transform go in streets.transform.Cast<Transform>().Reverse())
            {
                StreetModifier streetMod = go.GetComponent(typeof(StreetModifier)) as StreetModifier;
                if (streetMod != null)
                {
                    if (streetMod.streetType != StreetModifier.CScapeStreetType.River)
                    {
                        GameObject cloneH = Instantiate(streetDetailPrefabs[Random.Range(0, streetDetailPrefabs.Length)], go.transform.position, transform.rotation) as GameObject;
                        cloneH.transform.parent = streetDetails.transform;
                        cloneH.transform.name = go.name + "Street_";
                        CSInstantiator streetDetailsModifier = cloneH.GetComponent(typeof(CSInstantiator)) as CSInstantiator;
                        cloneH.SetActive(true);

                        streetDetailsModifier.depth = streetMod.sectionWidth;
                        streetDetailsModifier.width = streetMod.sectionDepth;
                        streetDetailsModifier.streetParent = go.gameObject;
                        streetDetailsModifier.AwakeMe();


                    }
                }
            }
        }


        public void GenerateBusStops()
        {

            foreach (Transform go in streets.transform.Cast<Transform>().Reverse())
            {
                StreetModifier streetMod = go.GetComponent(typeof(StreetModifier)) as StreetModifier;
                if (streetMod != null)
                {
                    if (streetMod.streetType != StreetModifier.CScapeStreetType.River)
                    {
                        bool boolValue = (Random.Range(0, 2) == 0);

                        if (boolValue)
                        {
                            GameObject cloneH = Instantiate(busStopPrefab, go.transform.position, transform.rotation) as GameObject;
                            cloneH.transform.parent = busStopsHolder.transform;
                            cloneH.transform.name = go.name + "BusStop_";
                            cloneH.transform.Rotate(-90, 0, -90);
                            cloneH.transform.Translate(streetMod.sectionDepth * 3f - 12f, 0, 0);
                            //  CSInstantiator streetDetailsModifier = cloneH.GetComponent(typeof(CSInstantiator)) as CSInstantiator;
                            cloneH.SetActive(true);

                            GameObject cloneW = Instantiate(busStopPrefab, go.transform.position, transform.rotation) as GameObject;
                            cloneW.transform.parent = busStopsHolder.transform;
                            cloneW.transform.name = go.name + "BusStop_";
                            cloneW.transform.Rotate(-90, 0, -90);
                            cloneW.transform.Translate(12f, streetMod.sectionWidth * 3f, 0);
                            //  CSInstantiator streetDetailsModifier = cloneH.GetComponent(typeof(CSInstantiator)) as CSInstantiator;
                            cloneW.transform.Rotate(0, 0, -180);
                            cloneW.SetActive(true);


                            GameObject cloneY = Instantiate(busStopPrefab, go.transform.position, transform.rotation) as GameObject;
                            cloneY.transform.parent = busStopsHolder.transform;
                            cloneY.transform.name = go.name + "BusStop_";
                            cloneY.transform.Rotate(-90, 0, 180);
                            cloneY.transform.Translate(-12f, 0, 0);
                            //  CSInstantiator streetDetailsModifier = cloneH.GetComponent(typeof(CSInstantiator)) as CSInstantiator;
                            cloneY.transform.Rotate(0, 0, 0);
                            cloneY.SetActive(true);


                        }
                    }
                }
            }
        }

        public void GenerateLights()
        {
            foreach (Transform go in streets.transform.Cast<Transform>().Reverse())
            {
                StreetModifier streetMod = go.GetComponent(typeof(StreetModifier)) as StreetModifier;
                if (streetMod != null)
                {
                    if (streetMod.streetType != StreetModifier.CScapeStreetType.River)
                    {

                        GameObject cloneH = Instantiate(streetLightsPrefabs[Random.Range(0, streetLightsPrefabs.Length)], go.transform.position, go.transform.rotation) as GameObject;
                        cloneH.transform.parent = streetLights.transform;
                        cloneH.transform.Translate(new Vector3(0f, 12f, 0f));
                        cloneH.transform.name = "Light" + go.transform.name;
                        CSInstantiatorLights streetLightsModifier = cloneH.GetComponent(typeof(CSInstantiatorLights)) as CSInstantiatorLights;

                        streetLightsModifier.depth = streetMod.blockDepth;
                        streetLightsModifier.width = streetMod.blockWidth;
                        //streetLightsModifier.Awake();
                        streetLightsModifier.UpdateElements();



                    }
                }
            }
        }

        public void GenerateFolliage()
        {
            if (quadtree == false && quadtreeSkew == false) {
               // Debug.Log("isn't quadtree");
                for (int i = 0; i < numberOfBuildingsX; i++)
                {

                    for (int j = 0; j < numberOfBuildingsZ; j++)
                    {
                        if (cityRuleArrayX[i].z >= folliageThreshold && cityRuleArrayZ[j].z >= folliageThreshold)
                        {
                            if (useRivers && riverPosition != i)
                            {
                                GameObject cloneH = Instantiate(streetFoliagePrefabs[Random.Range(0, streetFoliagePrefabs.Length)], new Vector3(cityRuleArrayX[i].y * 3f, 0, cityRuleArrayZ[j].y * 3f), transform.rotation) as GameObject;
                                cloneH.transform.parent = foliage.transform;
                                cloneH.transform.name = "Street_" + j + "_" + i;
                                cloneH.transform.localPosition = cloneH.transform.position - new Vector3(cityCenterWeightX, 0, cityCenterWeightZ);
                                CSFoliageInstantiator streetDetailsModifier = cloneH.GetComponent(typeof(CSFoliageInstantiator)) as CSFoliageInstantiator;
                                streetDetailsModifier.depth = Mathf.RoundToInt(cityRuleArrayX[i].x - (cityRuleArrayX[i].w));
                                streetDetailsModifier.width = Mathf.RoundToInt(cityRuleArrayZ[j].x - (cityRuleArrayZ[j].w));
                                streetDetailsModifier.Awake();
                                streetDetailsModifier.UpdateElements();
                            }
                        }
                    }
                }
            }
            else {
                StreetModifier[] sm = streets.GetComponentsInChildren<StreetModifier>();
                for (int i = 0; i < sm.Length; i++) {
                    if (sm[i].useSkewLR == true || sm[i].useSkewFB == true || sm[i].streetType == StreetModifier.CScapeStreetType.River);
                    else
                    {
                        GameObject cloneH = Instantiate(streetFoliagePrefabs[Random.Range(0, streetFoliagePrefabs.Length)], sm[i].gameObject.transform.position, transform.rotation) as GameObject;
                        cloneH.transform.parent = foliage.transform;
                        cloneH.transform.name = "Street_" + i + "_" + i; 
                        cloneH.transform.localPosition = cloneH.transform.position - new Vector3(cityCenterWeightX, 0, cityCenterWeightZ);
                        CSFoliageInstantiator streetDetailsModifier = cloneH.GetComponent(typeof(CSFoliageInstantiator)) as CSFoliageInstantiator;
                        streetDetailsModifier.depth = sm[i].sectionWidth;
                        streetDetailsModifier.width = sm[i].sectionDepth;
                        streetDetailsModifier.Awake();
                        streetDetailsModifier.UpdateElements();
                    }
                }
            }
        }

        public void GenerateStreets()
        {
            if (quadtreeSkew || quadtree) Deletesolution();
            Random.InitState(randomSeed);
            int iteration = 0;
            ////INIT Array
            cityRuleArrayX = new Vector4[numberOfBuildingsX];
            cityRuleArrayZ = new Vector4[numberOfBuildingsZ];
            //GenerateDivisionRuleArray
            var sectionDivision = new Vector4[cityRuleArrayX.Length * cityRuleArrayZ.Length];

            for (int i = 0; i < cityRuleArrayX.Length; i++)
            {
                iteration = iteration + 1;
                Random.InitState(randomSeed + iteration);
                // Blocks Size
                cityRuleArrayX[i].x = Mathf.Ceil(Random.Range(blockDistances, maxBlockDistances));
                // World Position (in CS units)
                if (i == 0) cityRuleArrayX[i].y = 0;
                else cityRuleArrayX[i].y = cityRuleArrayX[i - 1].y + cityRuleArrayX[i - 1].x;
                //SidawalkSize
                cityRuleArrayX[i].z = Mathf.Ceil(Random.Range(sidewalkSizeX, sidewalkSizeXmax));
                //lane Number
                cityRuleArrayX[i].w = Mathf.Ceil(Random.Range(streetSizeX, streetSizeXmax));
                // Debug.Log(cityRuleArrayX[i].w);


            }
            for (int i = 0; i < cityRuleArrayZ.Length; i++)
            {
                iteration = iteration + 1;
                cityRuleArrayZ[i].x = Mathf.Ceil(Random.Range(blockDistances, maxBlockDistances));
                if (i == 0) cityRuleArrayZ[i].y = 0;
                else cityRuleArrayZ[i].y = cityRuleArrayZ[i - 1].y + cityRuleArrayZ[i - 1].x;

                cityRuleArrayZ[i].z = Mathf.Ceil(Random.Range(sidewalkSizeZ, sidewalkSizeZmax));
                cityRuleArrayZ[i].w = Mathf.Ceil(Random.Range(streetSizeZ, streetSizeZmax));
            }

            /////////////init array end


            for (int i = 0; i < numberOfBuildingsX; i++)
            {

                for (int j = 0; j < numberOfBuildingsZ; j++)
                {
                    
                    iteration = iteration + 1;
                    //  Random.State (iteration);
                    //  Random.InitState(randomSeed + iteration);
                    GameObject cloneH;
                    if (useRivers && riverPosition == i)
                    {
                        cloneH = Instantiate(streetPrefabs[0], new Vector3(cityRuleArrayX[i].y * 3f, 0, cityRuleArrayZ[j].y * 3f), transform.rotation) as GameObject;
                        if (j == 0)
                        {
                            GameObject nordlake = Instantiate(river, new Vector3(cityRuleArrayX[i].y * 3f, 0, cityRuleArrayZ[j].y * 3f), Quaternion.identity) as GameObject;
                            nordlake.transform.position = (nordlake.transform.position + gameObject.transform.position);
                            if (!quadtreeSkew) nordlake.transform.localScale = new Vector3(Mathf.RoundToInt(cityRuleArrayX[i].x), 1, citySize);
                            else nordlake.transform.localScale = new Vector3(Mathf.RoundToInt(cityRuleArrayX[i].x), 1, numberOfBuildingsZ * maxBlockDistances);
                            nordlake.transform.name = "NordLakeRiver_" + j + "_" + i;
                            nordlake.transform.parent = streets.transform;
                            ReflectionProbe rProbe = nordlake.transform.Find("Reflection Probe").GetComponent<ReflectionProbe>();
                            rProbe.size = new Vector3(cityRuleArrayX[i].x * 3, rProbe.size.y, nordlake.transform.localScale.z * 3);
                            Debug.Log("GeneratedRiver " + cityRuleArrayX[i].y +" " + cityRuleArrayZ[i].y);
                        }
                    }
                    else
                    {
                        if (quadtreeSkew || quadtree)
                        cloneH = Instantiate(streetColliderTemplate, new Vector3(cityRuleArrayX[i].y * 3f, 0, cityRuleArrayZ[j].y * 3f), transform.rotation) as GameObject;
                        else cloneH = Instantiate(streetPrefabs[1], new Vector3(cityRuleArrayX[i].y * 3f, 0, cityRuleArrayZ[j].y * 3f), transform.rotation) as GameObject;

                    }
                    cloneH.transform.parent = streets.transform;
                    
                    cloneH.transform.name = "Street_" + j + "_" + i;
                    //       clone.transform.localPosition = clone.transform.position;
                    cloneH.transform.localPosition = cloneH.transform.position - new Vector3(cityCenterWeightX, 0, cityCenterWeightZ);

                    StreetModifier streetModifier = cloneH.GetComponent(typeof(StreetModifier)) as StreetModifier;
                    streetModifier.blockWidth = Mathf.RoundToInt(cityRuleArrayX[i].x);
                    streetModifier.blockDepth = Mathf.RoundToInt(cityRuleArrayZ[j].x);
                    streetModifier.sectionWidth = Mathf.RoundToInt(cityRuleArrayX[i].x - cityRuleArrayX[i].w);
                    streetModifier.sectionDepth = Mathf.RoundToInt(cityRuleArrayZ[j].x - cityRuleArrayZ[j].w);
                    streetModifier.sidewalkID = Mathf.RoundToInt(Random.Range(0f, 6f));
                    float value = Random.Range(0, 2);
                    if (value < 0.5)
                        streetModifier.useGraffiti = true;
                    else streetModifier.useGraffiti = false;
                    //  Debug.Log(streetModifier.sectionWidth + " street Size " + cityRuleArrayX[i].w + "Block width is: " + cityRuleArrayX[i].x);

                    if (useRivers && i == riverPosition - 1) streetModifier.protect = true;
                    if (useRivers && i == riverPosition + 1) streetModifier.protect = true;
                    if (useRivers && i == riverPosition) streetModifier.protect = true;
                    //try quadtree elimination

                    streetModifier.AwakeCity();
                    streetModifier.UpdateCity();

                }
            }


            if (quadtreeSkew) {
                iteration = QuadtreeSkew(iteration);
                var tempListst = streets.transform.Cast<Transform>().Reverse().ToList();
                foreach (var child in tempListst)
                {
                    iteration = iteration + 1;
                    var SM = child.GetComponent<StreetModifier>();
                    if (SM != null)
                    {
                        if (SM.streetType == StreetModifier.CScapeStreetType.Street)
                            SM.meshOriginal = streetPrefabs[1].GetComponent<StreetModifier>().meshOriginal;
                        SM.AwakeCity();
                        // SM.GenerateBuildings();
                    }
                }
            }
            if (quadtree)
                iteration = Quadtree(iteration);
        }

        private int QuadtreeSkew(int iteration)
        {
            {
                foreach (Transform go in streets.transform.Cast<Transform>().Reverse())
                {
                    if (Random.Range(0, 5) < 1 && go.gameObject.GetComponent<StreetModifier>())
                    {
                        if (!go.gameObject.GetComponent<StreetModifier>().protect)
                            go.gameObject.GetComponent<StreetModifier>().markToDelete = true;
                    }

                }
                iteration = iteration + 1;
                //  Random.State (iteration);
                //  Random.InitState(randomSeed + iteration);




                ///Delete random 
                var tempList = streets.transform.Cast<Transform>().Reverse().ToList();
                foreach (var child in tempList)
                {
                    if (child.gameObject.GetComponent<StreetModifier>() && child.gameObject.GetComponent<StreetModifier>().markToDelete)
                    {
                        RaycastHit hit;
                        var sM = child.GetComponent<StreetModifier>();

                        if (Physics.Raycast(child.transform.TransformPoint(new Vector3(0.03f, 0.03f, 0.1f)), child.transform.TransformDirection(new Vector3(0, 0, 1)), out hit) && sM && 0 == 0)
                        {
                            if (hit.transform.GetComponent<StreetModifier>())
                            {
                                if (hit.transform.GetComponent<StreetModifier>() && hit.transform.GetComponent<StreetModifier>().streetType != StreetModifier.CScapeStreetType.River)
                                {

                                    if (!sM.protect) DestroyImmediate(child.gameObject);
                                }

                            }
                        }

                    }
                }
                //Search Extensions
                var tempListst = streets.transform.Cast<Transform>().Reverse().ToList();
                foreach (var child in tempListst)
                {
                    RaycastHit hit;
                    Vector3 fwd = child.transform.TransformDirection(new Vector3(0, 0, 1));
                    Vector3 pos = child.transform.TransformPoint(new Vector3(0.03f, 0.03f, 0.1f));
                    var sM = child.GetComponent<StreetModifier>();

                    if (Physics.Raycast(pos, fwd, out hit) && sM && 0 == 0 && !sM.protect)
                    {
                        if (hit.distance > ((sM.blockDepth / 3f) - 1f))
                        {
                            var streetSize = sM.blockDepth - sM.sectionDepth;
                            sM.blockDepth = Mathf.CeilToInt(hit.distance / 3f);
                            sM.sectionDepth = sM.blockDepth - streetSize;
                            sM.AwakeCity();
                            sM.ModifyStreet();
                        }
                    }
                    fwd = child.transform.TransformDirection(new Vector3(1, 0, 0));
                    pos = child.transform.TransformPoint(new Vector3(0.1f, 0.03f, 0.03f));

                    if (Physics.Raycast(pos, fwd, out hit) && sM && 0 == 0 && !sM.protect)
                    {
                        if (hit.distance > ((sM.blockWidth / 3f) - 1f))
                        {
                            var streetSize = sM.blockWidth - sM.sectionWidth;
                            sM.blockWidth = Mathf.CeilToInt(hit.distance / 3f);
                            sM.sectionWidth = sM.blockWidth - streetSize;
                            //   Debug.Log(hit.distance);
                            sM.AwakeCity();
                            sM.ModifyStreet();
                        }
                    }
                    ////////////Use Skewing

                    fwd = child.transform.TransformDirection(new Vector3(1, 0, 0));
                    pos = child.transform.TransformPoint(new Vector3(0.1f, 0.03f, 0.03f));


                    if (Physics.Raycast(pos, fwd, out hit) && sM && 0 == 0 && !sM.protect)
                    {
                        var hitSM = hit.transform.GetComponent<StreetModifier>();
                        if (!sM.useSkewLR && !hitSM.useSkewLR && hitSM.blockDepth == sM.blockDepth && hitSM.streetType != StreetModifier.CScapeStreetType.River && !sM.protect)
                        {
                            var rand = Mathf.FloorToInt(Random.Range(-(sM.blockWidth / 3), sM.blockWidth / 3));
                            sM.useSkewLR = true;
                            hitSM.useSkewLR = true;
                            sM.useSkewFB = false;
                            hitSM.useSkewFB = false;
                            sM.slitRB = rand;
                            hitSM.slitLF = rand;
                            //   Debug.Log(hit.distance);
                        }
                    }



                }

                foreach (var child in tempListst)
                {
                    RaycastHit hit;
                    var fwd = child.transform.TransformDirection(new Vector3(0, 0, 1));
                    var pos = child.transform.TransformPoint(new Vector3(0.03f, 0.03f, 0.1f));
                    var sM = child.GetComponent<StreetModifier>();
                    if (Physics.Raycast(pos, fwd, out hit) && sM && 0 == 0)
                    {
                        var hitSM = hit.transform.GetComponent<StreetModifier>();
                        if (!sM.useSkewLR && !hitSM.useSkewLR && !sM.useSkewFB && !hitSM.useSkewFB && hitSM.blockWidth == sM.blockWidth && hitSM.streetType != StreetModifier.CScapeStreetType.River && !sM.protect)
                        {
                            
                            var rand = Mathf.FloorToInt(Random.Range (-( sM.blockDepth / 3), Mathf.Min (( sM.blockDepth / 3), 30)));
                            sM.useSkewFB = true;
                            hitSM.useSkewFB = true;
                            sM.slitRB = rand;
                            hitSM.slitLF = rand;
                            //   Debug.Log(hit.distance);
                        }
                    }
                }

                foreach (var child in tempListst)
                {
                    iteration = iteration + 1;
                    var SM = child.GetComponent<StreetModifier>();
                    if (SM)
                    {
                        if (SM.streetType == StreetModifier.CScapeStreetType.Street)
                       // SM.meshOriginal = streetPrefabs[1].GetComponent<StreetModifier>().meshOriginal;
                        SM.cityRandomizerParent = this;
                        SM.averageBuildingSizeMin = 1;
                        SM.averageBuildingSizeMax = 10;
                        SM.randomSeed = iteration;
                        SM.districtStyle = buildingStyles[Random.Range(0, buildingStyles.Length)];
                        SM.AwakeCity();
                       // SM.GenerateBuildings();
                    }
                }
            }

            return iteration;
        }

        private int Quadtree(int iteration)
        {
            {
                foreach (Transform go in streets.transform.Cast<Transform>().Reverse())
                {
                    if (Random.Range(0, 5) < 1 && go.gameObject.GetComponent<StreetModifier>())
                    {
                        if (!go.gameObject.GetComponent<StreetModifier>().protect)
                            go.gameObject.GetComponent<StreetModifier>().markToDelete = true;
                    }

                }

                ///Delete random 
                var tempList = streets.transform.Cast<Transform>().Reverse().ToList();
                foreach (var child in tempList)
                {
                    if (child.gameObject.GetComponent<StreetModifier>() && child.gameObject.GetComponent<StreetModifier>().markToDelete)
                    {
                        RaycastHit hit;
                        var sM = child.GetComponent<StreetModifier>();

                        if (Physics.Raycast(child.transform.TransformPoint(new Vector3(0.03f, 0.03f, 0.1f)), child.transform.TransformDirection(new Vector3(0, 0, 1)), out hit) && sM && 0 == 0)
                        {
                            if (hit.transform.GetComponent<StreetModifier>())
                            {
                                if (hit.transform.GetComponent<StreetModifier>() && hit.transform.GetComponent<StreetModifier>().streetType != StreetModifier.CScapeStreetType.River)
                                {

                                    if (!sM.protect) DestroyImmediate(child.gameObject);
                                }

                            }
                        }

                    }
                }
                //Search Extensions
                var tempListst = streets.transform.Cast<Transform>().Reverse().ToList();
                foreach (var child in tempListst)
                {
                    RaycastHit hit;
                    Vector3 fwd = child.transform.TransformDirection(new Vector3(0, 0, 1));
                    Vector3 pos = child.transform.TransformPoint(new Vector3(0.03f, 0.03f, 0.1f));
                    var sM = child.GetComponent<StreetModifier>();

                    if (Physics.Raycast(pos, fwd, out hit) && sM && 0 == 0 && !sM.protect)
                    {
                        if (hit.distance > ((sM.blockDepth / 3f) - 1f))
                        {
                            var streetSize = sM.blockDepth - sM.sectionDepth;
                            sM.blockDepth = Mathf.CeilToInt(hit.distance / 3f);
                            sM.sectionDepth = sM.blockDepth - streetSize;
                            sM.AwakeCity();
                            sM.ModifyStreet();
                        }
                    }
                    fwd = child.transform.TransformDirection(new Vector3(1, 0, 0));
                    pos = child.transform.TransformPoint(new Vector3(0.1f, 0.03f, 0.03f));

                    if (Physics.Raycast(pos, fwd, out hit) && sM && 0 == 0 && !sM.protect)
                    {
                        if (hit.distance > ((sM.blockWidth / 3f) - 1f))
                        {
                            var streetSize = sM.blockWidth - sM.sectionWidth;
                            sM.blockWidth = Mathf.CeilToInt(hit.distance / 3f);
                            sM.sectionWidth = sM.blockWidth - streetSize;
                            //   Debug.Log(hit.distance);
                            sM.AwakeCity();
                            sM.ModifyStreet();
                        }
                    }
                    ////////////Use Skewing

                    fwd = child.transform.TransformDirection(new Vector3(1, 0, 0));
                    pos = child.transform.TransformPoint(new Vector3(0.1f, 0.03f, 0.03f));






                }

                foreach (var child in tempListst)
                {
                    RaycastHit hit;
                    var fwd = child.transform.TransformDirection(new Vector3(0, 0, 1));
                    var pos = child.transform.TransformPoint(new Vector3(0.03f, 0.03f, 0.1f));
                    var sM = child.GetComponent<StreetModifier>();
    
                }

                foreach (var child in tempListst)
                {
                    iteration = iteration + 1;
                    var SM = child.GetComponent<StreetModifier>();
                    if (SM)
                    {
                        SM.cityRandomizerParent = this;
                        SM.averageBuildingSizeMin = 1;
                        SM.averageBuildingSizeMax = 8;
                        SM.randomSeed = iteration;
                        SM.districtStyle = buildingStyles[Random.Range(0, buildingStyles.Length)];
                        SM.AwakeCity();
                       // SM.GenerateBuildings();
                    }
                }
            }

            return iteration;
        }

        public void Refresh()
        {
            Component[] amodif = buildings.GetComponentsInChildren<CSArray>();
            //foreach (CSArray x in amodif)
            //{
            //    x.AwakeMe();
            //    //  x.UpdateCity();

            //}

            Component[] bmodif = buildings.GetComponentsInChildren<BuildingModifier>();

            foreach (BuildingModifier x in bmodif)
            {
                x.AwakeCity();
                x.UpdateCity();

            }


            Component[] smodif = streets.GetComponentsInChildren<StreetModifier>();
            foreach (StreetModifier x in smodif)
            {
                x.AwakeCity();
                x.UpdateCity();
            }

            Component[] sdmodif = streetDetails.GetComponentsInChildren<CSInstantiator>();
            foreach (CSInstantiator x in sdmodif)
            {
                x.UpdateElements();
            }
        }

        public void UpdateHeights()
        {
            // Random.InitState(randomSeed);
            // float curveVal = cityCurve.Evaluate(0f);

            foreach (Transform go in buildings.transform.Cast<Transform>().Reverse())
            {
                BuildingModifier building = go.GetComponent(typeof(BuildingModifier)) as BuildingModifier;
                float dist = Vector3.Distance(cityCenterObject.transform.position, go.position);
                //       int rnd = Random.Range(0, nightColors.Length - 1);
                building.floorNumber = Random.Range(minFloors, maxFloors + Mathf.CeilToInt(cityCurve.Evaluate(dist / (blockDistances * 3f * numberOfBuildingsX / 2)) * 200f));

            }
        }
        public void OptimizeReflectionProbes()
        {
            foreach (Transform go in buildings.transform.Cast<Transform>().Reverse())
            {
                MeshRenderer csMesh = go.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
                csMesh.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Simple;
#if UNITY_EDITOR
          //      UnityEditor.StaticOcclusionCulling.Compute();
#endif
            }

        }
        public void GenerateSlantedBuildings()
        {
            
            foreach (Transform go in streets.transform.Cast<Transform>().Reverse())
            {
                StreetModifier csInstantiator = go.GetComponent(typeof(StreetModifier)) as StreetModifier;
                if (csInstantiator !=null)
                csInstantiator.GenerateBuildings();
            }

        }

        public void StripScripts()
        {
            foreach (Transform go in streetDetails.transform.Cast<Transform>().Reverse())
            {
                CSInstantiator csInstantiator = go.GetComponent(typeof(CSInstantiator)) as CSInstantiator;
                DestroyImmediate(csInstantiator);
            }

            foreach (Transform go in streets.transform.Cast<Transform>().Reverse())
            {
                StreetModifier csInstantiator = go.GetComponent(typeof(StreetModifier)) as StreetModifier;
                DestroyImmediate(csInstantiator);
            }

            foreach (Transform go in streetLights.transform.Cast<Transform>().Reverse())
            {
                CSInstantiatorLights csInstantiator = go.GetComponent(typeof(CSInstantiatorLights)) as CSInstantiatorLights;
                DestroyImmediate(csInstantiator);
            }

            foreach (Transform go in buildings.transform.Cast<Transform>().Reverse())
            {
                CSAdvertising csInstantiator = go.GetComponent(typeof(CSAdvertising)) as CSAdvertising;
                DestroyImmediate(csInstantiator);
            }

            foreach (Transform go in buildings.transform.Cast<Transform>().Reverse())
            {
                CSRooftops csInstantiator = go.GetComponent(typeof(CSRooftops)) as CSRooftops;
                DestroyImmediate(csInstantiator);
            }

            foreach (Transform go in buildings.transform.Cast<Transform>().Reverse())
            {
                CSArray csInstantiator = go.GetComponent(typeof(CSArray)) as CSArray;
                DestroyImmediate(csInstantiator);
            }

            foreach (Transform go in buildings.transform.Cast<Transform>().Reverse())
            {
                BuildingModifier csInstantiator = go.GetComponent(typeof(BuildingModifier)) as BuildingModifier;
                DestroyImmediate(csInstantiator);
            }



        }






        public void Randomize()
        {
            Random.InitState(randomSeed);

            BuildingModifier[] buildingModifierArray = buildings.GetComponentsInChildren<BuildingModifier>();
            int init = 0;

            //    foreach (Transform go in buildings.transform.Cast<Transform>().Reverse())
            //{
            //    BuildingModifier building = go.GetComponent(typeof(BuildingModifier)) as BuildingModifier;
            //for (int i = 0; i < buildingModifierArray.Length; i++)
            //{
            foreach (BuildingModifier scripts in buildingModifierArray)
            {
                init = init + 1;
                Random.InitState(randomSeed + init);
                //  Debug.Log(scripts.transform.name + Random.Range(0, 9));
                float dist = Vector3.Distance(cityCenterObject.transform.position, scripts.gameObject.transform.position);
                int rnd = Random.Range(0, nightColors.Length - 1);
                scripts.colorVariation.x = Random.Range(0, 9);
                scripts.colorVariation.y = Mathf.Floor(Random.Range(0,10));
                scripts.colorVariation.z = Mathf.Floor(Random.Range(0, 10));
                scripts.colorVariation.w = Mathf.Floor(Random.Range(0, 10));

                scripts.colorVariation2.x = Random.Range(0, 9);
                scripts.colorVariation2.y = Random.Range(0, 9);
                scripts.colorVariation2.z = Random.Range(0, 9);
                scripts.colorVariation2.w = Random.Range(2, 9);

                scripts.colorVariation3.x = Random.Range(0, 10);
                scripts.colorVariation3.y = Random.Range(0, 10);
                scripts.colorVariation3.z = Random.Range(0, 10);
                scripts.colorVariation3.w = Random.Range(0, 10);

                scripts.colorVariation4.x = Random.Range(0, 10);
                scripts.colorVariation4.y = Random.Range(0, 10);
                scripts.colorVariation4.z = Random.Range(0, 10);
                scripts.colorVariation4.w = Random.Range(0, 10);
                scripts.lightnessFront = Random.Range(0, 10);
                scripts.lightnessSide = Random.Range(0, 10);
                scripts.colorVariation5.x = Random.Range(0, 10);
                scripts.colorVariation5.y = Random.Range(0, 10);
                scripts.borderCol = Random.Range(0, 10);
                scripts.lightsOnOff = Random.Range(0, 10);
                scripts.scale = Random.Range(minAdittionalScale, maxAdittionalScale);
                scripts.rooftopID = Random.Range(0, 10);
                scripts.lightsOnOff2 = Random.Range(0, 10);
                int customBool = Random.Range(0, 2);
                if (customBool == 0) scripts.customBool2 = true;
                else scripts.customBool2 = false;
                customBool = Random.Range(0, 5);
                if (customBool == 0)
                    scripts.useGraffiti = true;



                if (height)
                    scripts.floorNumber = Random.Range(minFloors, maxFloors + Mathf.CeilToInt(cityCurve.Evaluate(dist / (blockDistances * 3f * numberOfBuildingsX / 2)) * 200f));
                //        if (depth)
                //            building.buildingDepth = Random.Range(minDepth, maxDepth);
                //        if (width)
                //            building.buildingWidth = Random.Range(minWidth, maxWidth);
                scripts.uniqueMapping = Random.Range(-160, 160);
                if (faccadeStyles)
                {
                    scripts.colorVariation2.y = Random.Range(0, 9);
                    scripts.colorVariation2.z = Random.Range(0, 9);
                    scripts.materialId1 = 21;
                    scripts.materialId2 = Random.Range(minMatIndex1, maxMatIndex1);
                    scripts.materialId3 = Random.Range(minMatIndex2, maxMatIndex2);
                    scripts.materialId4 = Random.Range(minMatIndex4, maxMatIndex4);
                    scripts.materialId5 = Random.Range(0, 22);
                }
                if (openWindow)
                    scripts.windowOpen = Random.Range(minWindowOpen, maxWindowOpen);
                if (rndPatternHorizontal)
                    scripts.pattern = Random.Range(0f, 1f);
                if (rndColor)
                {
                    scripts.UpdateCity();
                }



                //  buildingModifierArray[i].AwakeCity();
                //  buildingModifierArray[i].UpdateCity();
            }

            CSAdvertising[] advertisings = buildings.GetComponentsInChildren<CSAdvertising>();
            int initAdv = 0;
            foreach (CSAdvertising scripts in advertisings)
            {
                init = init + 1;
                Random.InitState(randomSeed + init);
                scripts.randomSeed = randomSeed + init;
                scripts.instancesX = Random.Range(50, 150);

            }

            Refresh();

        }


    }
}

