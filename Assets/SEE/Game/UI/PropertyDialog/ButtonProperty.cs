using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SEE.Game.UI.PropertyDialog
{
    /// <summary>
    /// A button for a for a property dialog.
    /// </summary>
    public class ButtonProperty : Property<HideModeSelector>
    {
        /// <summary>
        /// The prefab for a button.
        /// </summary>
        private const string ButtonPrefab = "Prefabs/UI/Button";

        /// <summary>
        /// EventFunction that is triggered when the button is pressed.
        /// </summary>
        public readonly UnityEvent OnSelected = new UnityEvent();

        /// <summary>
        /// Instantiation of the prefab <see cref="ButtonPrefab"/>.
        /// </summary>
        private GameObject button;

        /// <summary>
        /// Used to store the icon of the button.
        /// </summary>
        public Sprite iconSprite;

        /// <summary>
        /// Saves which method of the hide action is to be executed.
        /// </summary>
        public HideModeSelector hideMode;

        /// <summary>
        /// Saves whether the button represents a function of the Hide action that can select several or only one element.
        /// </summary>
        public HideModeSelector selectionType;

        /// <summary>
        /// Used to set the color of the button.
        /// </summary>
        public Color buttonColor;

        /// <summary>
        /// Value of the input field.
        /// </summary>
        public override HideModeSelector Value
        {
            get => hideMode;
            set => hideMode = value;
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
        /// Sets <see cref="button"/> as an instantiation of prefab <see cref="ButtonPrefab"/>.
        /// Sets the label and value of the field.
        /// </summary>
        protected override void StartDesktop()
        {
            button = PrefabInstantiator.InstantiatePrefab(ButtonPrefab, instantiateInWorldSpace: false);

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
                buttonImage.color = buttonColor;
                textMeshPro.color = buttonColor.IdealTextColor();
                iconImage.color = buttonColor.IdealTextColor();
                iconImage.sprite = iconSprite;

                buttonManager.buttonText = Name;
                buttonManager.clickEvent.AddListener(Clicked);
                pointerHelper.EnterEvent.AddListener(() => tooltip.Show(Description));
            }

            void Clicked()
            {
                OnSelected.Invoke();
            }
        }

        /// <summary>
        /// Refers to <see cref="StartDesktop"/>
        /// </summary>
        protected override void StartMobile()
        {
            StartDesktop();
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
            if (HasStarted)
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
