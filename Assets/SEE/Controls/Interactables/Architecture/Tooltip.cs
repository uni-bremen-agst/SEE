using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls.Architecture
{
    
    /// <summary>
    /// GraphElement Tooltip component that shows a tooltip text on screen.
    /// The original source code was provided by https://github.com/bfollington/unity-tooltip-system
    /// It has been slightly modified to fit the project use-case.
    /// </summary>
    ///
    public class Tooltip : MonoBehaviour
    {
        public TMP_Text Text;
        public UnityEngine.UI.Image Image;
        public bool Center = true;
        
        public float Padding = 10f;
        private Vector2 _initialPosition;
        private Vector2 _initialSize;

        private void Start()
        {
            Assert.IsNotNull(Text);
            Assert.IsNotNull(Image);

            _initialPosition = (transform as RectTransform).anchoredPosition;
            _initialSize = Image.rectTransform.sizeDelta - Vector2.right * Padding;
        }
        
        private void Update()
        {
            Image.rectTransform.sizeDelta = new Vector2(Text.renderedWidth + Padding, 30);

            if (Center)
            {
                var sizeDiff = Image.rectTransform.sizeDelta - _initialSize;
                (transform as RectTransform).anchoredPosition = _initialPosition - new Vector2(sizeDiff.x / 2f, 0);
            }
        }
    }
}