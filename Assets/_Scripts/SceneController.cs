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
            Multiplayer,
            EditCharacter,
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

        public void LoadEditCharacterScene()
        {
            LoadScene(Scene.EditCharacter);
        }

        public static void LoadScene(Scene scene)
        {
            Debug.Log("Loading \"" + scene + "\" scene...");
            SceneManager.LoadScene((int)scene);
        }
    }

}
