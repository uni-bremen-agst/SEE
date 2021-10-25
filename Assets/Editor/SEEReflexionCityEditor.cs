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
    [CustomEditor(typeof(SEECityReflexion))]
    [CanEditMultipleObjects]
    public class SEEReflexionCityEditor : SEECityEditor
    {
        /// <summary>
        /// Displays the foldout in which users can load architecture, implementation, and mapping GXL files.
        /// </summary>
        protected override void Attributes()
        {
            SEECityReflexion city = target as SEECityReflexion;
            Assert.IsNotNull(city);
            showDataFiles = EditorGUILayout.Foldout(showDataFiles,
                                                    "Data Files", true, EditorStyles.foldoutHeader);
            if (showDataFiles)
            {
                city.GxlArchitecturePath = DataPathEditor.GetDataPath("GXL file (Architecture)",
                                                                      city.GxlArchitecturePath,
                                                                      Filenames.ExtensionWithoutPeriod(Filenames.GXLExtension));
                city.GxlImplementationPath = DataPathEditor.GetDataPath("GXL file (Implementation)",
                                                                        city.GxlImplementationPath,
                                                                        Filenames.ExtensionWithoutPeriod(Filenames.GXLExtension));
                city.GxlMappingPath = DataPathEditor.GetDataPath("GXL file (Mapping)",
                                                                 city.GxlMappingPath,
                                                                 Filenames.ExtensionWithoutPeriod(Filenames.GXLExtension));
            }
        }
    }
}