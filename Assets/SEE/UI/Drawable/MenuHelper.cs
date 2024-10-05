using SEE.Game.Drawable;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// This class provides methods intended to assist menus.
    /// </summary>
    public static class MenuHelper
    {
        /// <summary>
        /// Calculates the rect height for a dynamic UI menu.
        /// </summary>
        /// <param name="menuInstance">The instance of the menu</param>
        /// <param name="movePosition">Indicates whether the menu should be shifted to maintain the original appearance.</param>
        public static void CalculateHeight(GameObject menuInstance, bool movePosition = false)
        {
            /// Gets the three transforms of a menu.
            /// The whole menu transform
            RectTransform menuTransform = menuInstance.GetComponent<RectTransform>();
            /// The content transform
            RectTransform contentTransform = GameFinder.FindChild(menuInstance, "Content")
                .GetComponent<RectTransform>();
            /// The dragger transform
            RectTransform draggerTransform = GameFinder.FindChild(menuInstance, "Dragger")
                .GetComponent<RectTransform>();

            /// The old height of the menu.
            float oldHeight = contentTransform.rect.height;

            /// Forces the menu canvas to update so that the correct sizes are calculated.
            Canvas.ForceUpdateCanvases();

            /// Updates the Content Size Fitter of the content object.
            /// This is needed for it to recalculate its size.
            ContentSizeFitter csf = GameFinder.FindChild(menuInstance, "Content").GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            /// The new height of the menu.
            float newHeight = contentTransform.rect.height;

            /// The offset of the change.
            float diffHeight = oldHeight - newHeight;

            /// Calculates the new whole menu height.
            menuTransform.sizeDelta = new Vector2(contentTransform.rect.width,
                contentTransform.rect.height + draggerTransform.rect.height);

            /// It is necessary for the correct representation to update the parent and the position.
            Transform parent = menuTransform.parent;
            Vector3 position = menuTransform.position;
            menuTransform.SetParent(null);
            menuTransform.SetParent(parent);
            menuTransform.position = position;

            if (movePosition)
            {
                /// Moves the local position of the menu by the offset, divided by 2.
                Vector3 locPos = menuTransform.localPosition;
                menuTransform.localPosition = new Vector3(locPos.x, locPos.y + diffHeight / 2, locPos.z);
            }
        }
    }
}
