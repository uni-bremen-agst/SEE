#if UNITY_EDITOR

using SEE.Game;
using UnityEditor;

namespace SEEEditor
{
    /// <summary>
    /// Dumps the content of <see cref="GraphElementIDMap"/>.
    /// </summary>
    /// <remarks>Used for debugging.</remarks>
    public static class DumpMap
    {
        /// <summary>
        /// Dumps the contents of <see cref="GraphElementIDMap"/> to the Unity console.
        /// </summary>
        [MenuItem("SEE/Dump Graph Element Map")]
        public static void DumpGraphElementMap()
        {
            GraphElementIDMap.Dump();
        }
    }
}

#endif