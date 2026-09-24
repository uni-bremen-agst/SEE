using SEE.DataModel.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.UI.Drawable;
using SEE.Utils;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// Provides page management operations for drawable surfaces.
    /// </summary>
    public static class GameDrawablePageManager
    {
        /// <summary>
        /// Changes the currently selected page of a drawable surface.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <param name="page">The page to switch to.</param>
        /// <param name="forceChange">
        /// Whether the page should also change if it is already the current page.
        /// </param>
        public static void ChangeCurrentPage(GameObject obj, int page, bool forceChange = false)
        {
            GameObject surface = GameFinder.GetDrawableSurface(obj);
            DrawableHolder holder = surface.GetComponent<DrawableHolder>();

            if (holder.CurrentPage != page || forceChange)
            {
                holder.CurrentPage = page;

                foreach (DrawableType type in DrawableConfigManager.GetDrawableConfig(surface).GetAllDrawableTypes())
                {
                    GameObject typeObj = GameFinder.FindAttachedOrLocalDescendant(surface, type.ID);

                    if (typeObj.GetComponent<AssociatedPageHolder>().AssociatedPage == page)
                    {
                        typeObj.SetActive(true);
                    }
                    else
                    {
                        typeObj.SetActive(false);
                    }
                }

                holder.OrderInLayer =
                    GetMaximumPageOrderInLayer(GameFinder.GetAttachedObjectsObject(surface));
            }

            if (surface.TryGetDrawableSurface(out DrawableSurface drawableSurface))
            {
                drawableSurface.CurrentPage = page;
            }

            SurfacePageController surfacePageController =
                obj.GetRootParent().GetComponentInChildren<SurfacePageController>();

            if (surfacePageController != null)
            {
                surfacePageController.UpdatePage();
            }
        }

        /// <summary>
        /// Changes the maximum page size of a drawable surface.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <param name="maxPage">The new maximum page size.</param>
        public static void ChangeMaxPage(GameObject obj, int maxPage)
        {
            GameFinder.GetDrawableSurface(obj).GetComponent<DrawableHolder>().MaxPageSize = maxPage;
        }

        /// <summary>
        /// Removes the given page from a drawable surface.
        /// Following pages are renumbered to avoid gaps.
        /// If the currently selected page is removed, another valid page is selected.
        /// </summary>
        /// <param name="surface">The drawable surface from which the page should be removed.</param>
        /// <param name="page">The page to remove.</param>
        public static void RemovePage(GameObject surface, int page)
        {
            if (!surface.CompareTag(Tags.Drawable))
            {
                surface = GameFinder.GetDrawableSurface(surface);
            }

            DrawableHolder holder = surface.GetComponent<DrawableHolder>();
            bool equalChange = page == holder.CurrentPage;
            bool numberChange = page < holder.CurrentPage;

            DeleteDrawableTypesFromPage(surface, page);

            if (holder.MaxPageSize > 1)
            {
                holder.MaxPageSize--;
            }

            ChangePageNumbering(surface, page);

            if (numberChange)
            {
                ChangeCurrentPage(surface, holder.CurrentPage - 1);
            }

            if (equalChange)
            {
                if (holder.MaxPageSize > holder.CurrentPage)
                {
                    ChangeCurrentPage(surface, holder.CurrentPage, true);
                }
                else
                {
                    ChangeCurrentPage(surface, 0, true);
                }
            }
        }

        /// <summary>
        /// Destroys all <see cref="DrawableType"/> game objects from the given page.
        /// </summary>
        /// <param name="surface">The drawable surface whose page should be cleared.</param>
        /// <param name="page">The page to clear.</param>
        public static void DeleteDrawableTypesFromPage(GameObject surface, int page)
        {
            foreach (GameObject drawableType in GameFinder.GetDrawableTypesOfPage(surface, page))
            {
                Destroyer.Destroy(drawableType);
            }
        }

        /// <summary>
        /// Gets the maximum order in layer of the current page.
        /// </summary>
        /// <param name="attachedObjects">
        /// The object containing the drawable type objects.
        /// Only active objects of the current page are considered.
        /// </param>
        /// <returns>The maximum order in layer.</returns>
        private static int GetMaximumPageOrderInLayer(GameObject attachedObjects)
        {
            if (attachedObjects == null)
            {
                return 1;
            }

            OrderInLayerValueHolder[] holders =
                attachedObjects.GetComponentsInChildren<OrderInLayerValueHolder>();

            return holders.Length > 0
                ? holders.Max(holder => holder.OrderInLayer) + 1
                : 1;
        }

        /// <summary>
        /// Renumbers all pages following the removed page.
        /// </summary>
        /// <param name="surface">The drawable surface whose pages should be renumbered.</param>
        /// <param name="page">The removed page.</param>
        private static void ChangePageNumbering(GameObject surface, int page)
        {
            GameObject attachedObjects = GameFinder.GetAttachedObjectsObject(surface);

            if (attachedObjects == null)
            {
                return;
            }

            AssociatedPageHolder[] holders =
                attachedObjects.GetComponentsInChildren<AssociatedPageHolder>(true);

            foreach (AssociatedPageHolder holder in holders)
            {
                if (holder.AssociatedPage > page)
                {
                    holder.AssociatedPage--;
                }
            }
        }
    }
}
