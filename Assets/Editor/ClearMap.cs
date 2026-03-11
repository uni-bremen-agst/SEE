#if UNITY_EDITOR

using SEE.Game;
using UnityEditor;

namespace SEEEditor
{
    /// <summary>
    /// Clear <see cref="GraphElementIDMap"/>. Will be empty afterwards.
    /// </summary>
    /// <remarks>Used for cleaning in case something failed and graph elements
    /// were not removed properly from the map.</remarks>
    public static class ClearMap
    {
        [MenuItem("SEE/Clear Graph Element Map")]
        public static void ClearGraphElementMap()
        {
            GraphElementIDMap.Clear();
        }
    }
}

#endif
