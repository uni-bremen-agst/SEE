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
        [MenuItem("SEE/Dump Graph Element Map")]
        public static void DumpGraphElementMap()
        {
            GraphElementIDMap.Dump();
        }
    }
}

#endif