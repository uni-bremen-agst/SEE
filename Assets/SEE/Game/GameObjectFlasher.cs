using UnityEngine;
using DG.Tweening;
using SEE.Utils;

namespace SEE.Game
{
    /// <summary>
    /// Flashes a game object, that is, animates its color pulsating from its
    /// original color to its inverted color.
    /// </summary>
    class GameObjectFlasher
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gameObject">the object to be flashed</param>
        public GameObjectFlasher(GameObject gameObject)
        {
            this.gameObject = gameObject;
        }

        /// <summary>
        /// The object to be flashed.
        /// </summary>
        private readonly GameObject gameObject;

        /// <summary>
        /// The tween to animate <see cref="gameObject"/>.
        /// </summary>
        private Tween selectionTween;

        /// <summary>
        /// The original color of <see cref="gameObject"/>.
        /// </summary>
        private Color originalColor;

        /// <summary>
        /// The duration of the color animation in seconds.
        /// </summary>
        private const float animationDuration = 3;

        /// <summary>
        /// Starts the flashing animation.
        /// </summary>
        public void StartFlashing()
        {
            Material material = gameObject.GetComponent<Renderer>().sharedMaterial;
            originalColor = material.color;
            selectionTween = material.DOColor(material.color.Invert(), animationDuration).SetEase(DG.Tweening.Ease.Flash).SetLoops(-1, LoopType.Yoyo);
        }

        /// <summary>
        /// Stops the flashing animation immediately and restores the original color.
        /// </summary>
        public void StopFlashing()
        {
            if (selectionTween != null)
            {
                selectionTween.SetLoops(0);
                selectionTween.Kill();
                gameObject.GetComponent<Renderer>().sharedMaterial.color = originalColor;
                selectionTween = null;
            }
        }
    }
}
