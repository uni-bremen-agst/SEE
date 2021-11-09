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
                float x = timer / initialToHighlightTime;
                float t = Mathf.Sin(0.5f * Mathf.PI * x);
                Color c = Color.Lerp(initialColor, highlightColor, t);
                LineFactory.SetColor(lineRenderer, c);
            }
            else if (timer - initialToHighlightTime <= highlightToFinalTime)
            {
                float x = (timer - initialToHighlightTime) / highlightToFinalTime;
                float t = -Mathf.Cos(Mathf.PI * x) * 0.5f + 0.5f;
                Color c = Color.Lerp(highlightColor, finalColor, t);
                LineFactory.SetColor(lineRenderer, c);
            }
            else
            {
                LineFactory.SetColor(lineRenderer, finalColor);
                Destroy(this);
            }
        }
    }
}
