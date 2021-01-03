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

        private EvolutionRenderer evolutionRenderer;

        private bool wasAutoPlay = false;

        private bool wasAutoPlayReverse = false;

        private bool isDragging = false;

        private List<GameObject> hoverText;

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

            animationDataModel.Slider.onValueChanged.AddListener(delegate { TaskOnValueChanged(); });

            hoverText = new List<GameObject>();
        }

        private void Update()
        {
            if(hoverText.Count > 1)
            {
                for (int i = 0; i < hoverText.Count - 1; i++)
                {
                    GameObject g = hoverText[i];
                    Object.Destroy(g);
                }
            }
        }

        private void TaskOnValueChanged()
        {
            if (isDragging)
            {
                Vector3 handlePos = animationDataModel.Slider.handleRect.transform.position;
                Vector3 textPos = new Vector3(handlePos.x , handlePos.y + 0.05f, handlePos.z );
                GameObject text = TextFactory.GetTextWithWidth((animationDataModel.Slider.value+1).ToString() + "/" + (animationDataModel.Slider.maxValue+1f),
                                                      textPos, 0.1f);
                text.transform.SetParent(animationDataModel.Slider.transform);
                hoverText.Add(text);
            }
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
            isDragging = true;
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
            for (int i = 0; i < hoverText.Count; i++)
            {
                GameObject g = hoverText[i];
                Object.Destroy(g);
            }
            isDragging = false;
        }
    }
}

