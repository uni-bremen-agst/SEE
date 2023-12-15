using SEE.Game.Drawable;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.UI.Drawable
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
        public static void CalculateHeight(GameObject menuInstance)
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

            /// Forces the menu canvas to update so that the correct sizes are calculated.
            Canvas.ForceUpdateCanvases();

            /// Updates the Content Size Fitter of the content object. 
            /// This is needed for it to recalculate its size.
            ContentSizeFitter csf = GameFinder.FindChild(menuInstance, "Content").GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            /// Calculates the new whole menu height.
            menuTransform.sizeDelta = new Vector2(contentTransform.rect.width, 
                contentTransform.rect.height + draggerTransform.rect.height);

            /// It is necessary for the correct representation to update the parent and the position.
            Transform parent = menuTransform.parent;
            Vector3 position = menuTransform.position;
            menuTransform.SetParent(null);
            menuTransform.SetParent(parent);
            menuTransform.position = position;
        }
    }
}