// Copyright 2020 Robert Bohnsack
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using SEE.Controls;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SEE.Game.Charts
{
    /// <summary>
    /// Persistent data, containing relevant information about a
    /// <see cref="ScrollViewEntry"/>, that must not be removed, while the chart is
    /// opened.
    /// </summary>
    public struct ScrollViewEntryData
    {
        /// <summary>
        /// (Un)subscribes from/to events for the <see cref="ScrollViewEntryData"/>, as
        /// that is a struct and thus can not properly unsubscribe from an
        /// event, as it is copied and thus not identical.
        /// </summary>
        private class EventHandler
        {
            private readonly ChartContent chartContent;
            private readonly int index;                             // Unique index of the entry in the chart content
            private readonly InteractableObject interactableObject; // The object, whose events are subscribed to

            /// <summary>
            /// The color the label of the scroll view entry
            /// </summary>
            private Color originalLabelColor;

            /// <param name="index">The unique index of the <see cref="ScrollViewEntryData"/> within the <see cref="chartContent"/>.</param>
            internal EventHandler(ChartContent chartContent, int index, InteractableObject interactableObject)
            {
                this.chartContent = chartContent;
                this.index = index;
                this.interactableObject = interactableObject;

                if (this.interactableObject)
                {
                    this.interactableObject.HoverIn += OnHover;
                    this.interactableObject.HoverOut += OnUnhover;
                    this.interactableObject.SelectIn += OnSelect;
                    this.interactableObject.SelectOut += OnUnselect;

                    if (this.interactableObject.IsSelected)
                    {
                        OnSelect();
                    }
                    else if (this.interactableObject.IsHovered)
                    {
                        OnHover();
                    }
                }
            }

            #region InteractableObject Events

            /// <summary>
            /// Sets the color on hovering.
            /// </summary>
            /// <param name="_0">Unused.</param>
            /// <param name="_1">Unused.</param>
            internal void OnHover(InteractableObject _0 = null, bool _1 = true)
            {
                if (interactableObject == null || !interactableObject.IsSelected)
                {
                    ScrollViewEntry entry = chartContent.GetScrollViewEntry(index);
                    if (entry != null)
                    {
                        const int colorIndex = 1;

                        Color color = UIColorScheme.GetLight(colorIndex);
                        // Store old colors
                        originalLabelColor = entry.Label.color;
                        // Set new colors
                        ColorBlock colors = entry.UIToggle.colors;
                        colors.normalColor = color;
                        entry.Label.color = color;
                        entry.UIToggle.colors = colors;
                    }
                }
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="_0">Unused.</param>
            /// <param name="_1">Unused.</param>
            internal void OnUnhover(InteractableObject _0 = null, bool _1 = true)
            {
                if (interactableObject == null || !interactableObject.IsSelected)
                {
                    ScrollViewEntry entry = chartContent.GetScrollViewEntry(index);
                    if (entry != null)
                    {
                        entry.Label.color = originalLabelColor;
                        ColorBlock block = entry.UIToggle.colors;
                        block.normalColor = originalLabelColor;
                        entry.UIToggle.colors = block;
                    }
                }
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="_0">Unused.</param>
            /// <param name="_1">Unused.</param>
            private void OnSelect(InteractableObject _0 = null, bool _1 = true)
            {
                ScrollViewEntry entry = chartContent.GetScrollViewEntry(index);
                if (entry != null)
                {
                    const int colorIndex = 2;

                    Color color = UIColorScheme.GetLight(colorIndex);
                    ColorBlock colors = entry.UIToggle.colors;
                    colors.normalColor = color;
                    entry.UIToggle.colors = colors;
                    entry.Label.color = color;
                }
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="o">The unselected interactable object.</param>
            /// <param name="_1">Unused.</param>
            private void OnUnselect(InteractableObject o, bool _1 = true)
            {
                ScrollViewEntry entry = chartContent.GetScrollViewEntry(index);
                if (entry != null)
                {
                    int colorIndex = o.IsHovered ? 1 : 0;

                    Color color = UIColorScheme.GetLight(colorIndex);
                    UnityEngine.UI.ColorBlock colors = entry.UIToggle.colors;
                    colors.normalColor = color;
                    entry.UIToggle.colors = colors;
                    entry.Label.color = color;
                }
            }

            #endregion

            internal void OnDestroy()
            {
                if (interactableObject)
                {
                    interactableObject.HoverIn -= OnHover;
                    interactableObject.HoverOut -= OnUnhover;
                    interactableObject.SelectIn -= OnSelect;
                    interactableObject.SelectOut -= OnUnselect;
                }
            }
        }

        public const int NoParentIndex = -1; // The index for an entry without a parent (root)

        /// <summary>
        /// Unique index within the chart content. Every
        /// <see cref="ScrollViewEntryData"/> gets assigned a unique index, so that
        /// elements can be retrieved fast through <see cref="ChartContent"/>. This index
        /// always equals the index of corresponding <see cref="ScrollViewEntry"/>, if it
        /// currently exists. @UniqueIndexWithingChartContent
        /// </summary>
        internal readonly int Index;
        internal readonly ChartContent ChartContent;
        internal readonly InteractableObject InteractableObject;
        internal readonly int ParentIndex;                       // <see cref="ScrollViewEntryData.NoParentIndex"/>, if this is a root
        internal readonly int[] ChildIndices;                    // <code>null</code>, if this has no children
        internal bool IsOn;                                      // Whether the toggle of the entry is turned on

        private readonly EventHandler eventHandler;            // Handles the events, as this is a struct
        private static bool toggleParents = true;              // Used for propagation of <see cref="isOn"/> for parents/children
        private static bool toggleChildren = true;             // Used for propagation of <see cref="isOn"/> for parents/children

        /// <param name="index">The unique index within <see cref="ChartContent"/>.</param>
        /// <param name="parentIndex">The unique index of the parent within <see cref="ChartContent"/> or <see cref="NoParentIndex"/>, if this is a root.</param>
        /// <param name="childCount">The number of child-indices of this nodes' entry.</param>
        public ScrollViewEntryData(int index, ChartContent chartContent, InteractableObject interactableObject, int parentIndex, int childCount)
        {
            Assert.IsTrue(index >= 0);
            Assert.IsNotNull(chartContent);
            Assert.IsTrue(childCount >= 0);

            this.Index = index;
            this.ChartContent = chartContent;
            this.InteractableObject = interactableObject;
            this.ParentIndex = parentIndex;
            ChildIndices = childCount == 0 ? null : new int[childCount];
            IsOn = true;

            eventHandler = new EventHandler(this.ChartContent, this.Index, this.InteractableObject);
        }

        internal void OnDestroy()
        {
            if (eventHandler != null)
            {
                eventHandler.OnDestroy();
            }
        }

        /// <summary>
        /// Called by <see cref="ScrollViewEntry.UIToggle"/> through Unity, if the toggle button was clicked.
        /// </summary>
        internal void Toggle(bool value)
        {
            IsOn = value;

            if (InteractableObject != null)
            {
                ChartContent.SetShowInChart(InteractableObject, IsOn);
            }

            // Propagate changes through parents
            if (toggleParents && ParentIndex != NoParentIndex)
            {
                bool resetToggleChildren = toggleChildren;
                toggleChildren = false;

                ScrollViewEntry parent = ChartContent.GetScrollViewEntry(ParentIndex);
                ref ScrollViewEntryData parentData = ref ChartContent.GetScrollViewEntryData(ParentIndex);
                if (parentData.IsOn)
                {
                    if (!IsOn)
                    {
                        if (parent)
                        {
                            parent.UIToggle.isOn = false;
                        }
                        else
                        {
                            parentData.Toggle(false);
                        }
                    }
                    else if (parentData.ChildIndices != null)
                    {
                        foreach (int childIdx in parentData.ChildIndices)
                        {
                            ref ScrollViewEntryData childData = ref ChartContent.GetScrollViewEntryData(childIdx);
                            if (!ChartContent.ShowInChart(childData.InteractableObject))
                            {
                                // Note: This automatically calls Toggle() for the
                                // parent's ScrollViewEntry. As 'toggleChildren' is
                                // 'false', this propagates only through the parents and
                                // not back down the children.
                                if (parent)
                                {
                                    parent.UIToggle.isOn = false;
                                }
                                else
                                {
                                    parentData.Toggle(false);
                                }
                                break;
                            }
                        }
                    }
                }
                else
                {
                    bool activate = true;
                    if (parentData.ChildIndices != null)
                    {
                        foreach (int siblingIdx in parentData.ChildIndices)
                        {
                            ref ScrollViewEntryData siblingData = ref ChartContent.GetScrollViewEntryData(siblingIdx);
                            if (!ChartContent.ShowInChart(siblingData.InteractableObject))
                            {
                                activate = false;
                                break;
                            }
                        }
                    }
                    if (activate)
                    {
                        if (parent)
                        {
                            // Note: This automatically calls Toggle() for the parent's
                            // ScrollViewEntry. As 'toggleChildren' is 'false', this
                            // propagates only through the parents and not back down the
                            // children. @AutoToggle
                            parent.UIToggle.isOn = true;
                        }
                        else
                        {
                            parentData.Toggle(true);
                        }
                    }
                }

                if (resetToggleChildren)
                {
                    toggleChildren = true;
                }
            }

            // Propagate changes through children
            if (toggleChildren && ChildIndices != null)
            {
                bool resetToggleParents = toggleParents;
                toggleParents = false;

                foreach (int childIdx in ChildIndices)
                {
                    ref ScrollViewEntryData childData = ref ChartContent.GetScrollViewEntryData(childIdx);
                    if (childData.IsOn != IsOn)
                    {
                        ScrollViewEntry child = ChartContent.GetScrollViewEntry(childIdx);
                        if (child)
                        {
                            // See: @AutoToggle
                            child.UIToggle.isOn = IsOn;
                        }
                        else
                        {
                            childData.Toggle(IsOn);
                        }
                    }
                }

                if (resetToggleParents)
                {
                    toggleParents = true;
                }
            }
        }

        /// <summary>
        /// Called by <see cref="ScrollViewEntry.OnPointerEnter(PointerEventData)"/> and
        /// <see cref="ScrollViewEntry.OnPointerExit(PointerEventData)"/> through Unity,
        /// if the input device hovers over the entry. Updates the hover flags of this
        /// handled <see cref="Controls.InteractableObject"/>, depending on the value of
        /// <see cref="enter"/>.
        /// </summary>
        internal readonly void OnPointerEvent(bool enter)
        {
            if (InteractableObject != null)
            {
                if (InteractableObject.IsHovered != enter)
                {
                    InteractableObject.SetHoverFlag(HoverFlag.ChartScrollViewToggle, enter, true);
                }
            }
            else if (eventHandler != null)
            {
                if (enter)
                {
                    eventHandler.OnHover();
                }
                else
                {
                    eventHandler.OnUnhover();
                }
            }
        }
    }

    /// <summary>
    /// The scroll view entry handles the visuals of an entry within the chart. As not
    /// every entry is visible at all times to increase performance, the main data is
    /// kept within a <see cref="ScrollViewEntryData"/> object with the same ID as
    /// <see cref="index"/>. The corresponding data object always exist and can be
    /// retrieved via <see cref="ChartContent.GetScrollViewEntryData(int)"/>.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Toggle))]
    public class ScrollViewEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private ChartContent chartContent;
        private int index;                 // See: @UniqueIndexWithingChartContent

        [SerializeField] public TMPro.TextMeshProUGUI Label;  // This text field displays the label of the entry
        [SerializeField] public UnityEngine.UI.Toggle UIToggle; // This toggle hints, whether the corresponding marker should be enabled.

        /// <summary>
        /// Sets <see cref="Label"/> and <see cref="UIToggle"/>.
        /// </summary>
        private void Awake()
        {
            // The following code must be run in Awake(). Start() would be too late.

            // toggle is another component attached to the same game object as this ScrollViewEntry
            if (!TryGetComponent(out UIToggle))
            {
                Debug.LogError($"ScrollViewEntry {name} of has no Toggle.\n");
            }
            // label is a child of the game object this ScrollViewEntry is attached to
            Label = GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (Label == null)
            {
                Debug.LogError($"ScrollViewEntry of {name} of has no child with a TMPro.TextMeshProUGUI component.\n");
            }
        }

        public void Init(ChartContent chartContent, ScrollViewEntryData data, string label)
        {
            this.chartContent = chartContent;
            index = data.Index;
            this.Label.text = label;
            UIToggle.SetIsOnWithoutNotify(data.IsOn);
        }

        public void OnDestroy()
        {
            OnPointerExit(null);
#if UNITY_EDITOR
            UIToggle?.SetIsOnWithoutNotify(true);
            Label.text = "Pooled ScrollViewEntry, previously: " + Label.text;
            index = 0;
            chartContent = null;
#endif
        }

        #region Unity Events

        /// <summary>
        /// Called by Unity, if the toggle-button is clicked.
        /// </summary>
        public void Toggle()
        {
            chartContent.GetScrollViewEntryData(index).Toggle(UIToggle.isOn);
        }

        /// <summary>
        /// Called by Unity, if the entry is hovered.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            try {
                ref ScrollViewEntryData data = ref chartContent.GetScrollViewEntryData(index);
                data.OnPointerEvent(true);
            }
            catch
            {
                Destroyer.Destroy(this);
            }
        }

        /// <summary>
        /// Called by Unity, if the entry is not hovered anymore.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            try
            {
                ref ScrollViewEntryData data = ref chartContent.GetScrollViewEntryData(index);
                data.OnPointerEvent(false);
            }
            catch
            {
                Destroyer.Destroy(this);
            }
        }

        #endregion

    }
}
