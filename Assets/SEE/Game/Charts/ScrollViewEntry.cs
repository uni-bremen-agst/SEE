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
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace SEE.Game.Charts
{
    public struct ScrollViewEntryData
    {
        private class EventHandler
        {
            private readonly ChartContent chartContent;
            private readonly int index;
            private readonly InteractableObject interactableObject;

            /// <summary>
            /// Sadly, since you can not properly unsubscribe from an event with a
            /// member-function of a struct, this class is necessary to handle the event
            /// subscriptions.
            /// </summary>
            /// <param name="chartContent">The chart content.</param>
            /// <param name="index">The index of the <see cref="ScrollViewEntryData"/>.
            /// </param>
            /// <param name="interactableObject">The interactable object.</param>
            internal EventHandler(ChartContent chartContent, int index, InteractableObject interactableObject)
            {
                this.chartContent = chartContent;
                this.index = index;
                this.interactableObject = interactableObject;

                if (this.interactableObject)
                {
                    this.interactableObject.HoverIn += OnHoverOrSelect;
                    this.interactableObject.HoverOut += OnHoverOrSelect;
                    this.interactableObject.SelectIn += OnHoverOrSelect;
                    this.interactableObject.SelectOut += OnHoverOrSelect;

                    if (this.interactableObject.IsHovered)
                    {
                        OnHoverOrSelect(this.interactableObject, true); // TODO(torben): cache address of owner in InteractableObject
                    }
                    if (this.interactableObject.IsSelected)
                    {
                        OnHoverOrSelect(this.interactableObject, true); // TODO(torben): cache address of owner in InteractableObject
                    }
                }
            }

            internal void OnHoverOrSelect(InteractableObject o, bool isOwner)
            {
                ScrollViewEntry entry = chartContent.GetScrollViewEntry(index);
                if (entry != null)
                {
                    int colorIndex = 0;
                    if (o != null)
                    {
                        if (o.IsSelected)
                        {
                            colorIndex = 2;
                        }
                        else if (o.IsHovered)
                        {
                            colorIndex = 1;
                        }
                    }

                    Color color = UIColorScheme.GetLight(colorIndex);
                    UnityEngine.UI.ColorBlock colors = entry.toggle.colors;
                    colors.normalColor = color;
                    entry.toggle.colors = colors;
                    entry.label.color = color;
                }
            }

            internal void Destroy()
            {
                if (interactableObject)
                {
                    interactableObject.HoverIn -= OnHoverOrSelect;
                    interactableObject.HoverOut -= OnHoverOrSelect;
                    interactableObject.SelectIn -= OnHoverOrSelect;
                    interactableObject.SelectOut -= OnHoverOrSelect;
                }
            }
        }

        public const int InvalidIndex = -1;

        public readonly int index; // equals the index of corresponding ScrollViewEntry
        public readonly ChartContent chartContent;
        public readonly InteractableObject interactableObject;
        public readonly int parentIndex;
        public readonly int[] childIndices;
        public bool isOn;

        private EventHandler eventHandler;
        private static bool toggleParents = true;
        private static bool toggleChildren = true;

        public ScrollViewEntryData(
            int index,
            ChartContent chartContent,
            InteractableObject interactableObject,
            int parentIndex,
            int childIndexCount,
            bool isOn = true
        )
        {
            Assert.IsTrue(index >= 0);
            Assert.IsNotNull(chartContent);
            Assert.IsTrue(childIndexCount >= 0);

            this.index = index;
            this.chartContent = chartContent;
            this.interactableObject = interactableObject;
            this.parentIndex = parentIndex;
            childIndices = childIndexCount == 0 ? null : new int[childIndexCount];
            this.isOn = isOn;

            eventHandler = new EventHandler(this.chartContent, this.index, this.interactableObject);
        }

        internal void Destroy()
        {
            if (eventHandler != null)
            {
                eventHandler.Destroy();
            }
        }

        internal void Toggle(bool value)
        {
            isOn = value;

            if (interactableObject != null)
            {
                chartContent.SetShowInChart(interactableObject, isOn);
            }

            // Propagate changes through parents
            if (toggleParents && parentIndex != InvalidIndex)
            {
                bool resetToggleChildren = toggleChildren;
                toggleChildren = false;

                ScrollViewEntry parent = chartContent.GetScrollViewEntry(parentIndex);
                ref ScrollViewEntryData parentData = ref chartContent.GetScrollViewEntryData(parentIndex);
                if (parentData.isOn)
                {
                    if (!isOn)
                    {
                        if (parent)
                        {
                            parent.toggle.isOn = false;
                        }
                        else
                        {
                            parentData.Toggle(false);
                        }
                    }
                    else if (parentData.childIndices != null)
                    {
                        foreach (int childIdx in parentData.childIndices)
                        {
                            ref ScrollViewEntryData childData = ref chartContent.GetScrollViewEntryData(childIdx);
                            if (!chartContent.ShowInChart(childData.interactableObject))
                            {
                                // Note: This automatically calls Toggle() for the
                                // parent's ScrollViewEntry. As 'toggleChildren' is
                                // 'false', this propagates only through the parents and
                                // not back down the children.
                                if (parent)
                                {
                                    parent.toggle.isOn = false;
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
                    if (parentData.childIndices != null)
                    {
                        foreach (int siblingIdx in parentData.childIndices)
                        {
                            ref ScrollViewEntryData siblingData = ref chartContent.GetScrollViewEntryData(siblingIdx);
                            if (!chartContent.ShowInChart(siblingData.interactableObject))
                            {
                                activate = false;
                                break;
                            }
                        }
                    }
                    if (activate)
                    {
                        // Note: This automatically calls Toggle() for the parent's
                        // ScrollViewEntry. As 'toggleChildren' is 'false', this
                        // propagates only through the parents and not back down the
                        // children.
                        if (parent)
                        {
                            parent.toggle.isOn = true;
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
            if (toggleChildren && childIndices != null)
            {
                bool resetToggleParents = toggleParents;
                toggleParents = false;

                foreach (int childIdx in childIndices)
                {
                    ref ScrollViewEntryData childData = ref chartContent.GetScrollViewEntryData(childIdx);
                    if (childData.isOn != isOn)
                    {
                        // Note: This automatically calls Toggle() for the child's
                        // ScrollViewToggle. As 'toggleParents' is 'false', this
                        // propagates only through the children and not back up the
                        // parents.
                        ScrollViewEntry child = chartContent.GetScrollViewEntry(childIdx);
                        if (child)
                        {
                            child.toggle.isOn = isOn;
                        }
                        else
                        {
                            childData.Toggle(isOn);
                        }
                    }
                }

                if (resetToggleParents)
                {
                    toggleParents = true;
                }
            }
        }

        internal void OnPointerEvent(bool enter)
        {
            if (interactableObject != null && interactableObject.IsHovered != enter)
            {
                interactableObject.SetHoverFlag(HoverFlag.ChartScrollViewToggle, enter, true);
            }
        }
    }

    [RequireComponent(typeof(UnityEngine.UI.Toggle))]
    public class ScrollViewEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private ChartContent chartContent;
        private int index; // equals the index of corresponding ScrollViewEntryData

        [SerializeField] public TMPro.TextMeshProUGUI label;
        [SerializeField] public UnityEngine.UI.Toggle toggle;

        public void Init(ChartContent chartContent, ref ScrollViewEntryData data, string label)
        {
            this.chartContent = chartContent;
            index = data.index;
            this.label.text = label;
            toggle.SetIsOnWithoutNotify(data.isOn);
        }

        public void OnDestroy()
        {
            OnPointerExit(null);
#if UNITY_EDITOR
            toggle.SetIsOnWithoutNotify(true);
            label.text = "Pooled ScrollViewEntry, previously: " + label.text;
            index = ScrollViewEntryData.InvalidIndex;
            chartContent = null;
#endif
        }

        public void Toggle()
        {
            chartContent.GetScrollViewEntryData(index).Toggle(toggle.isOn);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ref ScrollViewEntryData data = ref chartContent.GetScrollViewEntryData(index);
            data.OnPointerEvent(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ref ScrollViewEntryData data = ref chartContent.GetScrollViewEntryData(index);
            data.OnPointerEvent(false);
        }
    }
}