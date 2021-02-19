using System;
using Michsky.UI.ModernUIPack;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    [RequireComponent(typeof(SliderManager))]
    public class SliderControlledInput : MonoBehaviour
    {
        public string fieldName;
        public SliderManager sliderManager;

        void Start()
        {
            sliderManager = GetComponent<SliderManager>();
            sliderManager.mainSlider.onValueChanged.AddListener(newValue => OnValueChange(newValue.ToString()));
        }

        public string Value
        {
            get => sliderManager.mainSlider.value.ToString();
            set
            {
                Debug.Log(value);
                sliderManager.mainSlider.value = float.Parse(value);
            }
        }

        public Action<string> OnValueChange { get; set; }
    }
}
