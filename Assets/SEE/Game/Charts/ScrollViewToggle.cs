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

using System.Collections;
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
        /// The running <see cref="UpdateStatus" /> <see cref="Coroutine" />.
        /// </summary>
        public Coroutine StatusUpdate { private get; set; }

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
        public NodeHighlights LinkedObject { private get; set; }

        /// <summary>
        /// Called by <see cref="ChartContent" /> after creation to pass some values and initialize attributes.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="script">The script to link.</param>
        public void Initialize(string label, ChartContent script)
        {
            this.label.text = label;
            _chartContent = script;
            toggle.isOn = !Parent || (bool)LinkedObject.showInChart[_chartContent];
        }

        /// <summary>
        /// Mainly called by Unity. Activates or deactivates a marker in the linked chart, depending on the
        /// status of <see cref="UnityEngine.UI.Toggle.isOn" />.
        /// </summary>
        public void Toggle()
        {
            if (Parent == null)
            {
                if (StatusUpdate == null)
                {
                    bool active = toggle.isOn;
                    foreach (ScrollViewToggle child in _children)
                    {
                        child.Toggle(active, true);
                    }
                }
            }
            else
            {
                LinkedObject.showInChart[_chartContent] = toggle.isOn;
                if (Parent.StatusUpdate == null)
                {
                    Parent.StatusUpdate = StartCoroutine(Parent.UpdateStatus());
                }
                if (_chartContent.drawing == null)
                {
                    _chartContent.drawing = StartCoroutine(_chartContent.QueueDraw());
                }
            }
        }

        /// <summary>
        /// Activates or deactivates a marker in the linked chart.
        /// </summary>
        /// <param name="active">If the marker will be activated</param>
        /// <param name="initial"></param>
        public void Toggle(bool active, bool initial)
        {
            toggle.isOn = active;

            if (initial && _children.Count > 0)
            {
                foreach (ScrollViewToggle child in _children)
                {
                    child.Toggle(active, true);
                }
            }
        }

        /// <summary>
        /// Updates the status on parent markers depending on the values of it's children.
        /// </summary>
        /// <returns></returns>
        private IEnumerator UpdateStatus()
        {
            yield return new WaitForSeconds(0.2f);
            bool active = true;
            foreach (ScrollViewToggle child in _children)
            {
                if (!child.GetStatus())
                {
                    Toggle(false, false);
                    active = false;
                    break;
                }
            }

            if (active)
            {
                Toggle(true, true);
            }

            StatusUpdate = null;
        }

        /// <summary>
        /// Used to check if a marker for the <see cref="LinkedObject" /> will be added to the linked chart.
        /// </summary>
        /// <returns>The status of the <see cref="LinkedObject" />.</returns>
        private bool GetStatus()
        {
            return (bool)LinkedObject.showInChart[_chartContent];
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

        /// <summary>
        /// Highlights the <see cref="LinkedObject" /> when the user points on this <see cref="GameObject" />.
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (LinkedObject != null && !_pointedOn)
            {
                _pointedOn = true;
                ChartManager.OnSelect(LinkedObject.gameObject);
            }
        }

        /// <summary>
        /// Stops highlighting the <see cref="LinkedObject" /> when the user stops pointing on it.
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (_pointedOn)
            {
                ChartManager.OnDeselect(LinkedObject.gameObject);
                _pointedOn = false;
            }
        }

        /// <summary>
        /// If the <see cref="GameObject" /> was still pointed on, the highlight of the
        /// <see cref="LinkedObject" /> will be stopped.
        /// </summary>
        private void OnDestroy()
        {
            if (_pointedOn && LinkedObject != null)
            {
                ChartManager.OnSelect(LinkedObject.gameObject);
            }
            StopAllCoroutines();
        }
    }
}