using UnityEngine;

namespace SEE.Layout
{
    [System.Serializable]
    public class ErosionCloud : MonoBehaviour
    {
        private Sprite sprite;
        public IconFactory.Erosion erosion;

        private void Start()
        {
            sprite = IconFactory.Instance.GetSprite(erosion);
            SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
        }
    }
}
