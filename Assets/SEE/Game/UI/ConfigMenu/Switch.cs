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
        public string label;

        private TextMeshProUGUI _label;
        private SwitchManager _switchManager;

        private readonly Queue<bool> _valueUpdates = new Queue<bool>();

        /// <summary>
        /// Requests an external value update.
        /// </summary>
        public bool Value {
            set => _valueUpdates.Enqueue(value);
        }

        void Start()
        {
            MustGetComponentInChild("Label", out _label);
            _label.text = label;

            MustGetComponentInChild("Switch", out _switchManager);
            _switchManager.OnEvents.AddListener(() => OnValueChange.Invoke(true));
            _switchManager.OffEvents.AddListener(() => OnValueChange.Invoke(false));
            ApplyUpdate();
        }

        void Update()
        {
            ApplyUpdate();
        }

        void ApplyUpdate()
        {
            if (_valueUpdates.Count > 0)
            {
                _switchManager.isOn = _valueUpdates.Dequeue();
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
    public class SwitchBuilder : UiBuilder<Switch>
    {
        protected override string PrefabPath => "Assets/Prefabs/UI/Input Group - Switch.prefab";

        private SwitchBuilder(Transform parent) : base(parent)
        {
        }

        public static SwitchBuilder Init(Transform parent)
        {
            return new SwitchBuilder(parent);
        }

        public SwitchBuilder SetLabel(string label)
        {
            Instance.label = label;
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
