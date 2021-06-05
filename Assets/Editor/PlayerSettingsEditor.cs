#if UNITY_EDITOR

using System;
using System.Linq;
using SEE.Controls.Actions;
using SEE.DataModel;
using SEE.Game;
using SEE.Net.Dashboard;
using UnityEditor;
using UnityEngine;
using Plane = SEE.GO.Plane;
using PlayerSettings = SEE.Controls.PlayerSettings;

namespace SEEEditor
{
    /// <summary>
    /// An editor for the player settings class. Allows the user to set platform settings and create new code cities.
    /// </summary>
    [CustomEditor(typeof(PlayerSettings))]
    [CanEditMultipleObjects]
    public class PlayerSettingsEditor : Editor
    {
        /// <summary>
        /// An array of all types of code cities which the user should be able to create.
        /// </summary>
        private static readonly Type[] CityTypes =
        {
            // If there are SEECity types not listed in the menu, you can add them here.
            typeof(SEECity), typeof(SEECityEvolution), typeof(SEECityRandom), typeof(SEEDynCity), typeof(SEEJlgCity)
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
        /// If true, the foldout for the Axivion dashboard settings is shown.
        /// </summary>
        private bool showAxivionDashboard = true;

        /// <summary>
        /// The kind of city to be created (regular code city, evolution city, dynamic city, etc.).
        /// </summary>
        private int selectedCityType;

        /// <summary>
        /// The URL to the Axivion Dashboard, up to the project name.
        /// </summary>
        private string baseUrl = "https://stvive.informatik.uni-bremen.de:9443/axivion/projects/SEE/";
        /// <summary>
        /// The API token for the Axivion Dashboard.
        /// </summary>
        private string token = "0.0000000000014.l-qjyU2eTKuNl7v0PsBb4qfI3sVHvtwKGGeOTKV_3eE";
        /// <summary>
        /// The public key for the X.509 certificate authority from the dashboard's certificate.
        /// </summary>
        private string publicKey = "3082020A0282020100B20ACB6E1639D673B6AF9E9F36578F66068AFDA50327DC2AB0F804E2F8"
                                   + "3765BCB7AD74FED31EC8812FF9AA9C2461D53F7DC08449C765F0ECFA9C0787B9D1E1AE92F8D"
                                   + "1919EDB6871E70601DB0834FF34389EDBA30BFF48F3EA8D07786E976B04F5232AC3A63D07DA"
                                   + "5EAD5F5450026C9E2FB9294D32FC0172E9F0DFF33CDCB35180DB22E6985C15B02BBFAD02499"
                                   + "D0E52AA916ADD5F9E7A40E22B8EC5427E02E47FD78CFEF30B5A2EDB53EA47E8B70230FB9EAE"
                                   + "57B4B7042BD8829F67F4DCDA0230BB933741AF42992CD9164C4F5E2C126A46DC42AE5BD2268"
                                   + "2C97880F8D0A82FA36FEC89CE9318E0DE2CAA3352F92F6231B18DF29913445AA323931106B0"
                                   + "764066DB4A2F8764CE4FAC2500F5A084AE3133C6C82D18181655FC1050629257A54B44FBACB"
                                   + "1BE43E51C7FA80DD7CE68D2F86AF448CA2E03B3C81A1289AA355E926CF221D881BFCD82BE7B"
                                   + "0FA99F1B04A95D23F9B030B6CDF81E90197868BC72F314E2DFBA5F9965517F8C33BF056C005"
                                   + "0DB08D285C988EFF7F212CC9D652E70B8FE67BC632F17ECFAE57603F5592C831951442F8215"
                                   + "75139D193B4DC4F2EB46FEE09495CCF67259A3F4516873612582B84512019A1157F621B46D4"
                                   + "5BCEE471BCE855C068B701F40C4CBB78F8E11550C83D7E6897967FD0B90C4BD25B0E3884492"
                                   + "66293CF52814112B7F1A95A8EC3D6CBB5567B6B0916A995D5EB8254E31647B2F810203010001";

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
            showAxivionDashboard = EditorGUILayout.Foldout(showAxivionDashboard, "Axivion Dashboard", true, EditorStyles.foldoutHeader);
            if (showAxivionDashboard)
            {
                AxivionDashboardGUI();
                EditorGUILayout.Space();
            }
            EditorGUILayout.Space();
            CodeCityGUI();
        }

        /// <summary>
        /// The GUI components responsible for configuring the Axivion dashboard data.
        /// </summary>
        private void AxivionDashboardGUI()
        {
            baseUrl = DashboardRetriever.BaseUrl = EditorGUILayout.TextField("URL (up to project name)", baseUrl);
            token = DashboardRetriever.Token = EditorGUILayout.TextField("API Token", token);
            publicKey = DashboardRetriever.PublicKey = EditorGUILayout.TextField("CA's Public Key", publicKey);
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
            Plane plane = codeCity.AddComponent<Plane>();

            codeCity.AddComponent(CityTypes[selectedCityType]);
        }

        /// <summary>
        /// Creates all required GameObjects for a scene to work, barring a code city.
        /// </summary>
        private void SetupScene()
        {
            //TODO: Check if objects are already there and only add as necessary
            //TODO: Make compatible with MRTK

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