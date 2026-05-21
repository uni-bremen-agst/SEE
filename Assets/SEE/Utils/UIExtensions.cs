using DG.Tweening;
using SEE.Game.Operator;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Utils
{
    /// <summary>
    /// Contains extension methods for (2D) Unity UI elements.
    /// </summary>
    public static class UIExtensions
    {
        /// <summary>
        /// Scrolls this <paramref name="scrollRect"/> to the given <paramref name="item"/>.
        /// If a <paramref name="duration"/> is given, the scrolling will be animated.
        /// </summary>
        /// <param name="scrollRect">The scroll rect to scroll.</param>
        /// <param name="item">The item to scroll to.</param>
        /// <param name="duration">The duration of the animation, if any.</param>
        /// <returns>A callback which can be used to react to the end of the animation.</returns>
        public static IOperationCallback<TweenCallback> ScrollTo(this ScrollRect scrollRect, RectTransform item, float? duration = null)
        {
            Canvas.ForceUpdateCanvases();
            // Adapted from https://gist.github.com/yasirkula/75ca350fb83ddcc1558d33a8ecf1483f
            Vector2 contentSize = scrollRect.content.rect.size;
            Vector2 contentScale = scrollRect.content.localScale;

            // Calculate the focus point relative to the content's pivot.
            Vector2 itemCenterPoint = scrollRect.content.InverseTransformPoint(item.transform.TransformPoint(item.rect.center));
            Vector2 contentSizeOffset = contentSize * scrollRect.content.pivot;

            // Scale position and size according to scroll rect's local scale.
            Vector2 position = (itemCenterPoint + contentSizeOffset) * contentScale;
            contentSize *= contentScale;

            Vector2 viewportSize = ((RectTransform) scrollRect.content.parent).rect.size;

            // Calculate the new scroll position which centers the item.
            Vector2 scrollPosition = scrollRect.normalizedPosition;
            if (scrollRect.horizontal && contentSize.x > viewportSize.x)
            {
                scrollPosition.x = Mathf.Clamp01((position.x - viewportSize.x * 0.5f) / (contentSize.x - viewportSize.x));
            }
            if (scrollRect.vertical && contentSize.y > viewportSize.y)
            {
                scrollPosition.y = Mathf.Clamp01((position.y - viewportSize.y * 0.5f) / (contentSize.y - viewportSize.y));
            }

            if (!duration.HasValue)
            {
                scrollRect.normalizedPosition = scrollPosition;
                return new DummyOperationCallback<TweenCallback>();
            }
            else
            {
                return new TweenOperationCallback(scrollRect.DONormalizedPos(scrollPosition, duration.Value).Play());
            }
        }
    }
}
