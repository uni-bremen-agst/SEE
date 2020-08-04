#if UNITY_EDITOR

using UnityEditor;
using SEE.Game;

namespace SEEEditor
{
    
        /// <summary>
        /// A custom editor for instances of SEEJlgCity as an extension of the SEECityEditor.
        /// </summary>
        [CustomEditor(typeof(SEEJlgCity))]
        [CanEditMultipleObjects]
        public class SEEJlgCityEditor : SEECityEditor
        {
            /// <summary>
            /// Adds the JLGPath value of SEEJlgCity to the inherited settings.
            /// </summary>
            protected override void Attributes()
            {
                base.Attributes();
                SEEJlgCity city = target as SEEJlgCity;
                city.jlgPath = EditorGUILayout.TextField("Full JLG filepath", city.jlgPath);
            }
        }
    }
#endif