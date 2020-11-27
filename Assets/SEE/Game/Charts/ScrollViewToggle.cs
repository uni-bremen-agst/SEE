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
        /// The linked chart. Also contains methods to refresh the chart.
        /// </summary>
        private ChartContent _chartContent;

        /// <summary>
        /// If the user is currently pointing on this <see cref="GameObject" />
        /// </summary>
        private bool _pointedOn;

        /// <summary>
        /// The parent to this <see cref="ScrollViewToggle" />.
        /// </summary>
        public ScrollViewToggle Parent { private get; set; }

        /// <summary>
        /// Contains all children to this <see cref="ScrollViewToggle" />.
        /// </summary>
        private readonly List<ScrollViewToggle> _children = new List<ScrollViewToggle>();

        /// <summary>
        /// Contains the name of the <see cref="LinkedObject" /> in the UI.
        /// </summary>
        [SerializeField] private TextMeshProUGUI label;

        /// <summary>
        /// The UI element the user can click on to change the state of
        /// <see cref="UnityEngine.UI.Toggle.isOn" />.
        /// </summary>
        [SerializeField] private Toggle toggle;

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
                    linkedInteractable.SelectIn  -= OnSelectIn;
                    linkedInteractable.SelectOut -= OnSelectOut;
                }

                linkedObject = value;
                if (linkedObject && linkedObject.TryGetComponent(out InteractableObject interactableObj))
                {
                    linkedInteractable = interactableObj;
                    linkedInteractable.SelectIn  -= OnSelectIn;
                    linkedInteractable.SelectOut -= OnSelectOut;
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
            _chartContent = script;
            toggle.isOn = !Parent || (bool)linkedObject.showInChart[_chartContent];
        }

        /// <summary>
        /// If the <see cref="GameObject" /> was still pointed on, the highlight of the
        /// <see cref="linkedObject"/> will be stopped.
        /// </summary>
        private void OnDestroy()
        {
            OnPointerExit(null);
        }

        /// <summary>
        /// Mainly called by Unity. Activates or deactivates a marker in the linked chart, depending on the
        /// status of <see cref="UnityEngine.UI.Toggle.isOn" />.
        /// </summary>
        public void Toggle()
        {
            linkedObject.showInChart[_chartContent] = toggle.isOn;

            // Propagate changes through parents
            ScrollViewToggle parent = Parent;
            bool deactivate = !toggle.isOn;
            while (parent)
            {
                if (deactivate)
                {
                    parent.toggle.isOn = false;
                }
                else
                {
                    parent.toggle.isOn = true;
                    foreach (ScrollViewToggle child in parent._children)
                    {
                        if (!child.GetStatus())
                        {
                            parent.toggle.isOn = false;
                            deactivate = true;
                            break;
                        }
                    }
                }
                parent = parent.Parent;
            }

            // Propagate changes through children
            Stack<ScrollViewToggle> childStack = new Stack<ScrollViewToggle>(_children.Count);
            foreach (ScrollViewToggle child in _children)
            {
                childStack.Push(child);
            }
            while (childStack.Count > 0)
            {
                ScrollViewToggle child = childStack.Pop();
                if (child.toggle.isOn != toggle.isOn)
                {
                    child.toggle.isOn = toggle.isOn;
                    foreach (ScrollViewToggle c in child._children)
                    {
                        childStack.Push(c);
                    }
                }
            }
        }
          

        /// <summary>
        /// Used to check if a marker for the <see cref="linkedObject" /> will be added to the linked chart.
        /// </summary>
        /// <returns>The status of the <see cref="linkedObject" />.</returns>
        private bool GetStatus()
        {
            return (bool)linkedObject.showInChart[_chartContent];
        }

        /// <summary>
        /// Adds a <see cref="ScrollViewToggle" /> as a child of this <see cref="ScrollViewToggle" />.
        /// </summary>
        /// <param name="child">The new child.</param>
        public void AddChild(ScrollViewToggle child)
        {
            _children.Add(child);
        }

        /// <summary>
        /// Sets the highlight state of this toggle.
        /// </summary>
        /// <param name="highlighted">Highlight on or off.</param>
        public void SetHighlighted(bool highlighted)
        {
            Toggle toggle = GetComponent<Toggle>();
            ColorBlock colors = toggle.colors;
            colors.normalColor = highlighted ? Color.yellow : Color.white;
            toggle.colors = colors;
        }

        #region UnityEngine Callbacks

        /// <summary>
        /// Highlights the <see cref="linkedObject" /> when the user points on this <see cref="GameObject" />.
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_pointedOn && linkedObject != null)
            {
                _pointedOn = true;
                linkedInteractable.SetHover(true, true);
            }
        }

        /// <summary>
        /// Stops highlighting the <see cref="linkedObject" /> when the user stops pointing on it.
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (_pointedOn && linkedObject != null)
            {
                _pointedOn = false;
                linkedInteractable.SetHover(false, true);
            }
        }

        #endregion

        #region InteractableObject Callbacks

        private void OnSelectIn(bool isOwner)
        {
            ColorBlock colors = toggle.colors;
            colors.normalColor = Color.yellow;
            toggle.colors = colors;
        }

        private void OnSelectOut(bool isOwner)
        {
            ColorBlock colors = toggle.colors;
            colors.normalColor = Color.white;
            toggle.colors = colors;
        }

        #endregion
    }
}