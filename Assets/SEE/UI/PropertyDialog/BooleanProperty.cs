using Michsky.UI.ModernUIPack;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;

namespace SEE.UI.PropertyDialog
{
    /// <summary>
    /// A boolean input field (switch) for a property dialog.
    /// </summary>
    public class BooleanProperty : Property<bool>
    {
        /// <summary>
        /// The prefab.
        /// </summary>
        private const string inputFieldPrefab = "Prefabs/UI/InputFields/BooleanInputField";

        /// <summary>
        /// The switch manager.
        /// </summary>
        private SwitchManager switchManager;

        /// <summary>
        /// The input field game object.
        /// </summary>
        private GameObject inputField;

        /// <summary>
        /// The parent of the input field.
        /// </summary>
        private GameObject parentOfInputField;

        /// <summary>
        /// The value before the <see cref="switchManager"/> is initialized.
        /// </summary>
        private bool savedValue;

        /// <summary>
        /// The value.
        /// </summary>
        public override bool Value
        {
            get => switchManager != null ? switchManager.isOn : savedValue;
            set
            {
                savedValue = value;
                if (switchManager != null)
                {
                    switchManager.isOn = value;
                    switchManager.UpdateUI();
                }
            }
        }

        protected override void StartDesktop()
        {
            inputField = PrefabInstantiator.InstantiatePrefab(inputFieldPrefab, instantiateInWorldSpace: false);
            switchManager = inputField.transform.Find("Switch").gameObject.MustGetComponent<SwitchManager>();
            switchManager.isOn = savedValue;
            switchManager.UpdateUI();

            if (parentOfInputField != null)
            {
                SetParent(parentOfInputField);
            }
            inputField.name = Name;

            Transform placeHolder = inputField.transform.Find("Label");
            if (placeHolder.gameObject.TryGetComponentOrLog(out TextMeshProUGUI nameTextMeshPro))
            {
                nameTextMeshPro.text = Name;
            }

            if (inputField.TryGetComponentOrLog(out PointerHelper pointerHelper))
            {
                pointerHelper.EnterEvent.AddListener(_ => Tooltip.ActivateWith(Description));
                pointerHelper.ExitEvent.AddListener(_ => Tooltip.Deactivate());
            }
        }

        public override void SetParent(GameObject parent)
        {
            if (inputField != null)
            {
                inputField.transform.SetParent(parent.transform);
            }
            else
            {
                /// save for later assignment in <see cref="StartDesktop"/>
                parentOfInputField = parent;
            }
        }
    }
}
