namespace SEE.Audio
{
    /// <summary>
    /// Defines the different game states that music can be played for.
    /// </summary>
    public static class GameStateManager
    {
        public static GameState GetBySceneName(string sceneName)
        {
            switch (sceneName)
            {
                case "SEEStart":
                    return GameState.LOBBY;
                case "SEEWorld":
                    return GameState.IN_GAME;
                default:
                    return GameState.IN_GAME;
            }
        }

        public enum GameState {
            LOBBY, IN_GAME
        }
        
    }

}
