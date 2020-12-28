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
using SEE.GO;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SEE.Game.Charts
{
    /// <summary>
    /// Contains the logic for entries in the content selection scroll view.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class ScrollViewToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// Contains the name of the <see cref="LinkedObject"/> in the UI.
        /// </summary>
        [SerializeField] private TextMeshProUGUI label;

        /// <summary>
        /// The UI element the user can click on to change the state of
        /// <see cref="UnityEngine.UI.Toggle.isOn"/> and toggle some
        /// <see cref="ChartMarker"/>s.
        /// </summary>
        [SerializeField] private Toggle toggle;

        /// <summary>
        /// The parent to this <see cref="ScrollViewToggle"/>.
        /// </summary>
        public ScrollViewToggle Parent { private get; set; }

        /// <summary>
        /// Contains all children to this <see cref="ScrollViewToggle"/>.
        /// </summary>
        private readonly List<ScrollViewToggle> children = new List<ScrollViewToggle>();

        /// <summary>
        /// The chart content of the chart, this scroll view toggle is attached to.
        /// </summary>
        private ChartContent chartContent;

        /// <summary>
        /// Used for propagating the activeness of scroll view toggles through the
        /// parents and children. If a toggle is enabled/disabled, parents/children may
        /// also need to be enabled/disabled.
        /// </summary>
        private static bool toggleParents = true;

        /// <summary>
        /// <see cref="toggleParents"/>
        /// </summary>
        private static bool toggleChildren = true;

        private InteractableObject linkedInteractable;

        /// <summary>
        /// Contains properties for adding objects to charts.
        /// </summary>
        private NodeHighlights linkedObject;

        /// <summary>
        /// Sets the <see cref="linkedObject"/> and the <see cref="linkedInteractable"/>
        /// of this toggle. Also updates event callbacks.
        /// </summary>
        public NodeHighlights LinkedObject
        {
            get
            {
                return linkedObject;
            }
            set
            {
                if (linkedInteractable)
                {
                    linkedInteractable.HoverIn -= OnHoverOrSelect;
                    linkedInteractable.HoverOut -= OnHoverOrSelect;
                    linkedInteractable.SelectIn -= OnHoverOrSelect;
                    linkedInteractable.SelectOut -= OnHoverOrSelect;
                }

                linkedObject = value;
                if (linkedObject && linkedObject.TryGetComponent(out InteractableObject interactableObj))
                {
                    linkedInteractable = interactableObj;
                    linkedInteractable.HoverIn += OnHoverOrSelect;
                    linkedInteractable.HoverOut += OnHoverOrSelect;
                    linkedInteractable.SelectIn  += OnHoverOrSelect;
                    linkedInteractable.SelectOut += OnHoverOrSelect;
                }
                else
                {
                    linkedInteractable = null;
                }
            }
        }

        /// <summary>
        /// Called by <see cref="ChartContent"/> after creation to pass some values and
        /// initialize attributes.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="chartContent">The script to link.</param>
        public void Initialize(string label, ChartContent chartContent)
        {
            this.label.text = label;
            this.chartContent = chartContent;
            toggle.isOn = !Parent || (bool)linkedObject.showInChart[this.chartContent];

            if (linkedInteractable)
            {
                if (linkedInteractable.IsHovered)
                {
                    OnHoverOrSelect(linkedInteractable, true); // TODO(torben): cache address of owner in InteractableObject
                }
                if (linkedInteractable.IsSelected)
                {
                    OnHoverOrSelect(linkedInteractable, true); // TODO(torben): cache address of owner in InteractableObject
                }
            }
        }

        /// <summary>
        /// If the <see cref="GameObject"/> was still pointed on, the highlight of the
        /// <see cref="linkedObject"/> will be stopped.
        /// </summary>
        public void OnDestroy()
        {
            OnPointerExit(null);

            Parent = null;
            children.Clear();
            chartContent = null;
            toggleParents = true;
            toggleChildren = true;
            if (linkedInteractable)
            {
                linkedInteractable.HoverIn -= OnHoverOrSelect;
                linkedInteractable.HoverOut -= OnHoverOrSelect;
                linkedInteractable.SelectIn -= OnHoverOrSelect;
                linkedInteractable.SelectOut -= OnHoverOrSelect;

                linkedInteractable = null;
            }
            linkedObject = null;

            label.text = string.Empty;
            OnHoverOrSelect(null, true); // TODO(torben): is owner should not simply be 'true'
        }

        /// <summary>
        /// Returns the width of the label of the scroll view toggle.
        /// </summary>
        /// <returns>The width of the label.</returns>
        public float GetLabelWidth()
        {
            // TODO(torben): this is too large!
            float result = label.preferredWidth;
            return result;
        }

        /// <summary>
        /// Mainly called by Unity. Activates or deactivates a marker in the linked
        /// chart, depending on the status of <see cref="UnityEngine.UI.Toggle.isOn"/>.
        /// </summary>
        public void Toggle()
        {
            if (linkedObject)
            {
                linkedObject.showInChart[chartContent] = toggle.isOn;
                NodeRef nodeRef = linkedInteractable.GetComponent<NodeRef>();
                if (nodeRef && chartContent.nodeRefToChartMarkerDict.TryGetValue(nodeRef, out ChartMarker chartMarker))
                {
                    chartMarker.UpdateInfoText();
                    chartMarker.UpdateVisibility();
                }
            }

            // Propagate changes through parents
            if (toggleParents && Parent)
            {
                bool resetToggleChildren = toggleChildren;
                toggleChildren = false;

                if (Parent.toggle.isOn)
                {
                    if (!toggle.isOn)
                    {
                        Parent.toggle.isOn = false;
                    }
                    else
                    {
                        foreach (ScrollViewToggle child in Parent.children)
                        {
                            if (!child.IsLinkedObjectVisible())
                            {
                                // Note: This automatically calls Toggle() for the
                                // parent's ScrollViewToggle. As 'toggleChildren' is
                                // 'false', this propagates only through the parents and
                                // not back down the children.
                                Parent.toggle.isOn = false;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    bool activate = true;
                    foreach (ScrollViewToggle sibling in Parent.children)
                    {
                        if (!sibling.IsLinkedObjectVisible())
                        {
                            activate = false;
                            break;
                        }
                    }
                    if (activate)
                    {
                        // Note: This automatically calls Toggle() for the parent's
                        // ScrollViewToggle. As 'toggleChildren' is 'false', this
                        // propagates only through the parents and not back down the
                        // children.
                        Parent.toggle.isOn = true;
                    }
                }

                if (resetToggleChildren)
                {
                    toggleChildren = true;
                }
            }

            // Propagate changes through children
            if (toggleChildren)
            {
                bool resetToggleParents = toggleParents;
                toggleParents = false;

                foreach (ScrollViewToggle child in children)
                {
                    if (child.toggle.isOn != toggle.isOn)
                    {
                        // Note: This automatically calls Toggle() for the child's
                        // ScrollViewToggle. As 'toggleParents' is 'false', this
                        // propagates only through the children and not back up the
                        // parents.
                        child.toggle.isOn = toggle.isOn;
                    }
                }

                if (resetToggleParents)
                {
                    toggleParents = true;
                }
            }
        }

        /// <summary>
        /// Whether the linked interactable object is currently visible in the chart.
        /// </summary>
        /// <returns><code>true</code> if the linked interactable object is currently
        /// visible in the chart, <code>false</code> otherwise.</returns>
        private bool IsLinkedObjectVisible()
        {
            return (bool)linkedObject.showInChart[chartContent];
        }

        /// <summary>
        /// Sets the capacity of the number of children of this scroll view toggle. If
        /// the new capacity is not greater than the old capacity, nothing happens.
        /// </summary>
        /// <param name="capacity">The new capacity.</param>
        public void SetChildrenCapacity(int capacity)
        {
            if (capacity > children.Capacity)
            {
                children.Capacity = capacity;
            }
        }

        /// <summary>
        /// Adds a <see cref="ScrollViewToggle"/> as a child of this toggle.
        /// </summary>
        /// <param name="child">The new child.</param>
        public void AddChild(ScrollViewToggle child)
        {
            children.Add(child);
        }

        /// <summary>
        /// Updates the color of the toggle, depending on whether the linked interactable
        /// object is hovered or selected.
        /// </summary>
        public void UpdateColor()
        {
            int colorIndex = 0;
            if (linkedInteractable != null)
            {
                if (linkedInteractable.IsSelected)
                {
                    colorIndex = 2;
                }
                else if (linkedInteractable.IsHovered)
                {
                    colorIndex = 1;
                }
            }
            Color color = UIColorScheme.GetLight(colorIndex);

            ColorBlock colors = toggle.colors;
            colors.normalColor = color;
            toggle.colors = colors;

            label.color = color;
        }

        #region UnityEngine Callbacks

        /// <summary>
        /// Called by Unity, if the mouse hovers over this toggle.
        /// 
        /// Hovers the linked interactable object.
        /// </summary>
        /// <param name="eventData">Ignored.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (linkedObject != null && !linkedInteractable.IsHovered)
            {
                linkedInteractable.SetHoverFlag(HoverFlag.ChartScrollViewToggle, true, true);
            }
        }

        /// <summary>
        /// Called by Unity, if the mouse stops hovering over this toggle.
        /// 
        /// Unhovers the linked interactable object.
        /// </summary>
        /// <param name="eventData">Ignored.</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (linkedInteractable != null && linkedInteractable.IsHovered)
            {
                linkedInteractable.SetHoverFlag(HoverFlag.ChartScrollViewToggle, false, true);
            }
        }

        #endregion

        #region InteractableObject Callbacks

        /// <summary>
        /// Called through events <see cref="InteractableObject.HoverIn"/>,
        /// <see cref="InteractableObject.Hoverout"/>,
        /// <see cref="InteractableObject.SelectIn"/> and
        /// <see cref="InteractableObject.SelectOut"/>.
        /// 
        /// Updates the color of the toggle, depending on whether the linked interactable
        /// object is hovered or selected.
        /// </summary>
        /// <param name="interactableObject">Ignored.</param>
        /// <param name="isOwner">Ignored.</param>
        private void OnHoverOrSelect(InteractableObject interactableObject, bool isOwner) => UpdateColor();

        #endregion
    }
}