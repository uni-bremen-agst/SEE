using SEE.Game.Drawable;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// Provides helper methods for dynamically sized UI menus.
    /// </summary>
    public static class MenuHelper
    {
        /// <summary>
        /// Caches the layout references required for calculating menu heights.
        /// Weak keys allow menu instances and their cached references to be garbage-collected
        /// once the menu instances are no longer referenced elsewhere.
        /// </summary>
        private static readonly ConditionalWeakTable<GameObject, MenuLayoutReferences>
            layoutReferences = new();

        /// <summary>
        /// Holds the UI components required for calculating the height of a menu.
        /// </summary>
        private sealed class MenuLayoutReferences
        {
            /// <summary>
            /// The rect transform of the complete menu.
            /// </summary>
            internal RectTransform MenuTransform { get; }

            /// <summary>
            /// The rect transform containing the dynamically sized menu content.
            /// </summary>
            internal RectTransform ContentTransform { get; }

            /// <summary>
            /// The rect transform of the menu dragger.
            /// </summary>
            internal RectTransform DraggerTransform { get; }

            /// <summary>
            /// The content-size fitter controlling the dynamically sized menu content.
            /// </summary>
            internal ContentSizeFitter ContentSizeFitter { get; }

            /// <summary>
            /// Resolves the UI components required for calculating the height of
            /// <paramref name="menuInstance"/>.
            /// </summary>
            /// <param name="menuInstance">The menu whose layout components should be resolved.</param>
            internal MenuLayoutReferences(GameObject menuInstance)
            {
                MenuTransform = menuInstance.GetComponent<RectTransform>();

                GameObject content =
                    GameFinder.FindAttachedOrLocalDescendant(menuInstance, "Content");
                ContentTransform = content.GetComponent<RectTransform>();
                ContentSizeFitter = content.GetComponent<ContentSizeFitter>();

                GameObject dragger =
                    GameFinder.FindAttachedOrLocalDescendant(menuInstance, "Dragger");
                DraggerTransform = dragger.GetComponent<RectTransform>();
            }

            /// <summary>
            /// Determines whether all cached layout components are still valid for
            /// <paramref name="menuInstance"/>.
            /// </summary>
            /// <param name="menuInstance">
            /// The menu for which the cached references should be validated.
            /// </param>
            /// <returns>
            /// True if all cached components still exist and belong to the given menu;
            /// otherwise, false.
            /// </returns>
            internal bool IsValidFor(GameObject menuInstance)
            {
                return MenuTransform != null
                       && MenuTransform.gameObject == menuInstance
                       && ContentTransform != null
                       && ContentSizeFitter != null
                       && ContentSizeFitter.gameObject == ContentTransform.gameObject
                       && DraggerTransform != null
                       && ContentTransform.IsChildOf(MenuTransform)
                       && DraggerTransform.IsChildOf(MenuTransform);
            }
        }

        /// <summary>
        /// Returns valid cached layout references for <paramref name="menuInstance"/>.
        /// If no valid references are cached, they are resolved again from the menu hierarchy.
        /// </summary>
        /// <param name="menuInstance">The menu whose layout references are required.</param>
        /// <returns>The valid layout references for the given menu.</returns>
        private static MenuLayoutReferences GetLayoutReferences(GameObject menuInstance)
        {
            if (layoutReferences.TryGetValue(menuInstance, out MenuLayoutReferences references)
                && references.IsValidFor(menuInstance))
            {
                return references;
            }

            layoutReferences.Remove(menuInstance);

            references = new MenuLayoutReferences(menuInstance);
            layoutReferences.Add(menuInstance, references);
            return references;
        }

        /// <summary>
        /// Calculates the height of a dynamic UI menu and optionally shifts the menu
        /// to preserve its previous visual position.
        /// </summary>
        /// <param name="menuInstance">The menu whose height should be recalculated.</param>
        /// <param name="movePosition">
        /// Whether the menu should be shifted according to the height difference.
        /// </param>
        public static void CalculateHeight(GameObject menuInstance, bool movePosition = false)
        {
            MenuLayoutReferences references = GetLayoutReferences(menuInstance);
            RectTransform menuTransform = references.MenuTransform;
            RectTransform contentTransform = references.ContentTransform;
            RectTransform draggerTransform = references.DraggerTransform;
            ContentSizeFitter contentSizeFitter = references.ContentSizeFitter;

            float oldHeight = contentTransform.rect.height;

            Canvas.ForceUpdateCanvases();

            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform);

            float newHeight = contentTransform.rect.height;
            float diffHeight = oldHeight - newHeight;

            menuTransform.sizeDelta = new Vector2(
                contentTransform.rect.width,
                contentTransform.rect.height + draggerTransform.rect.height);

            Transform parent = menuTransform.parent;
            Vector3 position = menuTransform.position;
            menuTransform.SetParent(null);
            menuTransform.SetParent(parent);
            menuTransform.position = position;

            if (movePosition)
            {
                Vector3 localPosition = menuTransform.localPosition;
                menuTransform.localPosition = new Vector3(
                    localPosition.x,
                    localPosition.y + diffHeight / 2,
                    localPosition.z);
            }
        }
    }
}
