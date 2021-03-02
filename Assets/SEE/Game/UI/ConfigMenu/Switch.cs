using System;
using System.Collections.Generic;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using TMPro;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    public class Switch : DynamicUIBehaviour
    {
        public String label;

        private TextMeshProUGUI _label;
        private SwitchManager _switchManager;

        private readonly Queue<bool> _valueUpdates = new Queue<bool>();

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

        public Action<bool> OnValueChange { get; set; }
    }

    public class SwitchBuilder
    {
        private readonly Switch _switch;

        private SwitchBuilder(Switch @switch)
        {
            _switch = @switch;
        }

        public static SwitchBuilder Init(GameObject checkboxHost)
        {
            checkboxHost.AddComponent<Switch>();
            checkboxHost.MustGetComponent(out Switch checkbox);
            return new SwitchBuilder(checkbox);
        }

        public Switch Build() => _switch;

        public SwitchBuilder SetLabel(string label)
        {
            _switch.label = label;
            return this;
        }

        public SwitchBuilder SetOnChangeHandler(Action<bool> onChangeHandler)
        {
            _switch.OnValueChange = onChangeHandler;
            return this;
        }

        public SwitchBuilder SetDefaultValue(bool defaultValue)
        {
            _switch.Value = defaultValue;
            return this;
        }
    }
}
