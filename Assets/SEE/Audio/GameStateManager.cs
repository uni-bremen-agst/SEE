using UnityEngine.SceneManagement;

namespace SEE.Audio
{
    /// <summary>
    /// Defines the different game states that music can be played for.
    /// </summary>
    public static class GameStateManager
    {
        /// <summary>
        /// Returns the game state enum for a given scene.
        /// </summary>
        /// <param name="scene">The currently loaded scene.</param>
        /// <returns>A game state enum for the given scene.</returns>
        public static GameState GetBySceneName(Scene scene) => scene.name switch
        {
            _ => GameState.IN_GAME,
        };

        /// <summary>
        /// Avaible game states.
        /// </summary>
        public enum GameState 
        {
            LOBBY, IN_GAME
        }
    }
}
