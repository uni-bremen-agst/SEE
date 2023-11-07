using SEE.Game.Drawable;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// This class provides method's for menu's.
    /// </summary>
    public static class MenuHelper
    {
        /// <summary>
        /// Calculates the rect height for a dynamic UI menu.
        /// </summary>
        /// <param name="menuInstance">The instance of the menu</param>
        public static void CalculateHeight(GameObject menuInstance)
        {
            RectTransform menuTransform = menuInstance.GetComponent<RectTransform>();
            RectTransform contentTransform = GameFinder.FindChild(menuInstance, "Content").GetComponent<RectTransform>();
            RectTransform draggerTransform = GameFinder.FindChild(menuInstance, "Dragger").GetComponent<RectTransform>();

            Canvas.ForceUpdateCanvases();
            ContentSizeFitter csf = GameFinder.FindChild(menuInstance, "Content").GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            menuTransform.sizeDelta = new Vector2(contentTransform.rect.width, contentTransform.rect.height + draggerTransform.rect.height);
            
            Transform parent = menuTransform.parent;
            Vector3 position = menuTransform.position;
            menuTransform.SetParent(null);
            menuTransform.SetParent(parent);
            menuTransform.position = position;
        }
    }
}