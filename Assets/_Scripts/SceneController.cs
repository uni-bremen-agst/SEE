using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneController
{
    public enum Scene
    {
        MainMenu,
        Singleplayer,
        Multiplayer
    }

    public static void LoadScene(Scene scene)
    {
        Debug.Log("Loading \"" + scene + "\" scene...");
        SceneManager.LoadScene((int)scene);
    }
}
