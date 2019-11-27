using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Datamodel for mainmenuview.
/// </summary>
public class MainMenuModel : MonoBehaviour
{
    public Dropdown OpenVersionDropdown;
    public Button CloseViewButton;

    /// <summary>
    /// Checks if all fields are initialized.
    /// </summary>
    void Start()
    {
        OpenVersionDropdown.AssertNotNull("OpenVersionDropdown");
        CloseViewButton.AssertNotNull("CloseViewButton");
    }
}
