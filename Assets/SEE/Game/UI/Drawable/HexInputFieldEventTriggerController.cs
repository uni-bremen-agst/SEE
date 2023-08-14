using Crosstales.RTVoice;
using Cysharp.Threading.Tasks.Triggers;
using SEE.Controls;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.SEE.Game.UI.Drawable
{
    [RequireComponent(typeof(TMP_InputField))]
    public class HexInputFieldEventTriggerController : MonoBehaviour
    {
        private TMP_InputField hexInputField;

        private void Awake()
        {
            hexInputField = GetComponent<TMP_InputField>();
            hexInputField.onEndEdit.AddListener(ActivateInput);
            hexInputField.onSelect.AddListener(DeactivateInput);
        }
        private void DeactivateInput(string hex)
        {
            SEEInput.KeyboardShortcutsEnabled = false;
        }

        private void ActivateInput(string hex)
        {
            SEEInput.KeyboardShortcutsEnabled = true;
        }

        private void OnDestroy()
        {
            SEEInput.KeyboardShortcutsEnabled = true;
        }
    }
}