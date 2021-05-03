using System;
using System.Collections.Generic;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using SEE.Controls.Actions;
using UnityEngine.Events;

namespace SEE.Game.UI.PropertyDialog
{
    /// <summary>
    /// A string input field for a property dialog.
    /// </summary>
    public class ButtonProperty : Property<HideModeSelector>
    {
        public string content;
        /// <summary>
        /// The prefab for a string input field.
        /// </summary>
        private const string ButtonPrefeb = "Prefabs/UI/Button";

        public readonly UnityEvent OnSelected = new UnityEvent();

        /// <summary>
        /// Instantiation of the prefab <see cref="StringInputFieldPrefab"/>.
        /// </summary>
        private GameObject button;

        public HideModeSelector hideMode;

        /// <summary>
        /// The parent of <see cref="inputField"/>. Because <see cref="SetParent(GameObject)"/>
        /// may be called before <see cref="StartDesktop"/>, the parameter passed to 
        /// <see cref="SetParent(GameObject)"/> will be buffered in this attribute.
        /// </summary>
        private GameObject parentOfInputField;

        /// <summary>
        /// The tooltip containing the <see cref="Description"/> of this <see cref="Property"/>, which will
        /// be displayed when hovering above it.
        /// </summary>
        private Tooltip.Tooltip tooltip;

        /// <summary>
        /// Sets <see cref="inputField"/> as an instantiation of prefab <see cref="StringInputFieldPrefab"/>.
        /// Sets the label and value of the field.
        /// </summary>
        protected override void StartDesktop()
        {
            button = PrefabInstantiator.InstantiatePrefab(ButtonPrefeb, instantiateInWorldSpace: false);

            if (parentOfInputField != null)
            {
                SetParent(parentOfInputField);
            }
            button.gameObject.name = Name;
            SetupTooltip(button);
            SetButtonName(button);





            void SetButtonName(GameObject button)
            {
                GameObject text = button.transform.Find("Text").gameObject;
                GameObject icon = button.transform.Find("Icon").gameObject;


                button.name = Name;
                if (!button.TryGetComponentOrLog(out Michsky.UI.ModernUIPack.ButtonManagerBasicWithIcon buttonManager) ||
                    !button.TryGetComponentOrLog(out PointerHelper pointerHelper))
                {
                    return;
                }

                buttonManager.buttonText = Name;
                buttonManager.clickEvent.AddListener(() => Clicked());
                pointerHelper.EnterEvent.AddListener(() => tooltip.Show(Description));



            }

            void Clicked()
            {
                OnSelected.Invoke();
                //Debug.Log(hideMode); 
            }
            
        }

        void SetupTooltip(GameObject button)
        {
            tooltip = gameObject.AddComponent<Tooltip.Tooltip>();
            if (button.TryGetComponentOrLog(out PointerHelper pointerHelper))
            {
                // Register listeners on entry and exit events, respectively
                pointerHelper.EnterEvent.AddListener(() => tooltip.Show(Description));
                pointerHelper.ExitEvent.AddListener(tooltip.Hide);
                // FIXME scrolling doesn't work while hovering above the field, because
                // the Modern UI Pack uses an Event Trigger (see Utils/PointerHelper for an explanation.)
                // It is unclear how to resolve this without either abstaining from using the Modern UI Pack
                // in this instance or without modifying the Modern UI Pack, which would complicate 
                // updates greatly. Perhaps the author of the Modern UI Pack (or Unity developers?) should
                // be contacted about this.
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
                parentOfInputField = parent;
            }
        }

        /// <summary>
        /// Value of the input field.
        /// </summary>
        public override HideModeSelector Value
        {
            get
            {
                return Value;
            }
            set
            {
                hideMode = value;
            }
        }
    }
}
