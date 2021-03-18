using UnityEngine;

namespace SEE.GO
{
    [RequireComponent(typeof(LineRenderer))]
    public class EdgeAnimator : MonoBehaviour
    {
        // Immutable after initialization
        private LineRenderer lineRenderer;
        private Color initialColor;
        private Color highlightColor;
        private Color finalColor;
        private float initialToHighlightTime;
        private float highlightToFinalTime;

        // Mutable
        private float timer = 0.0f;

        public static EdgeAnimator Create(GameObject attachTo, Color initialColor, Color highlightColor, Color finalColor, float initialToHighlightTime, float highlightToFinalTime)
        {
            EdgeAnimator result = attachTo.AddComponent<EdgeAnimator>();

            result.lineRenderer = attachTo.GetComponent<LineRenderer>();
            result.initialColor = initialColor;
            result.highlightColor = highlightColor;
            result.finalColor = finalColor;
            result.initialToHighlightTime = initialToHighlightTime;
            result.highlightToFinalTime = highlightToFinalTime;

            return result;
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer <= initialToHighlightTime)
            {
                float t = timer / initialToHighlightTime;
                Color c = Color.Lerp(initialColor, highlightColor, t);
                LineFactory.SetColor(lineRenderer, c);
            }
            else if (timer - initialToHighlightTime <= highlightToFinalTime)
            {
                float t = (timer - initialToHighlightTime) / highlightToFinalTime;
                Color c = Color.Lerp(highlightColor, finalColor, t);
                LineFactory.SetColor(lineRenderer, c);
            }
            else
            {
                Destroy(this);
            }
        }
    }
}
