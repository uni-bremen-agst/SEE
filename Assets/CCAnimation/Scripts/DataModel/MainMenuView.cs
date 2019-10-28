using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuView : MonoBehaviour
{
    public InputField GraphPathInputField;
    public Button GraphLoadButton;
    public Button GraphClearButton;
    public Dropdown OpenVersionDropdown;
    public Button CloseViewButton;

    void Start()
    {
        if (GraphPathInputField == null)
        {
            Debug.LogError("MainMenuView has no GraphPathInputField assigned!");
        }
        if (GraphLoadButton == null)
        {
            Debug.LogError("MainMenuView has no GraphLoadButton assigned!");
        }
        if (GraphClearButton == null)
        {
            Debug.LogError("MainMenuView has no GraphClearButton assigned!");
        }
        if (OpenVersionDropdown == null)
        {
            Debug.LogError("MainMenuView has no OpenVersionDropdown assigned!");
        }
        if (CloseViewButton == null)
        {
            Debug.LogError("MainMenuView has no CloseViewButton assigned!");
        }
    }
}
