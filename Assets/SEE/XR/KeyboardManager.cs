using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardManager : MonoBehaviour
{
    public static KeyboardManager instance;
    public Button shiftButton;
    public Button deleteButton;
    public Button spaceButton;
    public Button enterButton;
    public TMP_InputField inputField;
    private Image shiftButtonImage;

    private bool isShifted = false;
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

    private void Space()
    {
        inputField.text += " ";
    }

    private void Enter()
    {
        inputField.onSubmit.Invoke(inputField.text);
        gameObject.transform.Find("Keyboard").gameObject.SetActive(false);
    }

    private void Delete()
    {
        int length = inputField.text.Length;
        if (length != 0)
        {
            length = length - 1;
            inputField.text = inputField.text.Substring(0, length);
        }
    }
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
