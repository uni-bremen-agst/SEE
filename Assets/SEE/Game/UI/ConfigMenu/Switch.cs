// Copyright 2021 Ruben Smidt
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    /// <summary>
    /// The wrapper component for a switch input. A switch input operates on bool values, representing
    /// yes/no, on/off contexts. It comes with a label that is displayed next to the input.
    /// </summary>
    public class Switch : DynamicUIBehaviour
    {
        /// <summary>
        /// The label of this component.
        /// </summary>
        public string Label;

        private TextMeshProUGUI label;
        private SwitchManager switchManager;

        private readonly Queue<bool> valueUpdates = new Queue<bool>();

        /// <summary>
        /// Requests an external value update.
        /// </summary>
        public bool Value
        {
            set => valueUpdates.Enqueue(value);
        }
        private void Start()
        {
            MustGetComponentInChild("Label", out label);
            label.text = Label;

            MustGetComponentInChild("Switch", out switchManager);
            switchManager.OnEvents.AddListener(() => OnValueChange.Invoke(true));
            switchManager.OffEvents.AddListener(() => OnValueChange.Invoke(false));
            // FIXME: Why is ApplyUpdate() called here? Shouldn't Update() be automatically called after Start()?
            ApplyUpdate();
        }
        private void Update()
        {
            ApplyUpdate();
        }
        private void ApplyUpdate()
        {
            if (valueUpdates.Count > 0)
            {
                switchManager.isOn = valueUpdates.Dequeue();
            }
        }

        /// <summary>
        /// The event handler that gets invoked when the value changes.
        /// </summary>
        public Action<bool> OnValueChange { get; set; }
    }

    /// <summary>
    /// Instantiates a switch game object via prefab and sets the wrapper script.
    /// </summary>
    public class SwitchBuilder : UIBuilder<Switch>
    {
        protected override string PrefabPath => "Prefabs/UI/Input Group - Switch";

        private SwitchBuilder(Transform parent) : base(parent)
        {
        }

        public static SwitchBuilder Init(Transform parent)
        {
            return new SwitchBuilder(parent);
        }

        public SwitchBuilder SetLabel(string label)
        {
            Instance.Label = label;
            return this;
        }

        public SwitchBuilder SetOnChangeHandler(Action<bool> onChangeHandler)
        {
            Instance.OnValueChange = onChangeHandler;
            return this;
        }

        public SwitchBuilder SetDefaultValue(bool defaultValue)
        {
            Instance.Value = defaultValue;
            return this;
        }
    }
}
