using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Datamodel for mainmenuview.
/// </summary>
public class MainMenuModel : MonoBehaviour
{
    public InputField GraphPathInputField;
    public Button GraphLoadButton;
    public Button GraphClearButton;
    public Dropdown OpenVersionDropdown;
    public Button CloseViewButton;

    /// <summary>
    /// Checks if all fields are initialized.
    /// </summary>
    void Start()
    {
        GraphPathInputField.AssertNotNull("GraphPathInputField");
        GraphLoadButton.AssertNotNull("GraphLoadButton");
        GraphClearButton.AssertNotNull("GraphClearButton");
        OpenVersionDropdown.AssertNotNull("OpenVersionDropdown");
        CloseViewButton.AssertNotNull("CloseViewButton");
    }
}
