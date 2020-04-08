using UnityEditor;
using SEE.Game;

namespace SEEEditor
{
    /// <summary>
    /// A custom editor for instances of SEEDynCity as an extension of the SEECityEditor.
    /// </summary>
    [CustomEditor(typeof(SEEDynCity))]
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
            city.dynPath = EditorGUILayout.TextField("DYN file", city.dynPath);
        }
    }
}