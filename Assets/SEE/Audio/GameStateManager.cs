using UnityEngine.SceneManagement;

namespace SEE.Audio
{
    /// <summary>
    /// Determines the different types of scenes that music can be played for.
    /// </summary>
    public static class GameStateManager
    {
        /// <summary>
        /// The name of the scene to start the game, i.e., the one in which the
        /// user can configure the network configuration and enter the game.
        /// </summary>
        const string StartScene = "SEEStart";

        /// <summary>
        /// Returns <see cref="SceneType.LOBBY"/> if <paramref name="scene"/>
        /// is the <see cref="StartScene"/> or otherwise <see cref="SceneType.IN_GAME"/>.
        /// </summary>
        /// <param name="scene">The currently loaded scene.</param>
        /// <returns>A game state enum for the given scene.</returns>
        public static SceneType GetBySceneName(Scene scene) => scene.name switch
        {
            StartScene => SceneType.LOBBY,
            _          => SceneType.IN_GAME,
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
            LOBBY,
            /// <summary>
            /// Whether a player is in a scene in which he/she can interact
            /// with code cities and other players.
            /// </summary>
            IN_GAME
        }
    }
}
