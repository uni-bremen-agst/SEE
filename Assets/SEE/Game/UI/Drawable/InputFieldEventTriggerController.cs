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
    public class InputFieldEventTriggerController : MonoBehaviour
    {
        private TMP_InputField inputField;

        private void Awake()
        {
            inputField = GetComponent<TMP_InputField>();
            inputField.onSubmit.AddListener(ActivateInput);
            inputField.onSelect.AddListener(DeactivateInput);
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