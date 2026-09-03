using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

// Only use UnityEditor-Namespaces when inside the Unity-Editor.
#if UNITY_EDITOR

using UnityEditor;

#endif

namespace SEE.Cinemachines.UI.PictureInPicture
{
    /// <summary>
    /// The Data Source Structure for the Picture-In-Picture UI-Element.
    /// </summary>
    [CreateAssetMenu(menuName = "SEE/UI/Picture-in-Picture/Data Source")]
    internal class PIPDataSource : ScriptableObject
    {
        // Encasing Class content inside the UNITY_EDITOR directive to ensure, that these Components only activly work inside the Unity-Editor.
        #if UNITY_EDITOR

        [Header("Render Images")]
        /// <summary>
        /// The <see cref="RenderTexture"/> that is currently being displayed.
        /// </summary>
        [field: SerializeField, DontCreateProperty]
        [CreateProperty]
        public RenderTexture PIPImage { get; set; }

        #endif
    }
}
