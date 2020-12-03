#if UNITY_EDITOR

using System;
using System.Linq;
using SEE.Controls.Actions;
using SEE.DataModel;
using SEE.Game;
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
        private static readonly Type[] CityTypes = {
            // If there are SEECity types not listed in the menu, you can add them here.
            typeof(SEECity), typeof(SEECityEvolution), typeof(SEECityRandom), typeof(SEEDynCity), typeof(SEEJlgCity)
        };

        /// <summary>
        /// Names of the city types. This is automatically generated from <see cref="CityTypes"/> and shouldn't
        /// need to be changed.
        /// </summary>
        private static readonly string[] CityTypeNames = CityTypes.Select(x => x.Name).ToArray();
        
        // A few variables which help us keep track of the UI state
        private string cityName;
        private bool showCreation = true;
        private bool showPlatform = true;
        private int selectedType;
        
        public override void OnInspectorGUI()
        {
            // Platform settings which are defined in PlayerSettings class
            showPlatform = EditorGUILayout.Foldout(showPlatform, "Platform settings", true, EditorStyles.foldoutHeader);
            if (showPlatform)
            {
                base.OnInspectorGUI();
                EditorGUILayout.Space();  // additional space for improved readability
            }

            EditorGUILayout.Space();
                
            // Creation of new code city
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
                selectedType = EditorGUILayout.Popup("City type", selectedType, CityTypeNames);
                if (GUILayout.Button("Create City"))
                {
                    CreateCodeCity();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// Creates a new code city out of the parameters set in this editor.
        /// </summary>
        private void CreateCodeCity()
        {
            GameObject codeCity = new GameObject {tag = Tags.CodeCity, name = cityName};
            codeCity.transform.localScale = new Vector3(1f, 0.0001f, 1f); // choose sensible y-scale

            // Add required components
            codeCity.AddComponent<MeshRenderer>();
            codeCity.AddComponent<BoxCollider>();
            // Attach portal plane to navigation action components
            Plane plane = codeCity.AddComponent<Plane>();
            codeCity.AddComponent<DesktopNavigationAction>().portalPlane = plane;
            codeCity.AddComponent<XRNavigationAction>().portalPlane = plane;
            codeCity.AddComponent(CityTypes[selectedType]);
        }
    }
}

#endif