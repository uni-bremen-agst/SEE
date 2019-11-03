using UnityEngine;
using UnityEngine.SceneManagement;

namespace SEE
{

    public class SceneController : MonoBehaviour
    {
        public enum Scene
        {
            MainMenu,
            Singleplayer,
            Multiplayer
        }

        public void LoadMainMenuScene()
        {
            LoadScene(Scene.MainMenu);
        }

        public void LoadSingleplayerScene()
        {
            LoadScene(Scene.Singleplayer);
        }

        public void LoadMultiplayerScene()
        {
            LoadScene(Scene.Multiplayer);
        }

        public static void LoadScene(Scene scene)
        {
            Debug.Log("Loading \"" + scene + "\" scene...");
            SceneManager.LoadScene((int)scene);
        }
    }

}// namespace SEE
