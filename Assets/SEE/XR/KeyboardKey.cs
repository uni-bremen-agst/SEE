using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardKey : MonoBehaviour
{
    public string character;
    public string shiftCharacter;

    private bool isShifted = false;

    private Button key;
    public TextMeshProUGUI keyLabel;

    private void Start()
    {
        KeyboardManager.instance.shiftButton.onClick.AddListener(HandleShift);
        key = GetComponent<Button>();
        key.onClick.AddListener(TypeKey);
        character = keyLabel.text;
        shiftCharacter = keyLabel.text.ToUpper();

        string numbers = "1234567890";

        if (numbers.Contains(keyLabel.text))
        {
            shiftCharacter = GetShiftCharacter();
        }
    }

    private string GetShiftCharacter()
    {
        switch (keyLabel.text)
        {
            case "1":
                return "!";
            case "2":
                return ".";
            case "3":
                return "#";
            case "4":
                return "?";
            case "5":
                return "%";
            case "6":
                return "&";
            case "7":
                return "+";
            case "8":
                return "(";
            case "9":
                return ")";
            case "0":
                return "=";
            default:
                break;
        }
        return string.Empty;
    }

    private void HandleShift()
    {
        isShifted = !isShifted;

        if (isShifted)
        {
            keyLabel.text = shiftCharacter;
        }
        else
        {
            keyLabel.text = character;
        }
    }

    private void TypeKey()
    {
        if (isShifted)
        {
            KeyboardManager.instance.inputField.text += shiftCharacter;
        }
        else
        {
            KeyboardManager.instance.inputField.text += character;
        }
    }
}
