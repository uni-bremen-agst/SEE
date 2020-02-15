using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using CScape;
using UnityEngine.Rendering;
//using UnityEditor;

namespace CScape
{

    [CustomEditor(typeof(CityRandomizer))]
    [CanEditMultipleObjects]

    public class CityRandomizerEditor : Editor
    {
        private Texture banner;
        public bool configurePrefab = false;
        public bool randomSettings;
        public bool streetLayout;
        public bool buildingTemplates;
        public bool rooftopTemplates;
        public bool streetTemplates;
        public bool streetDetailTemplates;
        public bool busStopTemplate;
        public bool streetLightsTemplates;
        public bool streetFoliageTemplates;
        public bool folliageTemplates;
        public bool scanPrefabs;
        public bool citySize;
        public bool collidersSetup;
        bool checkConfiguration = false;
        bool realtimeHeights = true;
        bool optimizeMobile = false;


        void OnEnable()
        {
          //  CityRandomizer ce = (CityRandomizer)target;
            banner = Resources.Load("CSHeader") as Texture;
            if (System.IO.File.Exists(Application.dataPath + "/CSconfigured.txt")) checkConfiguration = true;
            

            }

        public override void OnInspectorGUI()
        {
            CityRandomizer ce = (CityRandomizer)target;
            GUILayout.Box(banner, GUILayout.ExpandWidth(true));
            if (PlayerSettings.colorSpace == ColorSpace.Gamma)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("WARNING! For best visual appearance");
                GUILayout.Label("switch Your project to Linear Color Space");
                if (GUILayout.Button("OK! Switch!"))
                {
                    PlayerSettings.colorSpace = ColorSpace.Linear;
                    PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel19;
                    EditorApplication.ExecuteMenuItem("Edit/Graphics Emulation/No Emulation");
                }
                GUILayout.EndVertical();
            }
            if (Lightmapping.bakedGI || Lightmapping.realtimeGI) { 
            GUILayout.BeginVertical("box");
            GUILayout.Label("WARNING! For workflow performance");
            GUILayout.Label("turn off lightmap baking");
            if (GUILayout.Button("Turn off lightmap baking"))
            {
                if (Lightmapping.bakedGI) Lightmapping.bakedGI = false;
                if (Lightmapping.realtimeGI) Lightmapping.realtimeGI = false;
            }
            GUILayout.EndVertical();
            }

            if (!checkConfiguration)
            {



                GUILayout.BeginVertical("box");
                GUILayout.Label("WARNING! Set Graphics API build settings:");
                GUILayout.Label("1. Android -  Graphics API change to OpenGLES3.0");
                GUILayout.Label("2. iOS -  Graphics API change to Metal");
                GUILayout.Label("3. Win Standalone -  Graphics API change to DX11");
                GUILayout.Label("4. MacOS -  Graphics API change to OpenGLCore");

                if (GUILayout.Button("Set Build Settings"))
                {
                    GraphicsDeviceType[] apis = { GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Direct3D12 };
                    GraphicsDeviceType[] apisIOS = { GraphicsDeviceType.Metal };
                    GraphicsDeviceType[] apisAndroid = { GraphicsDeviceType.OpenGLES3 };
                    GraphicsDeviceType[] apisMacOS = { GraphicsDeviceType.OpenGLCore };
                    PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows, false);
                    PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows, apis);

                    PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                    PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, apisAndroid);

                    PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
                    PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, apisIOS);

                    PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSXIntel64, false);
                    PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSXIntel64, apisMacOS);

                    PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSXIntel, false);
                    PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSXIntel, apisMacOS);
                    writeConfToFile();
                    checkConfiguration = true;
                }
                GUILayout.EndVertical();


            }
            GUILayout.BeginVertical("box");
            randomSettings = EditorGUILayout.Foldout(randomSettings, new GUIContent("Random settings", "Here you can set random generation values for buildings and building surfaces."), true);
            if (randomSettings)
            {
                GUILayout.BeginHorizontal();
                ce.height = EditorGUILayout.Toggle(ce.height);
                ce.minFloors = EditorGUILayout.IntField(new GUIContent("Floors", "Minimum and Maximum floor number when generating buildings. This isn't a precize value as it's influenced by city center object and minimum possible building size (some building shapes have a minimum possible size. "), ce.minFloors);
                ce.maxFloors = EditorGUILayout.IntField("", ce.maxFloors);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                ce.openWindow = EditorGUILayout.Toggle(ce.openWindow);
                ce.minAdittionalScale = EditorGUILayout.FloatField(new GUIContent("Adittional Floor Scale", "Adittional floor height scale, makes floors smaller or higher, helps on getting more realistic adjecent buildings "), ce.minAdittionalScale);
                ce.maxAdittionalScale = EditorGUILayout.FloatField("", ce.maxAdittionalScale);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                ce.width = EditorGUILayout.Toggle(ce.width);
                ce.minWidth = EditorGUILayout.IntField(new GUIContent("Width", "Minimum and Maximum building width when generating buildings. This isn't always used for calculation and can be overriden by other settings"), ce.minWidth);
                ce.maxWidth = EditorGUILayout.IntField("", ce.maxWidth);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                ce.depth = EditorGUILayout.Toggle(ce.depth);
                ce.minDepth = EditorGUILayout.IntField(new GUIContent("Depth", "Minimum and Maximum building Depth when generating buildings. This isn't always used for calculation and can be overriden by other settings"), ce.minDepth);
                ce.maxDepth = EditorGUILayout.IntField("", ce.maxDepth);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                ce.faccadeStyles = EditorGUILayout.Toggle(ce.faccadeStyles);
                ce.minMatIndex = EditorGUILayout.IntField(new GUIContent("Mat Index1", "Controls material assignements. There is no need to change material indexes as long as you don't change textures"), ce.minMatIndex);
                ce.maxMatIndex = EditorGUILayout.IntField("", ce.maxMatIndex);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                ce.faccadeStyles = EditorGUILayout.Toggle(ce.faccadeStyles);
                ce.minMatIndex1 = EditorGUILayout.IntField("Mat Index2", ce.minMatIndex1);
                ce.maxMatIndex1 = EditorGUILayout.IntField("", ce.maxMatIndex1);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                ce.faccadeStyles = EditorGUILayout.Toggle(ce.faccadeStyles);
                ce.minMatIndex2 = EditorGUILayout.IntField("Mat Index3", ce.minMatIndex2);
                ce.maxMatIndex2 = EditorGUILayout.IntField("", ce.maxMatIndex2);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                ce.faccadeStyles = EditorGUILayout.Toggle(ce.faccadeStyles);
                ce.minMatIndex4 = EditorGUILayout.IntField("Mat Index4", ce.minMatIndex4);
                ce.maxMatIndex4 = EditorGUILayout.IntField("", ce.maxMatIndex4);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                ce.openWindow = EditorGUILayout.Toggle(ce.openWindow);
                ce.minWindowOpen = EditorGUILayout.FloatField(new GUIContent("Window open", "Minimum and Maximum window blinds opening"), Mathf.Ceil(ce.minWindowOpen));
                ce.maxWindowOpen = EditorGUILayout.FloatField("", Mathf.Ceil(ce.maxWindowOpen));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                ce.openWindow = EditorGUILayout.Toggle(ce.openWindow);
                ce.streetSizeX = EditorGUILayout.IntField(new GUIContent("Street Size X", "Controls Min and Maximum street widths (lane numbers for each street). Lane width is 3 meters wide. "), ce.streetSizeX);
                ce.streetSizeXmax = EditorGUILayout.IntField("", ce.streetSizeXmax);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                ce.openWindow = EditorGUILayout.Toggle(ce.openWindow);
                ce.streetSizeZ = EditorGUILayout.IntField("Street Size Z", ce.streetSizeZ);
                ce.streetSizeZmax = EditorGUILayout.IntField("", ce.streetSizeZmax);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                ce.openWindow = EditorGUILayout.Toggle(ce.openWindow);
                ce.sidewalkSizeX = EditorGUILayout.IntField(new GUIContent("Sidewalk Size X", "Controls minimum and maximum sidewalk size - in CS units (multiply this value by 3 meters)"), ce.sidewalkSizeX);
                ce.sidewalkSizeXmax = EditorGUILayout.IntField("", ce.sidewalkSizeXmax);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                ce.openWindow = EditorGUILayout.Toggle(ce.openWindow);
                ce.sidewalkSizeZ = EditorGUILayout.IntField("Sidewalk Size Z", ce.sidewalkSizeZ);
                ce.sidewalkSizeZmax = EditorGUILayout.IntField("", ce.sidewalkSizeZmax);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                ce.openWindow = EditorGUILayout.Toggle(ce.openWindow);
                ce.folliageThreshold = EditorGUILayout.IntField(new GUIContent("Folliage Thresshold", "Trees will be placed only at sidewalks that are wider that this value. Note: folliage is expensive, always try to minimize it's usage"), ce.folliageThreshold);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                ce.sectionDivisionMin = EditorGUILayout.IntField(new GUIContent("Section Division Min/Max", "How many buildings can fit one single city block (block is an area between two streets)"), ce.sectionDivisionMin);
                ce.sectionDivisionMax = EditorGUILayout.IntField(ce.sectionDivisionMax);
                GUILayout.EndHorizontal();

            }


            GUILayout.EndVertical();

            ///city size layout
            GUILayout.BeginVertical("box");
            citySize = EditorGUILayout.Foldout(citySize, new GUIContent("City Size", "Control city size from this foldout"), true);
            if (citySize)
            {
                GUILayout.BeginVertical();
                ce.numberOfBuildingsX = EditorGUILayout.IntField(new GUIContent("Number of blocks X", "Number of blocks to be generated on X axes. (block is an area between two streets)"), ce.numberOfBuildingsX);
                ce.numberOfBuildingsZ = EditorGUILayout.IntField(new GUIContent("Number of blocks Z", "Number of blocks to be generated on Z axes. (block is an area between two streets)"), ce.numberOfBuildingsZ);
                ce.blockDistances = EditorGUILayout.IntField(new GUIContent("Min Block distances", "Distance between blocks. This can be also defined as block size value"), ce.blockDistances);
                ce.maxBlockDistances = EditorGUILayout.IntField("Max Block distances", ce.maxBlockDistances);
                ce.riverPosition = EditorGUILayout.IntField(new GUIContent("River Position", "Position of the river in a city. It will be generated by replacing blocks at a given step. To center it to a scene, use Center River buttn before generating city"), ce.riverPosition);
                if (GUILayout.Button("Center river"))
                {
                    ce.riverPosition = ce.numberOfBuildingsX / 2;
                }

                EditorGUI.BeginChangeCheck();
                ce.cityCurve = EditorGUILayout.CurveField(new GUIContent("Heights", "Control building heights with a curve by distance from a CityCenter object. If a city is generated, this curve updates city in realtime"), ce.cityCurve);
                if (EditorGUI.EndChangeCheck() && realtimeHeights)
                {
                    ce.UpdateHeights();
                    ce.Refresh();
                }
                realtimeHeights = EditorGUILayout.Toggle("Calculate Heights in Realtime", realtimeHeights);
                if (!realtimeHeights) {
                    if (GUILayout.Button("Update Heights"))
                    {
                        ce.UpdateHeights();
                        ce.Refresh();
                    }
                }
                //  ce.cityCurve = EditorGUILayout.CurveField("Heights", ce.cityCurve);


                GUILayout.EndVertical();

            }


            GUILayout.EndVertical();

            //street layout
            //GUILayout.BeginVertical("Box");
            //streetLayout = EditorGUILayout.Foldout(streetLayout, new GUIContent("Night Lights", "This controls random night lighting colour of the buildings. Note: always keep some black colours in this array - otherwise all building will be illuminated - and this isn't pleasing or realistic"), true);
            //if (streetLayout)
            //{
            //    GUILayout.BeginVertical("Box");
            //    for (int i = 0; i < ce.nightColors.Length; i++)
            //    {
            //        ce.nightColors[i] = EditorGUILayout.ColorField("Col " + i, ce.nightColors[i]);
            //    }
            //    GUILayout.BeginHorizontal("Box");
            //    if (GUILayout.Button("-", "Label", GUILayout.Width(20), GUILayout.Height(15)))
            //        System.Array.Resize(ref ce.nightColors, ce.nightColors.Length - 1);
            //    if (GUILayout.Button("+", "Label", GUILayout.Width(20), GUILayout.Height(15)))
            //        System.Array.Resize(ref ce.nightColors, ce.nightColors.Length + 1);
            //    GUILayout.EndHorizontal();

            //    GUILayout.EndVertical();

            //}


            //GUILayout.EndVertical();


            ///choose possible prefab building tops


            ///choose possible prefab buildings
            GUILayout.BeginVertical("Box");
            buildingTemplates = EditorGUILayout.Foldout(buildingTemplates, new GUIContent("Building templates", "Building templates to use. Building templates are found in Assets/CScape/Editor/Resources/BuildingTemplates/Buildings folder"), false);
            //if (buildingTemplates)
            //{
            //    GUILayout.BeginVertical();
            //    for (int i = 0; i < ce.prefabs.Length; i++)
            //    {
            //        ce.prefabs[i] = EditorGUILayout.ObjectField("Template " + i, ce.prefabs[i], typeof(GameObject), true) as GameObject;
            //    }
            //    GUILayout.BeginHorizontal("Box");
            //    if (GUILayout.Button("-", "Label", GUILayout.Width(20), GUILayout.Height(15)))
            //        System.Array.Resize(ref ce.prefabs, ce.prefabs.Length - 1);
            //    if (GUILayout.Button("+", "Label", GUILayout.Width(20), GUILayout.Height(15)))
            //        System.Array.Resize(ref ce.prefabs, ce.prefabs.Length + 1);
            //    GUILayout.EndHorizontal();
            //    //if (GUILayout.Button("Load All Templates"))
            //    //{
            //    //    ce.prefabs = Resources.LoadAll<GameObject>("BuildingTemplates/Buildings");
            //    //}
            //    GUILayout.EndVertical();

            //}
            for (int i = 0; i < ce.buildingStyles.Length; i++)
            {
                ce.buildingStyles[i] = EditorGUILayout.ObjectField("Template ", ce.buildingStyles[i], typeof(DistrictStyle), true) as DistrictStyle;
            }
            GUILayout.BeginHorizontal("Box");
            if (GUILayout.Button("-", "Label", GUILayout.Width(20), GUILayout.Height(15)))
                System.Array.Resize(ref ce.buildingStyles, ce.buildingStyles.Length - 1);
            if (GUILayout.Button("+", "Label", GUILayout.Width(20), GUILayout.Height(15)))
                System.Array.Resize(ref ce.buildingStyles, ce.buildingStyles.Length + 1);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            //Choose Possible streets
            GUILayout.BeginVertical("Box");
            streetTemplates = EditorGUILayout.Foldout(streetTemplates, "Street templates", false);
            if (streetTemplates)
            {
                GUILayout.BeginVertical();
                for (int i = 0; i < ce.streetPrefabs.Length; i++)
                {
                    ce.streetPrefabs[i] = EditorGUILayout.ObjectField("Template " + i, ce.streetPrefabs[i], typeof(GameObject), true) as GameObject;
                }
                GUILayout.BeginHorizontal("Box");
                if (GUILayout.Button("-", "Label", GUILayout.Width(20), GUILayout.Height(15)))
                    System.Array.Resize(ref ce.streetPrefabs, ce.streetPrefabs.Length - 1);
                if (GUILayout.Button("+", "Label", GUILayout.Width(20), GUILayout.Height(15)))
                    System.Array.Resize(ref ce.streetPrefabs, ce.streetPrefabs.Length + 1);
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Load Templates"))
                {
                    ce.streetPrefabs = Resources.LoadAll<GameObject>("BuildingTemplates/Streets");
                    //DirectoryInfo dir = new DirectoryInfo("Assets/CScape/BuildingTemplates/Streets");
                    //FileInfo[] info = dir.GetFiles("*.prefab");
                    //for (int j = 0; j < info.Length; j++)
                    //{
                    //    System.Array.Resize(ref ce.streetPrefabs, info.Length);
                    //    Debug.Log(info[j] + "");
                    //}
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();

            //Choose Possible street details
            GUILayout.BeginVertical("Box");
            streetDetailTemplates = EditorGUILayout.Foldout(streetDetailTemplates, "Street Detail templates", false);
            if (streetDetailTemplates)
            {
                GUILayout.BeginVertical();
                for (int i = 0; i < ce.streetDetailPrefabs.Length; i++)
                {
                    ce.streetDetailPrefabs[i] = EditorGUILayout.ObjectField("Template " + i, ce.streetDetailPrefabs[i], typeof(GameObject), true) as GameObject;
                }
                GUILayout.BeginHorizontal("Box");
                if (GUILayout.Button("-", "Label", GUILayout.Width(20), GUILayout.Height(15)))
                    System.Array.Resize(ref ce.streetDetailPrefabs, ce.streetDetailPrefabs.Length - 1);
                if (GUILayout.Button("+", "Label", GUILayout.Width(20), GUILayout.Height(15)))
                    System.Array.Resize(ref ce.streetDetailPrefabs, ce.streetDetailPrefabs.Length + 1);
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Scan Templates"))
                {

                    DirectoryInfo dir = new DirectoryInfo("Assets/CScape/BuildingTemplates/Details");
                    FileInfo[] info = dir.GetFiles("*.prefab");
                    for (int j = 0; j < info.Length; j++)
                    {
                        System.Array.Resize(ref ce.streetDetailPrefabs, info.Length);
                        Debug.Log(info[j] + "");
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();

            //Choose Possible street details
            GUILayout.BeginVertical("Box");
            busStopTemplate = EditorGUILayout.Foldout(busStopTemplate, "Bus Stop", false);
            if (busStopTemplate)
            {
                GUILayout.BeginVertical();

                ce.busStopPrefab = EditorGUILayout.ObjectField("Bus Stop ", ce.busStopPrefab, typeof(GameObject), true) as GameObject;

                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();



            //Choose possible light templates
            GUILayout.BeginVertical("Box");
            streetLightsTemplates = EditorGUILayout.Foldout(streetLightsTemplates, "Street Light templates", false);
            if (streetLightsTemplates)
            {
                GUILayout.BeginVertical();
                for (int i = 0; i < ce.streetLightsPrefabs.Length; i++)
                {
                    ce.streetLightsPrefabs[i] = EditorGUILayout.ObjectField("Template " + i, ce.streetLightsPrefabs[i], typeof(GameObject), true) as GameObject;
                }
                GUILayout.BeginHorizontal("Box");
                if (GUILayout.Button("-", "Label", GUILayout.Width(20), GUILayout.Height(15)))
                    System.Array.Resize(ref ce.streetLightsPrefabs, ce.streetLightsPrefabs.Length - 1);
                if (GUILayout.Button("+", "Label", GUILayout.Width(20), GUILayout.Height(15)))
                    System.Array.Resize(ref ce.streetLightsPrefabs, ce.streetLightsPrefabs.Length + 1);
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Scan Templates"))
                {

                    DirectoryInfo dir = new DirectoryInfo("Assets/CScape/BuildingTemplates/Details");
                    FileInfo[] info = dir.GetFiles("*.prefab");
                    for (int j = 0; j < info.Length; j++)
                    {
                        System.Array.Resize(ref ce.streetLightsPrefabs, info.Length);
                        Debug.Log(info[j] + "");
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
            ///Choose possible folliage
            GUILayout.BeginVertical("Box");
            streetFoliageTemplates = EditorGUILayout.Foldout(streetFoliageTemplates, "Street Folliage templates", false);
            if (streetFoliageTemplates)
            {
                GUILayout.BeginVertical();
                for (int i = 0; i < ce.streetFoliagePrefabs.Length; i++)
                {
                    ce.streetFoliagePrefabs[i] = EditorGUILayout.ObjectField("Template " + i, ce.streetFoliagePrefabs[i], typeof(GameObject), true) as GameObject;
                }
                GUILayout.BeginHorizontal("Box");
                if (GUILayout.Button("-", "Label", GUILayout.Width(20), GUILayout.Height(15)))
                    System.Array.Resize(ref ce.streetFoliagePrefabs, ce.streetFoliagePrefabs.Length - 1);
                if (GUILayout.Button("+", "Label", GUILayout.Width(20), GUILayout.Height(15)))
                    System.Array.Resize(ref ce.streetFoliagePrefabs, ce.streetFoliagePrefabs.Length + 1);
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Scan Templates"))
                {

                    DirectoryInfo dir = new DirectoryInfo("Assets/CScape/BuildingTemplates/Details");
                    FileInfo[] info = dir.GetFiles("*.prefab");
                    for (int j = 0; j < info.Length; j++)
                    {
                        System.Array.Resize(ref ce.streetFoliagePrefabs, info.Length);
                        Debug.Log(info[j] + "");
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();





            ce.randomSeed = EditorGUILayout.IntField("Random seed", ce.randomSeed);


            if (ce.quadtreeSkew || ce.quadtree)
            {

                if (GUILayout.Button("1) Generate Street Layout"))
                {
                    ce.delete = true;
                    ce.DeleteBusStops();
                    ce.UpdateCity();
                    ce.GenerateStreets();

                }

                if (GUILayout.Button("2) Generate buildings"))
                {
                    
                    ce.GenerateSlantedBuildings();

                }

                if (GUILayout.Button("3) Add Street Lightpoles"))
                {
                    ce.DeleteStreeetDetails();
                    ce.GenerateDetails();
                    //    ce.UpdateCity();
                }

                if (GUILayout.Button("4) Add Bus Stops"))
                {
                    ce.DeleteBusStops();
                    ce.GenerateBusStops();
                    //    ce.UpdateCity();
                }

                if (GUILayout.Button("5) Add Street Lights"))
                {
                    ce.DeleteLights();
                    ce.GenerateLights();
                    //  ce.UpdateCity();
                }

                if (GUILayout.Button("Add Foliage"))
                {
                    ce.DeleteFolliage();
                    ce.GenerateFolliage();
                    //     ce.UpdateCity();
                }

               
            }

            else
            {
                if (GUILayout.Button("Generate City"))
                {

                    ce.Generate();
                    ce.GenerateStreets();
                    ce.UpdateCity();
                }
                if (GUILayout.Button("Generate Streets"))
                {
                    ce.DeleteStreets();
                    ce.GenerateStreets();

                }

                if (GUILayout.Button("Add Street Lightpoles"))
                {
                    ce.DeleteStreeetDetails();
                    ce.GenerateDetails();
                    //    ce.UpdateCity();
                }

                if (GUILayout.Button("Add Bus Stops"))
                {
                    ce.DeleteBusStops();
                    ce.GenerateBusStops();
                    //    ce.UpdateCity();
                }



                if (GUILayout.Button("Add Foliage"))
                {
                    ce.DeleteFolliage();
                    ce.GenerateFolliage();
                    //     ce.UpdateCity();
                }
            }
            if (GUILayout.Button("Delete Generated City"))
            {
                ce.delete = true;
                ce.UpdateCity();
            }

            if (GUILayout.Button("Delete All except Streets"))
            {
                ce.keepStreets = true;
                ce.delete = true;
                ce.UpdateCity();
            }

            if (GUILayout.Button("Add Street Lights"))
            {
                ce.DeleteLights();
                ce.GenerateLights();
                //  ce.UpdateCity();
            }

            if (GUILayout.Button(new GUIContent("Strip CS Scripts", "This will delete all CS related scripts from your object. It can be used before exporting final level. USE IT WITH CAUTION AS YOU WILL LOOSE EIDITNG POSSIBILITY")))
            {
                ce.StripScripts();
            }

            if (GUILayout.Button(new GUIContent("Refresh City", "Use this button to recreate whole city from stored parameters. Can be used when you update your city from an older CScape version.")))
            {
                ce.Refresh();


            }

            if (GUILayout.Button("Optimize Performance"))
            {
                ce.buildings.GetComponent<BuildingEditorOrganizer>().Organize();
            }
            if (GUILayout.Button("Break up optimizations"))
            {
                ce.buildings.GetComponent<BuildingEditorOrganizer>().DeOrganize();
            }

            GUILayout.BeginVertical("box");
            optimizeMobile = EditorGUILayout.Foldout(optimizeMobile, new GUIContent("Optimize for Mobile", "Here you can set random generation values for buildings and building surfaces."), true);
            if (optimizeMobile)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                ce.useGrafitti = EditorGUILayout.Toggle(new GUIContent("Use Grafitti", "This deisables reflection probe blending"), ce.useGrafitti);
                ce.usePOM = EditorGUILayout.Toggle(new GUIContent("POM", "Disable POM"), ce.usePOM);

                if (GUILayout.Button("Apply Optimizations"))
                {
                    ce.OptimizeReflectionProbes();
                }
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();


            }


            GUILayout.EndVertical();
            ce.usePOM = EditorGUILayout.Toggle(new GUIContent("Use Parralax Mapping", "Switch between Parralax or normal mapped shader versions. Parralax shader versions aren't suitable for mobile platforms."), ce.usePOM);
            if (ce.usePOM == true)
                Shader.DisableKeyword ("_CSCAPE_DESKTOP_ON");
            else Shader.EnableKeyword("_CSCAPE_DESKTOP_ON");
            ce.quadtreeSkew = EditorGUILayout.Toggle(new GUIContent("Use Slanted streets (experimental)", "Use slated streets generation - this feature is experimental"), ce.quadtreeSkew);
            if (ce.quadtreeSkew) ce.quadtree = false;
            ce.quadtree = EditorGUILayout.Toggle(new GUIContent("Use Quadtree streets (experimental)", "Use slated streets generation - this feature is experimental"), ce.quadtree);
            if (ce.quadtree) ce.quadtreeSkew = false;
         //   CityRandomizer.aoSteps = EditorGUILayout.IntField("", CityRandomizer.aoSteps);
         //   CityRandomizer.aoAngle = EditorGUILayout.FloatField("", CityRandomizer.aoAngle);





            if (GUI.changed)
            {
                EditorUtility.SetDirty(ce);
#if UNITY_5_4_OR_NEWER
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
#endif
            }

        }
        void writeConfToFile()
        {
            string txt = "Configured";
            System.IO.File.WriteAllText(Application.dataPath + "/CSconfigured.txt", txt);
        }

        void readConfFromFile()
        {
            string txt;
            if (System.IO.File.Exists(Application.dataPath + "/CSconfigured.txt"))
            {
                txt = System.IO.File.ReadAllText(Application.dataPath + "/CSconfigured.txt");
                Debug.Log(txt);
            }
            
            
        }
    }
}
