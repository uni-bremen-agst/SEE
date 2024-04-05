using UnityEngine.SceneManagement;

namespace SEE.Audio
{
    /// <summary>
    /// Determines the different types of scenes that music can be played for.
    /// </summary>
    public static class SceneContext
    {
        /// <summary>
        /// The name of the scene to start the game, i.e., the one in which the
        /// user can configure the network configuration and enter the game.
        /// </summary>
        private const string startScene = "SEEStart";

        /// <summary>
        /// Returns <see cref="SceneType.Lobby"/> if <paramref name="scene"/>
        /// is the <see cref="startScene"/> or otherwise <see cref="SceneType.InGame"/>.
        /// </summary>
        /// <param name="scene">The currently loaded scene.</param>
        /// <returns>A game state enum for the given scene.</returns>
        public static SceneType GetSceneType(Scene scene) => scene.name switch
        {
            startScene => SceneType.Lobby,
            _          => SceneType.InGame,
        };

        /// <summary>
        /// Available types of scenes.
        /// </summary>
        public enum SceneType
        {
            /// <summary>
            /// Whether a player is currently in the "lobby", i.e, the scene in
            /// which the user can select the network configuration.
            /// </summary>
            Lobby,
            /// <summary>
            /// Whether a player is in a scene in which he/she can interact
            /// with code cities and other players.
            /// </summary>
            InGame
        }
    }
}
