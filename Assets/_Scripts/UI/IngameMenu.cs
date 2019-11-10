using UnityEngine;
using UnityEngine.UI;

namespace SEE
{

    public class IngameMenu : MonoBehaviour
    {
        public GameStateController gameStateController { get; private set; }

        public void Initialize()
        {
            gameStateController = FindObjectOfType<GameStateController>();
            RawImage rawImage = GetComponent<RawImage>();
            MenuBackdropGenerator mbg = FindObjectOfType<MenuBackdropGenerator>();
            rawImage.texture = mbg.Backdrop;
        }

        public void Close()
        {
            gameStateController.SetState(GameState.Ingame);
        }

        public void GoToMainMenu()
        {
            SceneController.LoadScene(SceneController.Scene.MainMenu);
        }
    }

}// namespace SEE
