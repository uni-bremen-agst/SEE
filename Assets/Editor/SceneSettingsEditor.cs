#if UNITY_EDITOR

using System;
using System.Linq;
using SEE.DataModel;
using SEE.Game.City;
using SEE.Net.Dashboard;
using UnityEditor;
using UnityEngine;
using Plane = SEE.GO.Plane;

namespace SEEEditor
{
    /// <summary>
    /// An editor for the player settings class. Allows the user to set platform settings and create new code cities.
    /// </summary>
    ///[CustomEditor(typeof(SceneSettings))]
    [CanEditMultipleObjects]
    [Obsolete]
    public class SceneSettingsEditor : Editor
    {
        /// <summary>
        /// An array of all types of code cities which the user should be able to create.
        /// </summary>
        private static readonly Type[] CityTypes =
        {
            // If there are SEECity types not listed in the menu, you can add them here.
            typeof(SEECity), typeof(SEECityEvolution), typeof(SEECityRandom), typeof(SEEJlgCity)
        };

        /// <summary>
        /// Names of the city types. This is automatically generated from <see cref="CityTypes"/> and shouldn't
        /// need to be changed.
        /// </summary>
        private static readonly string[] CityTypeNames = CityTypes.Select(x => x.Name).ToArray();

        /// <summary>
        /// Name of the new city.
        /// </summary>
        private string cityName;

        /// <summary>
        /// If true, the foldout for creating a new city is shown.
        /// </summary>
        private bool showCreation = true;

        /// <summary>
        /// If true, the foldout for the platform settings is shown.
        /// </summary>
        private bool showPlatform = true;

        /// <summary>
        /// The kind of city to be created (regular code city, evolution city, dynamic city, etc.).
        /// </summary>
        private int selectedCityType;

        public override void OnInspectorGUI()
        {
            // Platform settings which are defined in PlayerSettings class
            showPlatform = EditorGUILayout.Foldout(showPlatform, "Platform settings", true, EditorStyles.foldoutHeader);
            if (showPlatform)
            {
                base.OnInspectorGUI();
                EditorGUILayout.Space(); // additional space for improved readability
            }
            EditorGUILayout.Space();
            CodeCityGUI();
        }

        /// <summary>
        /// The GUI components responsible for configuring and creating a code city.
        /// </summary>
        private void CodeCityGUI()
        {
            showCreation = EditorGUILayout.Foldout(showCreation, "Create a new code city", true, EditorStyles.foldoutHeader);
            if (showCreation)
            {
                cityName = EditorGUILayout.TextField("Name of new city", cityName);
                EditorGUILayout.BeginHorizontal();
                // Dropdown of all code city types
                selectedCityType = EditorGUILayout.Popup("City type", selectedCityType, CityTypeNames);
                if (GUILayout.Button("Create City"))
                {
                    CreateCodeCity();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                GUILayout.Label("Setup new scene", EditorStyles.largeLabel);
                if (GUILayout.Button("Add required objects to scene"))
                {
                    SetupScene();
                }
            }
        }

        /// <summary>
        /// Creates a new code city out of the parameters set in this editor.
        /// </summary>
        private void CreateCodeCity()
        {
            GameObject codeCity = new GameObject {tag = Tags.CodeCity, name = cityName};
            codeCity.transform.localScale = new Vector3(1f, 0.0001f, 1f); // choose sensible y-scale
            codeCity.transform.position = new Vector3(0, 0.964f, 0);

            // Add required components
            codeCity.AddComponent<MeshRenderer>();
            codeCity.AddComponent<BoxCollider>();
            // Attach portal plane to navigation action components
            codeCity.AddComponent<Plane>();

            codeCity.AddComponent(CityTypes[selectedCityType]);
        }

        /// <summary>
        /// Creates all required GameObjects for a scene to work, barring a code city.
        /// </summary>
        private void SetupScene()
        {
            //TODO: Check if objects are already there and only add as necessary
            //TODO: This is out of date, the MainScene looks different now!

            // Add dashboard retriever to PlayerSettings if it isn't there yet
            //PlayerSettings settings = FindObjectOfType<PlayerSettings>();
            //if (!settings.TryGetComponent(out DashboardRetriever _))
            //{
            //    settings.gameObject.AddComponent<DashboardRetriever>();
            //}

            // Create light
            GameObject light = new GameObject {name = "Light"};
            light.AddComponent<Light>().lightmapBakeType = LightmapBakeType.Mixed;

            // Create table from table prefab
            UnityEngine.Object tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Table.prefab");
            GameObject table = Instantiate(tablePrefab) as GameObject;
            UnityEngine.Assertions.Assert.IsNotNull(table);
            table.name = "Table";
            table.tag = Tags.CullingPlane;

            // Create ChartManager from prefab
            UnityEngine.Object chartManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Charts/ChartManager.prefab");
            GameObject chartManager = Instantiate(chartManagerPrefab) as GameObject;
            UnityEngine.Assertions.Assert.IsNotNull(chartManager);
            chartManager.name = "Chart Manager";
        }
    }
}

#endif