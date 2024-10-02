using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class manages the input for all buttons on the keyboard.
/// </summary>
public class KeyboardManager : MonoBehaviour
{
    /// <summary>
    /// The instance of the keyboardmanager.
    /// </summary>
    public static KeyboardManager instance;
    /// <summary>
    /// The shiftbutton, which performs the shift-action.
    /// </summary>
    public Button shiftButton;
    /// <summary>
    /// The deletebutton, which performs the delete-action.
    /// </summary>
    public Button deleteButton;
    /// <summary>
    /// The spacebuttom, which performs the space-action.
    /// </summary>
    public Button spaceButton;
    /// <summary>
    /// The enterbutton, which performs the enter-action.
    /// </summary>
    public Button enterButton;
    /// <summary>
    /// The inputfield, which is accessed by the keyboard.
    /// </summary>
    public TMP_InputField inputField;
    /// <summary>
    /// This image is used, to tell the user, that the shiftbutton is active.
    /// </summary>
    private Image shiftButtonImage;

    /// <summary>
    /// Is true, when the shiftbutton is active.
    /// </summary>
    private bool isShifted = false;
    // Awake is always called before any Start functions.
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        spaceButton.onClick.AddListener(Space);
        deleteButton.onClick.AddListener(Delete);
        shiftButton.onClick.AddListener(Shifted);
        enterButton.onClick.AddListener(Enter);
        shiftButtonImage = shiftButton.gameObject.GetComponent<Image>();
    }
    /// <summary>
    /// This method performs the space-action on the keyboard.
    /// </summary>
    private void Space()
    {
        inputField.text += " ";
    }
    /// <summary>
    /// This method performs the enter-action on the keyboard.
    /// </summary>
    private void Enter()
    {
        inputField.onSubmit.Invoke(inputField.text);
        gameObject.transform.Find("Keyboard").gameObject.SetActive(false);
    }
    /// <summary>
    /// This method performs the delete-action on the keyboard.
    /// </summary>
    private void Delete()
    {
        int length = inputField.text.Length;
        if (length != 0)
        {
            length = length - 1;
            inputField.text = inputField.text.Substring(0, length);
        }
    }
    /// <summary>
    /// This method performs the shift-action on the keyboard.
    /// </summary>
    private void Shifted()
    {
        isShifted = !isShifted;

        if (isShifted)
        {
            shiftButtonImage.color = Color.yellow;
        }
        else
        {
            shiftButtonImage.color = Color.white;
        }
    }
}
