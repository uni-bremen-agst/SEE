using SEE;
using SEE.DataModel;
using SEE.Layout;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// : doc
/// </summary>
public class CCAnimation : MonoBehaviour
{
    public FlyCamera FlyCamera;
    public GameObject InGameCanvas;
    public GameObject MainMenuCanvas;

    private InGameMenuModel inGameMenu;
    private MainMenuModel mainMenu;

    public CCAStateManager stateManager;

    public bool IsMainMenuOpen => !FlyCamera.IsEnabled;

    void Start()
    {
        mainMenu = MainMenuCanvas.GetComponent<MainMenuModel>();
        if (mainMenu == null)
        {
            Debug.LogError("MainMenuCanvas has no MainMenuModel assigned!");
        }
        inGameMenu = InGameCanvas.GetComponent<InGameMenuModel>();
        if (inGameMenu == null)
        {
            Debug.LogError("InGameMenuCanvas has no InGameMenuModel assigned!");
        }

        mainMenu.CloseViewButton.onClick.AddListener(ToogleMainMenu);

        ToogleMainMenu(true);
        OnViewDataChanged();
        stateManager.ViewDataChangedEvent.AddListener(OnViewDataChanged);
    }

    void Update()
    {
        if (!IsMainMenuOpen)
        {
            if (Input.GetKeyDown("k"))
            {
                stateManager.ShowPreviousGraph();
            }
            else if (Input.GetKeyDown("l"))
            {
                stateManager.ShowNextGraph();
            }
            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                stateManager.ToggleAutoplay();
            }

            string[] animationTimeKeys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
            float[] animationTimeValues = { 0.1f, 0.5f, 1, 2, 3, 4, 5, 8, 16, 0 };
            for (int i = 0; i < animationTimeKeys.Length; i++)
            {
                if (Input.GetKeyDown(animationTimeKeys[i]))
                {
                    stateManager.AnimationTime = animationTimeValues[i];
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToogleMainMenu(FlyCamera.IsEnabled);
        }
    }

    void ToogleMainMenu()
    {
        ToogleMainMenu(FlyCamera.IsEnabled);
    }

    void ToogleMainMenu(bool enabled)
    {
        FlyCamera.IsEnabled = !enabled;
        InGameCanvas.SetActive(!enabled);
        MainMenuCanvas.SetActive(enabled);
        stateManager.ToggleAutoplay(false);
    }

    void OnViewDataChanged()
    {
        inGameMenu.RevisionNumberText.text = (stateManager.OpenGraphIndex + 1) + " / " + stateManager.GraphCount;
        inGameMenu.AutoplayToggle.isOn = stateManager.IsAutoPlay;
        inGameMenu.AnimationTimeText.text = "Revision animation time: " + stateManager.AnimationTime + "s";
    }
}
