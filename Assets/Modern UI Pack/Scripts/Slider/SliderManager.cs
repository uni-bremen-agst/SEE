﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

namespace Michsky.UI.ModernUIPack
{
    public class SliderManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // Resources
        public Slider mainSlider;
        public TextMeshProUGUI valueText;
        public TextMeshProUGUI popupValueText;

        // Saving
        public bool enableSaving = false;
        public string sliderTag = "Tag Text";

        // Settings
        public bool usePercent = false;
        public bool showValue = true;
        public bool showPopupValue = true;
        public bool useRoundValue = false;

        // Events
        [System.Serializable]
        public class SliderEvent : UnityEvent<float> { }
        [SerializeField]
        public SliderEvent onValueChanged = new SliderEvent();
        [Space(8)] public SliderEvent sliderEvent;

        // Other Variables
        [HideInInspector] public Animator sliderAnimator;
        [HideInInspector] public float saveValue;

        void Start()
        {
            try
            {
                sliderAnimator = gameObject.GetComponent<Animator>();

                if (enableSaving == true)
                {
                    if (PlayerPrefs.HasKey(sliderTag + "SliderValue") == false)
                        saveValue = mainSlider.value;
                    else
                        saveValue = PlayerPrefs.GetFloat(sliderTag + "SliderValue");

                    mainSlider.value = saveValue;
                    mainSlider.onValueChanged.AddListener(delegate
                    {
                        saveValue = mainSlider.value;
                        PlayerPrefs.SetFloat(sliderTag + "SliderValue", saveValue);
                    });
                }

                mainSlider.onValueChanged.AddListener(delegate 
                {
                    sliderEvent.Invoke(mainSlider.value);
                });
            }

            catch
            {
                Debug.LogError("Slider - Cannot initalize the object due to missing components.");
            }
        }

        void Update()
        {
            if (useRoundValue == true)
            {
                if (usePercent == true)
                {
                    if (valueText != null)
                        valueText.text = Mathf.Round(mainSlider.value * 1.0f).ToString() + "%";

                    if (popupValueText != null)
                        popupValueText.text = Mathf.Round(mainSlider.value * 1.0f).ToString() + "%";
                }

                else
                {
                    if (valueText != null)
                        valueText.text = Mathf.Round(mainSlider.value * 1.0f).ToString();

                    if (popupValueText != null)
                        popupValueText.text = Mathf.Round(mainSlider.value * 1.0f).ToString();
                }
            }

            else
            {
                if (usePercent == true)
                {
                    if (valueText != null)
                        valueText.text = mainSlider.value.ToString("F1") + "%";

                    if (popupValueText != null)
                        popupValueText.text = mainSlider.value.ToString("F1") + "%";
                }

                else
                {
                    if (valueText != null)
                        valueText.text = mainSlider.value.ToString("F1");

                    if (popupValueText != null)
                        popupValueText.text = mainSlider.value.ToString("F1");
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (showPopupValue == true)
                sliderAnimator.Play("Value In");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (showPopupValue == true)
                sliderAnimator.Play("Value Out");
        }
    }
}