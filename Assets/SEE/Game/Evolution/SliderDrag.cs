using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.Utils;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SEE.Game.Evolution
{
    public class SliderDrag : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
    {

        /// <summary>
        /// The user-data model for AnimationCanvas.
        /// </summary>
        private AnimationDataModel animationDataModel; // not serialized; will be set in Init()

        public GameObject AnimationCanvas;

        private EvolutionRenderer evolutionRenderer;

        private bool wasAutoPlay = false;

        private bool wasAutoPlayReverse = false;

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
        }

        public void OnPointerDown(PointerEventData eventData)
        {

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
        }

        public void OnPointerUp(PointerEventData eventData)
        {
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
        }
    }
}

