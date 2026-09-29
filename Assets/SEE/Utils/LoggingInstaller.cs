using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Connects <see cref="Logging.Logger"/> to the Unity console.
    ///
    /// The layers below Unity log through <see cref="Logging"/>, whose default
    /// writes to the standard output streams. Inside Unity those streams are not
    /// visible, so this class substitutes <see cref="SEELogger"/> and the messages
    /// end up in the Unity console again, exactly as they did when those layers
    /// still called <c>UnityEngine.Debug</c> themselves.
    /// </summary>
    internal static class LoggingInstaller
    {
        /// <summary>
        /// Makes <see cref="Logging.Logger"/> forward to <see cref="SEELogger"/>.
        /// </summary>
        /// <remarks>
        /// This runs before the first scene is loaded, and in the editor also after
        /// every domain reload, because code may emit messages while an inspector is
        /// drawn, that is, long before any scene is played.
        /// </remarks>
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ProvideUnityLogger()
        {
            Logging.Logger = new SEELogger();
        }
    }
}
