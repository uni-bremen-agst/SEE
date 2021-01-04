using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.Utils;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SEE.GO;

namespace SEE.Game.Evolution
{
    public class SliderDrag : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
    {

        /// <summary>
        /// The user-data model for AnimationCanvas.
        /// </summary>
        private AnimationDataModel animationDataModel; // not serialized; will be set in Init()

        public GameObject AnimationCanvas;

        public Text hoverText;

        private EvolutionRenderer evolutionRenderer;

        private bool wasAutoPlay = false;

        private bool wasAutoPlayReverse = false;

        private bool isDragging = false;

        public EvolutionRenderer EvolutionRenderer
        {
            set
            {
                evolutionRenderer = value;
                Init();
            }
        }

        private void Init()
        {
            animationDataModel = AnimationCanvas.GetComponent<AnimationDataModel>();

            hoverText.enabled = false;

        }

        private void Update()
        {
            if (isDragging)
            {
                Vector3 handlePos = animationDataModel.Slider.handleRect.transform.position;
                Vector3 textPos = new Vector3(handlePos.x, handlePos.y + 0.05f, handlePos.z);
                hoverText.text = (animationDataModel.Slider.value + 1f).ToString() + "/" + (animationDataModel.Slider.maxValue + 1f);
                hoverText.rectTransform.position = textPos;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            hoverText.enabled = true;
            if (evolutionRenderer.IsAutoPlay)
            {
                wasAutoPlay = true;
                evolutionRenderer.ToggleAutoPlay();
            }
            if(evolutionRenderer.IsAutoPlayReverse)
            {
                wasAutoPlayReverse = true;
                evolutionRenderer.ToggleAutoPlayReverse();
            }
            isDragging = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            hoverText.enabled = false;
            if (animationDataModel.Slider.value != evolutionRenderer.CurrentGraphIndex)
            {
                evolutionRenderer.TryShowSpecificGraph((int)animationDataModel.Slider.value);
            }

            if (wasAutoPlay)
            {
                evolutionRenderer.ToggleAutoPlay();
                wasAutoPlay = false;
            }

            if(wasAutoPlayReverse)
            {
                evolutionRenderer.ToggleAutoPlayReverse();
                wasAutoPlayReverse = false;
            }
            isDragging = false;
        }
    }
}

