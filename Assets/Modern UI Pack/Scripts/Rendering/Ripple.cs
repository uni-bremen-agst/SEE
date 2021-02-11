using UnityEngine;
using UnityEngine.UI;

namespace Michsky.UI.ModernUIPack
{
    public class Ripple : MonoBehaviour
    {
        public float speed;
        public float maxSize;
        public Color startColor;
        public Color transitionColor;
        Image colorImg;

        void Start()
        {
            transform.localScale = new Vector3(0f, 0f, 0f);
            colorImg = GetComponent<Image>();
            colorImg.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a);
        }

        void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(maxSize, maxSize, maxSize), Time.deltaTime * speed);
            colorImg.color = Color.Lerp(colorImg.color, new Color(transitionColor.r, transitionColor.g, transitionColor.b, transitionColor.a), Time.deltaTime * speed);

            if (transform.localScale.x >= maxSize * 0.998)
            {
                if (transform.parent.childCount == 1)
                    transform.parent.gameObject.SetActive(false);

                Destroy(gameObject);
            }
        }
    }
}