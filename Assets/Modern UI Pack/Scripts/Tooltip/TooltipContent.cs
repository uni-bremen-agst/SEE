using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace Michsky.UI.ModernUIPack
{
    [AddComponentMenu("Modern UI Pack/Tooltip/Tooltip Content")]
    public class TooltipContent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Content")]
        [TextArea] public string description;
        public float delay;

        [Header("Resources")]
        public GameObject tooltipRect;
        public TextMeshProUGUI descriptionText;

        TooltipManager tpManager;
        [HideInInspector] public Animator tooltipAnimator;

        void Start()
        {
            if (tooltipRect == null || descriptionText == null)
            {
                try
                {
                    tooltipRect = GameObject.Find("Tooltip Rect");
                    descriptionText = tooltipRect.transform.GetComponentInChildren<TextMeshProUGUI>();
                }

                catch { Debug.LogError("No Tooltip Rect assigned.", this); }
            }

            if (tooltipRect != null)
            {
                tpManager = tooltipRect.GetComponentInParent<TooltipManager>();
                tooltipAnimator = tooltipRect.GetComponentInParent<Animator>();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltipRect == null)
                return;

            descriptionText.text = description;
            tpManager.allowUpdating = true;
            tooltipAnimator.gameObject.SetActive(false);
            tooltipAnimator.gameObject.SetActive(true);

            if (delay == 0)
                tooltipAnimator.Play("In");
            else
                StartCoroutine("ShowTooltip");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltipRect == null)
                return;

            if (delay != 0)
            {
                StopCoroutine("ShowTooltip");

                if (tooltipAnimator.GetCurrentAnimatorStateInfo(0).IsName("In"))
                     tooltipAnimator.Play("Out");
            }

            else
                tooltipAnimator.Play("Out");

            tpManager.allowUpdating = false;
        }

        IEnumerator ShowTooltip()
        {
            yield return new WaitForSeconds(delay);
            tooltipAnimator.Play("In");
            StopCoroutine("ShowTooltip");
        }
    }
}