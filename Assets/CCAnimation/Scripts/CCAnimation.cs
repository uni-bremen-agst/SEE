using SEE;
using SEE.DataModel;
using SEE.Layout;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        inGameMenu = InGameCanvas.GetComponent<InGameMenuModel>();

        mainMenu.AssertNotNull("mainMenu");
        inGameMenu.AssertNotNull("inGameMenu");

        mainMenu.CloseViewButton.onClick.AddListener(ToogleMainMenu);
        mainMenu.OpenVersionDropdown.onValueChanged.AddListener(OnDropDownChanged);

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
        if (enabled)
        {
            mainMenu.OpenVersionDropdown.ClearOptions();
            var options = Enumerable
                .Range(1, stateManager.GraphCount)
                .Select(i => new Dropdown.OptionData(i.ToString()))
                .ToList();
            mainMenu.OpenVersionDropdown.AddOptions(options);
            mainMenu.OpenVersionDropdown.value = stateManager.OpenGraphIndex;
        }
    }

    void OnViewDataChanged()
    {
        inGameMenu.RevisionNumberText.text = (stateManager.OpenGraphIndex + 1) + " / " + stateManager.GraphCount;
        inGameMenu.AutoplayToggle.isOn = stateManager.IsAutoPlay;
        inGameMenu.AnimationTimeText.text = "Revision animation time: " + stateManager.AnimationTime + "s";
    }

    void OnDropDownChanged(int value)
    {
        if (value != stateManager.OpenGraphIndex)
        {
            stateManager.TryShowSpecificGraph(value);
        }
    }
}
