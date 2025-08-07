using Cysharp.Threading.Tasks;
using SEE.GO;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Monitors the height of the scrollable content in the runtime configuration menu.
    /// If the content height changes (e.g., due to expanding/collapsing sections or dynamic UI),
    /// the scrollbar position is adjusted to preserve the user's current scroll position,
    /// preventing sudden jumps or disorienting repositioning.
    /// </summary>
    public class ContentSizeWatcher : MonoBehaviour
    {
        /// <summary>
        /// The RectTransform of the scrollable content.
        /// </summary>
        private RectTransform contentRect;

        /// <summary>
        /// The RectTransform of the viewport that displays the scrollable content.
        /// </summary>
        private RectTransform viewportRect;

        /// <summary>
        /// The content height from the last frame.
        /// Used to detect changes in height.
        /// </summary>
        private float lastHeight;

        /// <summary>
        /// The last known scrollbar value.
        /// Used to restore the relative scroll position after content size changes.
        /// </summary>
        private float oldScrollValue = 1f;

        /// <summary>
        /// Getter property of <see cref="oldScrollValue"/>.
        /// </summary>
        public float CurrentScrollValue => oldScrollValue;

        /// <summary>
        /// The scrollbar controlling vertical scrolling of the view.
        /// </summary>
        private Scrollbar scrollbar;

        /// <summary>
        /// Constant for city configuration.
        /// </summary>
        public const string CityConfiguration = "City Configuration";

        /// <summary>
        /// Initializes references to the content, viewport, and scrollbar.
        /// Also stores the initial content height for later comparsion.
        /// </summary>
        private void Start()
        {
            GameObject runtimeTabGO = GetRuntimeTabGameObject(gameObject);
            GameObject viewList = runtimeTabGO.FindDescendant("ViewList");
            viewportRect = viewList.GetComponent<RectTransform>();
            contentRect = viewList.FindDescendant("Content", false).GetComponent<RectTransform>();
            scrollbar = runtimeTabGO.FindDescendant("TabScrollbar", false).GetComponent<Scrollbar>();
            lastHeight = contentRect.rect.height;
        }

        /// <summary>
        /// Called once per frame after all Update() calls.
        /// Detects content height changes and triggers scroll adjustment if necessary.
        /// </summary>
        private void LateUpdate()
        {
            float currentHeight = contentRect.rect.height;
            if (!Mathf.Approximately(currentHeight, lastHeight))
            {
                OnContentHeightChanged(lastHeight, currentHeight);
                lastHeight = currentHeight;
            }
            else
            {
                oldScrollValue = scrollbar.value;
            }
        }

        /// <summary>
        /// Adjusts the scrollbar value to maintain scroll position after a change in
        /// content height.
        /// </summary>
        /// <param name="oldHeight">The previous content height.</param>
        /// <param name="newHeight">The updated content height.</param>
        private void OnContentHeightChanged(float oldHeight, float newHeight)
        {
            float viewportHeight = viewportRect.rect.height;
            float scrollPosPixels = (1 - oldScrollValue) * (oldHeight - viewportHeight);
            float newScrollValue;

            if (newHeight <= viewportHeight)
            {
                newScrollValue = 1f;
            }
            else
            {
                newScrollValue = 1 - scrollPosPixels / (newHeight - viewportHeight);
                newScrollValue = Mathf.Clamp01(newScrollValue);
            }

            scrollbar.value = newScrollValue;
        }

        /// <summary>
        /// Recursively searches the hierarchy upwards to find the runtime tab GameObject.
        /// The tab is identified by the name "City Configuration".
        /// </summary>
        /// <param name="go">The starting GameObject.</param>
        /// <returns>The runtime tab GameObject, or null if not found.</returns>
        public static GameObject GetRuntimeTabGameObject(GameObject go)
        {
            if (go.transform.parent == null)
            {
                return null;
            }
            else
            {
                if (go.transform.parent.name.Equals(CityConfiguration))
                {
                    return go.transform.parent.gameObject;
                }
                else
                {
                    return GetRuntimeTabGameObject(go.transform.parent.gameObject);
                }
            }
        }

        /// <summary>
        /// Restores the previous scrollbar position based on the last known scroll value
        /// and the current content and viewport sizes. Should be used when the layout is
        /// reloaded but no content height change occurred.
        /// </summary>
        public async UniTask ApplyPreviousScrollPositionAsync(float scrollValue = -1f)
        {
            float value = scrollValue != -1f? scrollValue: oldScrollValue;
            await UniTask.DelayFrame(3);
            scrollbar.value = value;
        }
    }
}