using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Script for handling the dragging of the slider in the Evolution UI.
    /// </summary>
    public class SliderDrag : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
    {

        /// <summary>
        /// The user-data model for AnimationCanvas.
        /// </summary>
        private AnimationDataModel animationDataModel; // not serialized; will be set in Init()

        /// <summary>
        /// The AnimationCanvas.
        /// </summary>
        public GameObject AnimationCanvas;

        /// <summary>
        /// The hover text showing the iteration currently hovered over
        /// </summary>
        public Text HoverText;

        /// <summary>
        /// The evolution renderer doing the rendering and animations of the graphs.
        /// </summary>
        private EvolutionRenderer evolutionRenderer; // not serialized; will be set in property EvolutionRenderer

        /// <summary>
        /// True if autoplay was on when drag started
        /// </summary>
        private bool wasAutoPlay = false;

        /// <summary>
        /// True if reverse autoplay was on when drag started
        /// </summary>
        private bool wasAutoPlayReverse = false;

        /// <summary>
        /// True if currently dragging
        /// </summary>
        private bool isDragging = false;

        /// <summary>
        /// True if drag is finished but evolutionRenderer is still animating 
        /// </summary>
        private bool awaitFinish;


        /// <summary>
        /// The evolution renderer doing the rendering and animations of the graphs.
        /// </summary>
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

            HoverText.enabled = false;
        }

        /// <summary>
        /// Move hover text while dragging
        /// </summary>
        private void Update()
        {
            if (isDragging)
            {
                if (awaitFinish && !evolutionRenderer.IsStillAnimating)
                {
                    awaitFinish = false;
                    FinishDrag();
                }
                Vector3 handlePos = animationDataModel.Slider.handleRect.transform.position;
                Vector3 textPos = new Vector3(handlePos.x, handlePos.y + 0.05f, handlePos.z);
                HoverText.text = (animationDataModel.Slider.value + 1f).ToString() + "/" + (animationDataModel.Slider.maxValue + 1f);
                HoverText.rectTransform.position = textPos;
            }
        }

        /// <summary>
        /// Actions to do when slider is clicked.
        /// </summary>
        /// <param name="eventData"> Data from the event </param>
        public void OnPointerDown(PointerEventData eventData)
        {
            HoverText.enabled = true;
            if (evolutionRenderer.IsAutoPlay)
            {
                wasAutoPlay = true;
                evolutionRenderer.ToggleAutoPlay();
            }
            if (evolutionRenderer.IsAutoPlayReverse)
            {
                wasAutoPlayReverse = true;
                evolutionRenderer.ToggleAutoPlayReverse();
            }
            isDragging = true;
        }

        /// <summary>
        /// Actions to do when cursor is let go after dragging
        /// </summary>
        /// <param name="eventData"> Data from the event </param>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (evolutionRenderer.IsStillAnimating)
            {
                awaitFinish = true;
            } else
            {
                FinishDrag();
            }
        }

        /// <summary>
        /// Finalize drag
        /// </summary>
        private void FinishDrag()
        {
            HoverText.enabled = false;
            if (animationDataModel.Slider.value != evolutionRenderer.CurrentGraphIndex)
            {
                evolutionRenderer.TryShowSpecificGraph((int)animationDataModel.Slider.value);
            }

            if (wasAutoPlay)
            {
                evolutionRenderer.ToggleAutoPlay();
                wasAutoPlay = false;
            }

            if (wasAutoPlayReverse)
            {
                evolutionRenderer.ToggleAutoPlayReverse();
                wasAutoPlayReverse = false;
            }
            isDragging = false;
        }
    }
}
