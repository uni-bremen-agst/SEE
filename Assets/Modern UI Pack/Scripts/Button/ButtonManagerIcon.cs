﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Michsky.UI.ModernUIPack
{
    [RequireComponent(typeof(Button))]
    public class ButtonManagerIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        // Content
        public Sprite buttonIcon;
        public UnityEvent clickEvent;
        public UnityEvent hoverEvent;
        public AudioClip hoverSound;
        public AudioClip clickSound;
        public Button buttonVar;

        // Resources
        public Image normalIcon;
        public Image highlightedIcon;
        public AudioSource soundSource;
        public GameObject rippleParent;

        // Settings
        public AnimationSolution animationSolution = AnimationSolution.SCRIPT;
        [Range(0.25f, 15)] public float fadingMultiplier = 8;
        public bool useCustomContent = false;
        public bool enableButtonSounds = false;
        public bool useHoverSound = true;
        public bool useClickSound = true;
        public bool useRipple = true;

        // Ripple
        public RippleUpdateMode rippleUpdateMode = RippleUpdateMode.UNSCALED_TIME;
        public Sprite rippleShape;
        [Range(0.1f, 5)] public float speed = 1f;
        [Range(0.5f, 25)] public float maxSize = 4f;
        public Color startColor = new Color(1f, 1f, 1f, 1f);
        public Color transitionColor = new Color(1f, 1f, 1f, 1f);
        public bool renderOnTop = false;
        public bool centered = false;
        bool isPointerOn;

        CanvasGroup normalCG;
        CanvasGroup highlightedCG;
        float currentNormalValue;
        float currenthighlightedValue;

        public enum AnimationSolution
        {
            ANIMATOR,
            SCRIPT
        }

        public enum RippleUpdateMode
        {
            NORMAL,
            UNSCALED_TIME
        }

        void OnEnable()
        {
            if (normalCG == null && highlightedCG == null)
                return;

            normalCG.alpha = 1;
            highlightedCG.alpha = 0;
        }

        void Start()
        {
            if (animationSolution == AnimationSolution.SCRIPT)
            {
                normalCG = transform.Find("Normal").GetComponent<CanvasGroup>();
                highlightedCG = transform.Find("Highlighted").GetComponent<CanvasGroup>();

                Animator tempAnimator = this.GetComponent<Animator>();
                Destroy(tempAnimator);
            }

            if (buttonVar == null)
                buttonVar = gameObject.GetComponent<Button>();

            buttonVar.onClick.AddListener(delegate { clickEvent.Invoke(); });

            if (enableButtonSounds == true && useClickSound == true)
                buttonVar.onClick.AddListener(delegate { soundSource.PlayOneShot(clickSound); });

            if (useCustomContent == false)
                UpdateUI();

            if (useRipple == true && rippleParent != null)
                rippleParent.SetActive(false);
            else if (useRipple == false && rippleParent != null)
                Destroy(rippleParent);
        }

        public void UpdateUI()
        {
            normalIcon.sprite = buttonIcon;
            highlightedIcon.sprite = buttonIcon;
        }

        public void CreateRipple(Vector2 pos)
        {
            if (rippleParent != null)
            {
                GameObject rippleObj = new GameObject();
                rippleObj.AddComponent<Image>();
                rippleObj.GetComponent<Image>().sprite = rippleShape;
                rippleObj.name = "Ripple";
                rippleParent.SetActive(true);
                rippleObj.transform.SetParent(rippleParent.transform);

                if (renderOnTop == true)
                    rippleParent.transform.SetAsLastSibling();
                else
                    rippleParent.transform.SetAsFirstSibling();

                if (centered == true)
                    rippleObj.transform.localPosition = new Vector2(0f, 0f);
                else
                    rippleObj.transform.position = pos;

                rippleObj.AddComponent<Ripple>();
                Ripple tempRipple = rippleObj.GetComponent<Ripple>();
                tempRipple.speed = speed;
                tempRipple.maxSize = maxSize;
                tempRipple.startColor = startColor;
                tempRipple.transitionColor = transitionColor;

                if (rippleUpdateMode == RippleUpdateMode.NORMAL)
                    tempRipple.unscaledTime = false;
                else
                    tempRipple.unscaledTime = true;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (useRipple == true && isPointerOn == true)
#if ENABLE_LEGACY_INPUT_MANAGER
                CreateRipple(Input.mousePosition);
#elif ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
                CreateRipple(Input.mousePosition);
#elif ENABLE_INPUT_SYSTEM
                CreateRipple(Mouse.current.position.ReadValue());
#endif
            else if (useRipple == false)
                this.enabled = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (enableButtonSounds == true && useHoverSound == true && buttonVar.interactable == true)
                soundSource.PlayOneShot(hoverSound);

            hoverEvent.Invoke();
            isPointerOn = true;

            if (animationSolution == AnimationSolution.SCRIPT && buttonVar.interactable == true)
                StartCoroutine("FadeIn");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOn = false;

            if (animationSolution == AnimationSolution.SCRIPT && buttonVar.interactable == true)
                StartCoroutine("FadeOut");
        }

        IEnumerator FadeIn()
        {
            StopCoroutine("FadeOut");
            currentNormalValue = normalCG.alpha;
            currenthighlightedValue = highlightedCG.alpha;

            while (currenthighlightedValue <= 1)
            {
                currentNormalValue -= Time.deltaTime * fadingMultiplier;
                normalCG.alpha = currentNormalValue;

                currenthighlightedValue += Time.deltaTime * fadingMultiplier;
                highlightedCG.alpha = currenthighlightedValue;

                if (normalCG.alpha >= 1)
                    StopCoroutine("FadeIn");

                yield return null;
            }
        }

        IEnumerator FadeOut()
        {
            StopCoroutine("FadeIn");
            currentNormalValue = normalCG.alpha;
            currenthighlightedValue = highlightedCG.alpha;

            while (currentNormalValue >= 0)
            {
                currentNormalValue += Time.deltaTime * fadingMultiplier;
                normalCG.alpha = currentNormalValue;

                currenthighlightedValue -= Time.deltaTime * fadingMultiplier;
                highlightedCG.alpha = currenthighlightedValue;

                if (highlightedCG.alpha <= 0)
                    StopCoroutine("FadeOut");

                yield return null;
            }
        }
    }
}