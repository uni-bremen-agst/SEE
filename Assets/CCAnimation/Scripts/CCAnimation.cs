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
    public CCAStateManager stateManager;

    void Start()
    {
        updateText();
    }

    void Update()
    {

        if (Input.GetKeyDown("k"))
        {
            stateManager.ShowPreviousGraph();
            updateText();
        }
        else if (Input.GetKeyDown("l"))
        {
            stateManager.ShowNextGraph();
            updateText();
        }
    }

    void updateText()
    {
        revisionNumberText.text = (stateManager.OpenGraphIndex + 1) + " / " + stateManager.GraphCount;
    }
}
