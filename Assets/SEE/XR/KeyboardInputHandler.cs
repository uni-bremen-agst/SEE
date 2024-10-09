using SEE.Controls;
using SEE.GO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// This class changes the inputfield for the VR-Keyboard and
/// also activates the keyboard.
/// This script is based on this tutorial: https://www.youtube.com/watch?v=vTonHBr4t4g
/// </summary>
public class KeyboardInputHandler : MonoBehaviour, IPointerClickHandler
{
    /// <summary>
    /// This method gets called, when the user clicks the input field.
    /// If the user is in VR, the keyboard gets activated.
    /// </summary>
    /// <param name="eventdata">Event data associated with the event, when the user clicks on the inputfield.</param>
    public void OnPointerClick(PointerEventData eventdata)
    {
        if (SceneSettings.InputType == PlayerInputType.VRPlayer)
        {
            KeyboardManager.instance.inputField = GetComponent<TMP_InputField>();
            KeyboardManager.instance.gameObject.transform.Find("Keyboard").gameObject.SetActive(true);
        }
    }
}
