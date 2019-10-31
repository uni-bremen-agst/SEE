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

    private MainMenuModel mainMenu;

    public Text revisionNumberText;
    public Toggle autoplayToggle;

    public CCAStateManager stateManager;

    public bool IsMainMenuOpen => !FlyCamera.IsEnabled;

    void Start()
    {
        mainMenu = MainMenuCanvas.GetComponent<MainMenuModel>();
        if (mainMenu == null)
        {
            Debug.LogError("MainMenuCanvas has no MainMenuView assigned!");
        }

        mainMenu.CloseViewButton.onClick.AddListener(ToogleMainMenu);

        ToogleMainMenu(true);
        UpdateText();
        stateManager.ViewDataChangedEvent.AddListener(OnViewDataChanged);
        // TODO flo: subscribe to toggle changed
    }

    void Update()
    {
        if(IsMainMenuOpen)
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

    void UpdateText()
    {
        revisionNumberText.text = (stateManager.OpenGraphIndex + 1) + " / " + stateManager.GraphCount;
    }

    void OnViewDataChanged()
    {
        UpdateText();
        autoplayToggle.isOn = stateManager.IsAutoPlay;
    }
}
