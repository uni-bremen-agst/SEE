using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SEE.UI.PropertyDialog
{
    /// <summary>
    /// A button for a property dialog.
    /// </summary>
    public class ButtonProperty : Property<HideModeSelector>
    {
        /// <summary>
        /// The prefab for a button.
        /// </summary>
        private const string buttonPrefab = "Prefabs/UI/Button";

        /// <summary>
        /// EventFunction that is triggered when the button is pressed.
        /// </summary>
        public readonly UnityEvent OnSelected = new();

        /// <summary>
        /// Instantiation of the prefab <see cref="buttonPrefab"/>.
        /// </summary>
        private GameObject button;

        /// <summary>
        /// The codepoint of the icon for the button.
        /// </summary>
        public char Icon;

        /// <summary>
        /// Saves which method of the hide action is to be executed.
        /// </summary>
        public HideModeSelector HideMode;

        /// <summary>
        /// Saves whether the button represents a function of the Hide action that can select several or only one element.
        /// </summary>
        public HideModeSelector SelectionType;

        /// <summary>
        /// Used to set the color of the button.
        /// </summary>
        public Color ButtonColor;

        /// <summary>
        /// Value of the input field.
        /// </summary>
        public override HideModeSelector Value
        {
            get => HideMode;
            set => HideMode = value;
        }

        /// <summary>
        /// The parent of the Button. Because <see cref="SetParent(GameObject)"/>
        /// may be called before <see cref="StartDesktop"/>, the parameter passed to
        /// <see cref="SetParent(GameObject)"/> will be buffered in this attribute.
        /// </summary>
        private GameObject parent;

        /// <summary>
        /// Sets <see cref="button"/> as an instantiation of prefab <see cref="buttonPrefab"/>.
        /// Sets the label and value of the field.
        /// </summary>
        protected override void StartDesktop()
        {
            button = PrefabInstantiator.InstantiatePrefab(buttonPrefab, instantiateInWorldSpace: false);

            if (parent != null)
            {
                SetParent(parent);
            }

            SetupTooltip();
            SetUpButton();
            return;

            void SetUpButton()
            {
                button.name = Name;
                GameObject text = button.transform.Find("Text").gameObject;
                GameObject icon = button.transform.Find("Icon").gameObject;

                if (!button.TryGetComponentOrLog(out ButtonManagerBasic buttonManager)
                     || !button.TryGetComponentOrLog(out Image buttonImage)
                     || !text.TryGetComponentOrLog(out TextMeshProUGUI textMeshPro)
                     || !icon.TryGetComponentOrLog(out TextMeshProUGUI iconText)
                     || !button.TryGetComponentOrLog(out PointerHelper pointerHelper))
                {
                    return;
                }

                textMeshPro.fontSize = 20;
                buttonImage.color = ButtonColor;
                textMeshPro.color = ButtonColor.IdealTextColor();
                iconText.color = ButtonColor.IdealTextColor();
                iconText.text = Icon.ToString();

                buttonManager.buttonText = Name;
                buttonManager.clickEvent.AddListener(Clicked);
                pointerHelper.EnterEvent.AddListener(_ => Tooltip.ActivateWith(Description));
                pointerHelper.ExitEvent.AddListener(_ => Tooltip.Deactivate());
            }

            void Clicked()
            {
                OnSelected.Invoke();
            }
        }

        /// <summary>
        /// Sets up the tooltips for the button.
        /// </summary>
        /// <param name="button">The object to which the tooltip is to be attached.</param>
        private void SetupTooltip()
        {
            if (button.TryGetComponentOrLog(out PointerHelper pointerHelper))
            {
                // Register listeners on entry and exit events, respectively
                pointerHelper.EnterEvent.AddListener(_ => Tooltip.ActivateWith(Description));
                pointerHelper.ExitEvent.AddListener(_ => Tooltip.Deactivate());
            }
        }

        /// <summary>
        /// Sets <paramref name="parent"/> as the parent of the <see cref="inputField"/>.
        /// </summary>
        /// <param name="parent">New parent of <see cref="inputField"/>.</param>
        public override void SetParent(GameObject parent)
        {
            if (button != null)
            {
                button.transform.SetParent(parent.transform);
            }
            else
            {
                /// save for later assignment in <see cref="StartDesktop"/>
                this.parent = parent;
            }
        }
    }
}
