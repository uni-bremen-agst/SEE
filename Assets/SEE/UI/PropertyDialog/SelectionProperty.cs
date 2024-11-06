using System;
using System.Collections.Generic;
using Michsky.UI.ModernUIPack;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.PropertyDialog
{
    /// <summary>
    /// A selector for a fixed set of strings for a property dialog.
    /// </summary>
    public class SelectionProperty : Property<string>
    {
        /// <summary>
        /// Adds options to the list of options.
        /// </summary>
        /// <param name="options">additional options</param>
        public void AddOptions(IEnumerable<string> options)
        {
            this.options.AddRange(options);
        }

        /// <summary>
        /// The list of options.
        /// </summary>
        private readonly List<string> options = new();

        /// <summary>
        /// The prefab for a string input field.
        /// </summary>
        private const string inputFieldPrefab = "Prefabs/UI/InputFields/SelectionInputField";

        /// <summary>
        /// Instantiation of the prefab <see cref="inputFieldPrefab"/>.
        /// </summary>
        private GameObject inputField;

        /// <summary>
        /// The parent of <see cref="inputField"/>. Because <see cref="SetParent(GameObject)"/>
        /// may be called before <see cref="StartDesktop"/>, the parameter passed to
        /// <see cref="SetParent(GameObject)"/> will be buffered in this attribute.
        /// </summary>
        private GameObject parentOfInputField;

        /// <summary>
        /// The horizontal selector.
        /// </summary>
        public HorizontalSelector HorizontalSelector { get; private set; }

        /// <summary>
        /// Sets <see cref="inputField"/> as an instantiation of prefab <see cref="inputFieldPrefab"/>.
        /// Sets the label and value of the field.
        /// </summary>
        protected override void StartDesktop()
        {
            inputField = PrefabInstantiator.InstantiatePrefab(inputFieldPrefab, instantiateInWorldSpace: false);
            if (parentOfInputField != null)
            {
                SetParent(parentOfInputField);
            }
            inputField.name = Name;
            HorizontalSelector = GetHorizontalSelector(inputField);
            SetOptions(HorizontalSelector, options);
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
                if (!field.TryGetComponent(out PointerHelper pointerHelper))
                {
                    pointerHelper = field.AddComponent<PointerHelper>();
                }
                // Register listeners on entry and exit events, respectively
                pointerHelper.EnterEvent.AddListener(_ => Tooltip.ActivateWith(Description));
                pointerHelper.ExitEvent.AddListener(_ => Tooltip.Deactivate());
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
                    throw new Exception($"Prefab {inputFieldPrefab} does not have a {typeof(HorizontalSelector)}");
                }
            }

            #endregion
        }

        protected override void StartVR()
        {
            StartDesktop();
        }

        /// <summary>
        /// Sets <paramref name="parent"/> as the parent of the <see cref="inputField"/>.
        /// </summary>
        /// <param name="parent">new parent of <see cref="inputField"/></param>
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

        /// <summary>
        /// The buffered selected value. Because <see cref="Value"/> may be set before
        /// <see cref="StartDesktop"/> is called, the parameter passed to
        /// <see cref="Value"/> will be buffered in this attribute if <see cref="StartDesktop"/>
        /// has not been called and, hence, <see cref="HorizontalSelector"/> does not exist yet.
        /// </summary>
        private string savedValue;

        /// <summary>
        /// Moves the selector to the <see cref="savedValue"/>.
        /// Assumption: <see cref="HorizontalSelector"/> is not null.
        /// </summary>
        /// <exception cref="Exception">thrown in case <see cref="savedValue"/> is not
        /// contained in <see cref="options"/></exception>
        public override void GetReady()
        {
            UnityEngine.Assertions.Assert.IsNotNull(HorizontalSelector);
            if (savedValue != options[HorizontalSelector.index])
            {
                MoveToSelection(savedValue);
            }
        }

        /// <summary>
        /// Value of the input field.
        /// </summary>
        public override string Value
        {
            get => HorizontalSelector == null ? savedValue : options[HorizontalSelector.index];
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
            if (HorizontalSelector != null)
            {
                int targetIndex = GetIndex(option);
                int currentIndex = HorizontalSelector.index;
                if (targetIndex < currentIndex)
                {
                    for (int i = currentIndex - targetIndex; i >= 1; i--)
                    {
                        HorizontalSelector.PreviousClick();
                    }
                }
                else if (targetIndex > currentIndex)
                {
                    for (int i = targetIndex - currentIndex; i >= 1; i--)
                    {
                        HorizontalSelector.ForwardClick();
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
