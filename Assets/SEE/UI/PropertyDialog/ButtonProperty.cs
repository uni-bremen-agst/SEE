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
    /// A button for a for a property dialog.
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
        /// Used to store the icon of the button.
        /// </summary>
        public Sprite IconSprite;

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
        /// The tooltip containing the <see cref="description"/> of this <see cref="Property"/>, which will
        /// be displayed when hovering above it.
        /// </summary>
        private Tooltip.Tooltip tooltip;

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

            void SetUpButton()
            {
                button.name = Name;
                GameObject text = button.transform.Find("Text").gameObject;
                GameObject icon = button.transform.Find("Icon").gameObject;

                if (!button.TryGetComponentOrLog(out ButtonManagerBasicWithIcon buttonManager)
                     || !button.TryGetComponentOrLog(out Image buttonImage)
                     || !text.TryGetComponentOrLog(out TextMeshProUGUI textMeshPro)
                     || !icon.TryGetComponentOrLog(out Image iconImage)
                     || !button.TryGetComponentOrLog(out PointerHelper pointerHelper))
                {
                    return;
                }

                textMeshPro.fontSize = 20;
                buttonImage.color = ButtonColor;
                textMeshPro.color = ButtonColor.IdealTextColor();
                iconImage.color = ButtonColor.IdealTextColor();
                iconImage.sprite = IconSprite;

                buttonManager.buttonText = Name;
                buttonManager.clickEvent.AddListener(Clicked);
                pointerHelper.EnterEvent.AddListener(() => tooltip.Show(Description));
                pointerHelper.ExitEvent.AddListener(() => tooltip.Hide());
            }

            void Clicked()
            {
                OnSelected.Invoke();
            }
        }

        /// <summary>
        /// Sets up the tooltips for the button.
        /// </summary>
        /// <param name="button">The object to which the tooltip is to be attached</param>
        private void SetupTooltip()
        {
            tooltip = gameObject.AddComponent<Tooltip.Tooltip>();
            if (button.TryGetComponentOrLog(out PointerHelper pointerHelper))
            {
                // Register listeners on entry and exit events, respectively
                pointerHelper.EnterEvent.AddListener(() => tooltip.Show(Description));
                pointerHelper.ExitEvent.AddListener(tooltip.Hide);
            }
        }

        /// <summary>
        /// Sets <paramref name="parent"/> as the parent of the <see cref="inputField"/>.
        /// </summary>
        /// <param name="parent">new parent of <see cref="inputField"/></param>
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
