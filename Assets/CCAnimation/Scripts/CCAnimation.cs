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
    public Text revisionNumberText;
    public Toggle endlessModeToggle;
    public CCAStateManager stateManager;

    void Start()
    {
        UpdateText();
        stateManager.ViewDataChangedEvent.AddListener(OnViewDataChanged);
    }

    void Update()
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
            stateManager.ToggleEndlessMode();
        }
    }

    void UpdateText()
    {
        revisionNumberText.text = (stateManager.OpenGraphIndex + 1) + " / " + stateManager.GraphCount;
    }

    void OnViewDataChanged()
    {
        UpdateText();
        endlessModeToggle.isOn = stateManager.IsEndlessMode;
    }
}
