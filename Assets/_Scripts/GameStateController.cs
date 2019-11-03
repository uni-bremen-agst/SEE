using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE;//TODO

public class GameStateController : MonoBehaviour
{
    public GameObject ingameMenu;

    private FlyCamera cameraScript;

    void Start()
    {
        ingameMenu.SetActive(false);
        cameraScript = Camera.main.GetComponent<FlyCamera>();
        cameraScript.isActive = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        bool menuOpened = !ingameMenu.activeSelf;
        ingameMenu.SetActive(menuOpened);
        cameraScript.isActive = !menuOpened;
    }

    public void OpenMenu()
    {
        ingameMenu.SetActive(true);
        cameraScript.isActive = false;
    }

    public void CloseMenu()
    {
        ingameMenu.SetActive(false);
        cameraScript.isActive = true;
    }
}
