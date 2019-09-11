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
            sprite = IconFactory.GetSprite(erosion);
            SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
        }
    }
}
