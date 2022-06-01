#if UNITY_EDITOR

using SEE.Game.City;
using SEE.Utils;
using UnityEditor;

namespace SEEEditor
{
    /// <summary>
    /// A custom editor for instances of SEEDynCity as an extension of the SEECityEditor.
    /// </summary>
    //[CustomEditor(typeof(SEEDynCity))]
    [CanEditMultipleObjects]
    public class SEEDynCityEditor : SEECityEditor
    {
        /// <summary>
        /// In addition to the other attributes inherited, the specific attributes of
        /// the SEEDynCity instance are shown and set here.
        /// </summary>
        protected override void Attributes()
        {
            base.Attributes();
            SEEDynCity city = target as SEEDynCity;
            city.DYNPath = DataPathEditor.GetDataPath("DYN file", city.DYNPath, Filenames.ExtensionWithoutPeriod(Filenames.DYNExtension)) as FilePath;
        }
    }
}

#endif