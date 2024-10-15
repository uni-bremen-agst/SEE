using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.XR
{
    /// <summary>
    /// This class manages the input for all buttons on the keyboard.
    /// This script is based on this tutorial: https://www.youtube.com/watch?v=vTonHBr4t4g
    /// </summary>
    public class KeyboardManager : MonoBehaviour
    {
        /// <summary>
        /// The instance of the keyboardmanager.
        /// </summary>
        public static KeyboardManager instance;

        /// <summary>
        /// The shiftbutton, which shifts a character.
        /// </summary>
        public Button shiftButton;

        /// <summary>
        /// The deletebutton, which deletes a character.
        /// </summary>
        public Button deleteButton;

        /// <summary>
        /// The spacebuttom, which adds a space.
        /// </summary>
        public Button spaceButton;

        /// <summary>
        /// The enterbutton, which finalizes the input.
        /// </summary>
        public Button enterButton;

        /// <summary>
        /// The inputfield, which is accessed by the keyboard.
        /// </summary>
        public TMP_InputField inputField;

        /// <summary>
        /// This image is used to tell the user that the shiftbutton is active.
        /// </summary>
        private Image shiftButtonImage;

        /// <summary>
        /// Is true when the shiftbutton is active.
        /// </summary>
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

        /// <summary>
        /// This method adds a whitespace character.
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
}
