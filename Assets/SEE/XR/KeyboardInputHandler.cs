using SEE.Controls;
using SEE.GO;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class KeyboardInputHandler : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventdata)
    {
        if (SceneSettings.InputType == PlayerInputType.VRPlayer)
        {
            KeyboardManager.instance.inputField = GetComponent<TMP_InputField>();
            KeyboardManager.instance.gameObject.transform.Find("Keyboard").gameObject.SetActive(true);
        }
    }
}
