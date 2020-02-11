using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Datamodel for mainmenuview.
/// </summary>
public class MainMenuModel : MonoBehaviour
{

    /// <summary>
    /// Dropdown that sets the actual shown revision.
    /// </summary>
    public Dropdown OpenVersionDropdown;

    /// <summary>
    /// Button to close the MainMenu
    /// </summary>
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
