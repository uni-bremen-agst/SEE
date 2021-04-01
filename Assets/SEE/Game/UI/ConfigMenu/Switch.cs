using System;
using System.Collections.Generic;
using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    public class Switch : DynamicUIBehaviour
    {
        public string label;

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
