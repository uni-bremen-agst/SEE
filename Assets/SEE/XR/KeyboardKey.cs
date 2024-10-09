using TMPro;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// This class handles a single keyboard key.
/// This script is based on this tutorial: https://www.youtube.com/watch?v=vTonHBr4t4g
/// </summary>
public class KeyboardKey : MonoBehaviour
{
    /// <summary>
    /// The character of the key.
    /// </summary>
    public string character;
    /// <summary>
    /// The shifted character.
    /// </summary>
    public string shiftCharacter;
    /// <summary>
    /// Is true, when the key is shifted.
    /// </summary>
    private bool isShifted = false;
    /// <summary>
    /// The actual keyboard key.
    /// </summary>
    private Button key;
    /// <summary>
    /// The label of the keyboard key.
    /// </summary>
    public TextMeshProUGUI keyLabel;
    // Start is called before the first frame update
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
    /// <summary>
    /// This method returns the shifted equivalent to
    /// each number on the keyboard.
    /// </summary>
    /// <returns>The shifted equivalent to each number.</returns>
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
    /// <summary>
    /// This method handles the shifting of the
    /// alphabetical characters.
    /// </summary>
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
    /// <summary>
    /// This method transfer the character to the inputfield.
    /// </summary>
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
