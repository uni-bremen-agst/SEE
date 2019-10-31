using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Datamodel for ingameview.
/// </summary>
public class InGameMenuModel : MonoBehaviour
{
    public Text AnimationTimeText;
    public Text RevisionNumberText;
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
