using SEE.Game;
using SEE.Game.City;
using SEE.Utils;
using UnityEditor;
using UnityEngine.Assertions;

namespace SEEEditor
{
    /// <summary>
    /// Custom editor for cities intended for reflexion analysis.
    /// The reflection city is constructed by loading three separate files -- one for the implementation,
    /// one for the architecture, and one for the mapping from implementation nodes to architecture nodes.
    /// </summary>
    [CustomEditor(typeof(SEEReflexionCity))]
    [CanEditMultipleObjects]
    public class SEEReflexionCityEditor : SEECityEditor
    {
        /// <summary>
        /// Displays the foldout in which users can load architecture, implementation, and mapping GXL files.
        /// </summary>
        protected override void Attributes()
        {
            SEEReflexionCity reflexionCity = target as SEEReflexionCity;
            Assert.IsNotNull(reflexionCity);
            showDataFiles = EditorGUILayout.Foldout(showDataFiles,
                                                    "Data Files", true, EditorStyles.foldoutHeader);
            if (showDataFiles)
            {
                reflexionCity.GxlArchitecturePath = DataPathEditor.GetDataPath("GXL file (Architecture)",
                                                                      reflexionCity.GxlArchitecturePath,
                                                                      Filenames.ExtensionWithoutPeriod(Filenames.GXLExtension)) as FilePath;
                reflexionCity.GxlImplementationPath = DataPathEditor.GetDataPath("GXL file (Implementation)",
                                                                        reflexionCity.GxlImplementationPath,
                                                                        Filenames.ExtensionWithoutPeriod(Filenames.GXLExtension)) as FilePath;
                reflexionCity.GxlMappingPath = DataPathEditor.GetDataPath("GXL file (Mapping)",
                                                                 reflexionCity.GxlMappingPath,
                                                                 Filenames.ExtensionWithoutPeriod(Filenames.GXLExtension)) as FilePath;
                reflexionCity.CsvArchitecturePath = DataPathEditor.GetDataPath("CSV file (Architecture)",
                                                                      reflexionCity.CsvArchitecturePath,
                                                                      Filenames.ExtensionWithoutPeriod(Filenames.CSVExtension)) as FilePath;
                reflexionCity.CsvImplementationPath = DataPathEditor.GetDataPath("CSV file (Implementation)",
                                                                        reflexionCity.CsvImplementationPath,
                                                                        Filenames.ExtensionWithoutPeriod(Filenames.CSVExtension)) as FilePath;
                reflexionCity.CityName = EditorGUILayout.TextField("City Name", reflexionCity.CityName);
            }
        }
    }
}