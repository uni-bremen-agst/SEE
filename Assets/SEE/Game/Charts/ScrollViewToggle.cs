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
        /// The parent to this <see cref="ScrollViewToggle" />.
        /// </summary>
        public ScrollViewToggle Parent { private get; set; }

        /// <summary>
        /// Contains all children to this <see cref="ScrollViewToggle" />.
        /// </summary>
        private readonly List<ScrollViewToggle> children = new List<ScrollViewToggle>();

        /// <summary>
        /// Contains the name of the <see cref="LinkedObject"/> in the UI.
        /// </summary>
        [SerializeField] private TextMeshProUGUI label;

        /// <summary>
        /// The UI element the user can click on to change the state of
        /// <see cref="UnityEngine.UI.Toggle.isOn"/>.
        /// </summary>
        [SerializeField] private Toggle toggle;

        /// <summary>
        /// The linked chart. Also contains methods to refresh the chart.
        /// </summary>
        private ChartContent chartContent;

        private static bool toggleParents = true;
        private static bool toggleChildren = true;

        /// <summary>
        /// Contains properties for adding objects to charts.
        /// </summary>
        private InteractableObject linkedInteractable;
        private NodeHighlights linkedObject;
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
                    linkedInteractable.HoverIn -= OnHoverIn;
                    linkedInteractable.HoverOut -= OnHoverOut;
                    linkedInteractable.SelectIn -= OnSelectIn;
                    linkedInteractable.SelectOut -= OnSelectOut;
                }

                linkedObject = value;
                if (linkedObject && linkedObject.TryGetComponent(out InteractableObject interactableObj))
                {
                    linkedInteractable = interactableObj;
                    linkedInteractable.HoverIn += OnHoverIn;
                    linkedInteractable.HoverOut += OnHoverOut;
                    linkedInteractable.SelectIn  += OnSelectIn;
                    linkedInteractable.SelectOut += OnSelectOut;
                }
                else
                {
                    linkedInteractable = null;
                }
            }
        }

        /// <summary>
        /// Called by <see cref="ChartContent" /> after creation to pass some values and initialize attributes.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="script">The script to link.</param>
        public void Initialize(string label, ChartContent script)
        {
            this.label.text = label;
            chartContent = script;
            toggle.isOn = !Parent || (bool)linkedObject.showInChart[chartContent];
        }

        /// <summary>
        /// If the <see cref="GameObject" /> was still pointed on, the highlight of the
        /// <see cref="linkedObject"/> will be stopped.
        /// </summary>
        private void OnDestroy()
        {
            OnPointerExit(null);

            if (linkedInteractable)
            {
                linkedInteractable.HoverIn -= OnHoverIn;
                linkedInteractable.HoverOut -= OnHoverOut;
                linkedInteractable.SelectIn -= OnSelectIn;
                linkedInteractable.SelectOut -= OnSelectOut;
            }
        }

        /// <summary>
        /// Mainly called by Unity. Activates or deactivates a marker in the linked chart, depending on the
        /// status of <see cref="UnityEngine.UI.Toggle.isOn" />.
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
                            if (!child.GetStatus())
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
                        if (!sibling.GetStatus())
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
        /// Used to check if a marker for the <see cref="linkedObject" /> will be added to the linked chart.
        /// </summary>
        /// <returns>The status of the <see cref="linkedObject" />.</returns>
        private bool GetStatus()
        {
            return (bool)linkedObject.showInChart[chartContent];
        }

        /// <summary>
        /// Adds a <see cref="ScrollViewToggle" /> as a child of this <see cref="ScrollViewToggle" />.
        /// </summary>
        /// <param name="child">The new child.</param>
        public void AddChild(ScrollViewToggle child)
        {
            children.Add(child);
        }

        public void UpdateColor()
        {
            Color color = UIColorScheme.GetLight(linkedInteractable.IsSelected ? 2 : (linkedInteractable.IsHovered ? 1 : 0));

            ColorBlock colors = toggle.colors;
            colors.normalColor = color;
            toggle.colors = colors;

            label.color = color;
        }

        #region UnityEngine Callbacks

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (linkedObject != null && !linkedInteractable.IsHovered)
            {
                linkedInteractable.SetHoverFlag(HoverFlag.ChartScrollViewToggle, true, true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (linkedInteractable != null && linkedInteractable.IsHovered)
            {
                linkedInteractable.SetHoverFlag(HoverFlag.ChartScrollViewToggle, false, true);
            }
        }

        #endregion

        #region InteractableObject Callbacks

        private void OnHoverIn(InteractableObject interactableObject, bool isOwner) => UpdateColor();
        private void OnHoverOut(InteractableObject interactableObject, bool isOwner) => UpdateColor();
        private void OnSelectIn(InteractableObject interactableObject, bool isOwner) => UpdateColor();
        private void OnSelectOut(InteractableObject interactableObject, bool isOwner) => UpdateColor();

        #endregion
    }
}