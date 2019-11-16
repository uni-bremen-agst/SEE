using UnityEngine;

namespace SEE
{

    public enum GameState
    {
        Ingame,
        IngameMenu,
        SearchMenu
    }

    public class GameStateController : MonoBehaviour
    {
        private GameState currentGameState;
        private FlyCamera cameraScript;
        private GameObject ingameMenu;
        private GameObject searchMenu;

        public void Initialize()
        {
            cameraScript = Camera.main.GetComponent<FlyCamera>();
            ingameMenu = GameObject.Find("IngameMenu");
            searchMenu = GameObject.Find("SearchMenu");
            SetState(GameState.Ingame);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                switch (currentGameState)
                {
                    case GameState.Ingame:
                        {
                            SetState(GameState.IngameMenu);
                        } break;
                    case GameState.IngameMenu:
                        {
                            SetState(GameState.Ingame);
                        } break;
                    case GameState.SearchMenu:
                        {
                            SetState(GameState.Ingame);
                        } break;
                }
            }
            if (Input.GetKeyDown(KeyCode.F) && Input.GetKey(KeyCode.LeftShift))
            {
                switch (currentGameState)
                {
                    case GameState.Ingame:
                        {
                            SetState(GameState.SearchMenu);
                        }
                        break;
                    case GameState.IngameMenu: break;
                    case GameState.SearchMenu:
                        {
                            SetState(GameState.Ingame);
                        }
                        break;
                }
            }
        }

        public void SetState(GameState state)
        {
            currentGameState = state;
            switch (state)
            {
                case GameState.Ingame:
                    {
                        cameraScript.isActive = true;
                        ingameMenu.SetActive(false);
                        searchMenu.SetActive(false);
                    } break;
                case GameState.IngameMenu:
                    {
                        cameraScript.isActive = false;
                        ingameMenu.SetActive(true);
                        searchMenu.SetActive(false);
                    } break;
                case GameState.SearchMenu:
                    {
                        cameraScript.isActive = false;
                        ingameMenu.SetActive(false);
                        searchMenu.SetActive(true);
                    } break;
            }
        }
    }

}
