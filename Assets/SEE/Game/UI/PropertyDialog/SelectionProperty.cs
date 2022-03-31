using System;
using System.Collections.Generic;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;

namespace SEE.Game.UI.PropertyDialog
{
    /// <summary>
    /// A selector for a fixed set of strings for a property dialog.
    /// </summary>
    public class SelectionProperty : Property<string>
    {

        public void AddOptions(ICollection<string> options)
        {
            this.options.AddRange(options);
        }

        private readonly List<string> options = new List<string>();

        /// <summary>
        /// The prefab for a string input field.
        /// </summary>
        private const string InputFieldPrefab = "Prefabs/UI/InputFields/SelectionInputField";

        /// <summary>
        /// The text field in which the value will be entered by the user.
        /// Note: The input field has a child Text Area/Text with a TextMeshProUGUI
        /// component holding the text, too. Yet, one should never use the latter, because
        /// the latter contains invisible characters. One must always use the attribute
        /// text of the TMP_InputField.
        /// </summary>
        private TMP_InputField textField;

        /// <summary>
        /// Instantiation of the prefab <see cref="InputFieldPrefab"/>.
        /// </summary>
        private GameObject inputField;

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

        private HorizontalSelector horizontalSelector;

        /// <summary>
        /// Sets <see cref="inputField"/> as an instantiation of prefab <see cref="InputFieldPrefab"/>.
        /// Sets the label and value of the field.
        /// </summary>
        protected override void StartDesktop()
        {
            inputField = PrefabInstantiator.InstantiatePrefab(InputFieldPrefab, instantiateInWorldSpace: false);
            if (parentOfInputField != null)
            {
                SetParent(parentOfInputField);
            }
            inputField.gameObject.name = Name;
            horizontalSelector = GetHorizontalSelector(inputField);
            SetOptions(horizontalSelector, options);
            SetupTooltip(inputField);

            #region Local Methods

            static void SetOptions(HorizontalSelector horizontalSelector, IList<string> options)
            {
                foreach (string option in options)
                {
                    horizontalSelector.CreateNewItem(option);
                }
            }

            void SetupTooltip(GameObject field)
            {
                tooltip = gameObject.AddComponent<Tooltip.Tooltip>();
                if (!field.TryGetComponent(out PointerHelper pointerHelper))
                {
                    pointerHelper = field.AddComponent<PointerHelper>();
                }
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

            static HorizontalSelector GetHorizontalSelector(GameObject field)
            {
                if (field.TryGetComponent(out HorizontalSelector horizontalSelector))
                {
                    return horizontalSelector;
                }
                else
                {
                    throw new Exception($"Prefab {InputFieldPrefab} does not have a {typeof(HorizontalSelector)}");
                }
            }

            #endregion
        }

        /// <summary>
        /// Refers to <see cref="StartDesktop"/>
        /// </summary>
        protected override void StartMobile()
        {
            StartDesktop();
        }

        /// <summary>
        /// Sets <paramref name="parent"/> as the parent of the <see cref="inputField"/>.
        /// </summary>
        /// <param name="parent">new parent of <see cref="inputField"/></param>
        public override void SetParent(GameObject parent)
        {
            if (HasStarted)
            {
                inputField.transform.SetParent(parent.transform);
            }
            else
            {
                /// save for later assignment in <see cref="StartDesktop"/>
                parentOfInputField = parent;
            }
        }

        /// <summary>
        /// The buffered selected value. Because <see cref="Value"/> may be set before
        /// <see cref="StartDesktop"/> is called, the parameter passed to
        /// <see cref="Value"/> will be buffered in this attribute if <see cref="StartDesktop"/>
        /// has not been called and, hence, <see cref="horizontalSelector"/> does not exist yet.
        /// </summary>
        private string savedValue;

        /// <summary>
        /// Moves the selector to the <see cref="savedValue"/>.
        /// Assumption: <see cref="horizontalSelector"/> is not null.
        /// </summary>
        /// <exception cref="Exception">thrown in case <see cref="savedValue"/> is not
        /// contained in <see cref="options"/></exception>
        public override void GetReady()
        {
            UnityEngine.Assertions.Assert.IsNotNull(horizontalSelector);
            if (savedValue != options[horizontalSelector.index])
            {
                MoveToSelection(savedValue);
            }
        }

        /// <summary>
        /// Value of the input field.
        /// </summary>
        public override string Value
        {
            get => horizontalSelector == null ? savedValue : options[horizontalSelector.index];
            set
            {
                // Because the Value could be set before StartDesktop() was called,
                // the horizontalSelector might not yet exist. In that case, we are buffering
                // the given value in savedValue.
                savedValue = value;
                MoveToSelection(value);
            }
        }

        /// <summary>
        /// Moves the selector to the given <paramref name="option"/> if the selector
        /// exists already.
        /// </summary>
        /// <param name="option">the option that should be considered selected</param>
        /// <exception cref="Exception">thrown in case  <paramref name="option"/> is not
        /// contained in <see cref="options"/></exception>
        private void MoveToSelection(string option)
        {
            if (horizontalSelector != null)
            {
                int targetIndex = GetIndex(option);
                int currentIndex = horizontalSelector.index;
                if (targetIndex < currentIndex)
                {
                    for (int i = currentIndex - targetIndex; i >= 1; i--)
                    {
                        horizontalSelector.PreviousClick();
                    }
                }
                else if (targetIndex > currentIndex)
                {
                    for (int i = targetIndex - currentIndex; i >= 1; i--)
                    {
                        horizontalSelector.ForwardClick(); ;
                    }
                }
            }

            /// <summary>
            /// Returns the first index i in <see cref="options"/> for which
            /// <see cref="options"/>[i] == <paramref name="value"/> holds.
            /// </summary>
            /// <param name="value">selection option to be searched for</param>
            /// <exception cref="Exception">thrown in case  <paramref name="value"/> is not
            /// contained in <see cref="options"/></exception>
            int GetIndex(string value)
            {
                for (int index = 0; index < options.Count; index++)
                {
                    if (options[index] == value)
                    {
                        return index;
                    }
                }
                throw new Exception($"Invalid selection option {value}.");
            }
        }
    }
}
