using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Datamodel for ingameview.
/// </summary>
public class InGameMenuModel : MonoBehaviour
{
    /// <summary>
    /// TextField for the used AnimationTime in seconds.
    /// </summary>
    public Text AnimationTimeText;

    /// <summary>
    /// TextField for the show revision in game.
    /// </summary>
    public Text RevisionNumberText;

    /// <summary>
    /// Toggle that shows if autoplaing the animations is active.
    /// </summary>
    public Toggle AutoplayToggle;

    /// <summary>
    /// Checks if all fields are initialized.
    /// </summary>
    void Start()
    {
        AnimationTimeText.AssertNotNull("AnimationTimeText");
        RevisionNumberText.AssertNotNull("RevisionNumberText");
        AutoplayToggle.AssertNotNull("AutoplayToggle");
    }
}
