using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SEE.UI.PictureInPicture
{
    /// <summary>
    /// The Data Source Structure for the Picture-In-Picture UI-Element.
    /// </summary>
    [CreateAssetMenu(menuName = "SEE/UI/Picture-in-Picture/Data Source")]
    public class PIPDataSource : ScriptableObject
    {
        [Header("Render Images")]
        /// <summary>
        /// The <see cref="RenderTexture"/> that is currently being displayed.
        /// </summary>
        [field: SerializeField, DontCreateProperty]
        [CreateProperty]
        public RenderTexture PIPImage { get; set; }

    }
}
