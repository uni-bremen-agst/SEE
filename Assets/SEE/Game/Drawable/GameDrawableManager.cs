using SEE.DataModel.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.UI.Drawable;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Drawable
{
    public static class GameDrawableManager
    {
        /// <summary>
        /// Changes the color of a drawable surface.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <param name="color">The new color for the drawable</param>
        public static void ChangeColor(GameObject obj, Color color)
        {
            GameObject surfaceObj = GameFinder.GetDrawableSurface(obj);
            if (surfaceObj != null
                && surfaceObj.GetComponent<MeshRenderer>() != null)
            {
                surfaceObj.GetComponent<MeshRenderer>().material.color = color;
                if (surfaceObj.TryGetDrawableSurface(out DrawableSurface surface))
                {
                    surface.Color = color;
                }
            }
        }

        /// <summary>
        /// Change the lighting of a drawable.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <param name="state">The state of the light. true = on; false = off.</param>
        public static void ChangeLighting(GameObject obj, bool state)
        {
            Transform transform = GameFinder.GetDrawableSurfaceParent(obj).transform;
            transform.GetComponentInChildren<Light>().enabled = state;
            if (GameFinder.GetDrawableSurface(obj).TryGetDrawableSurface(out DrawableSurface surface))
            {
                surface.Lighting = state;
            }
        }

        /// <summary>
        /// Change the current maximum order in layer value of a surface.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <param name="orderInLayer">The new value for the order in layer.</param>
        public static void ChangeOrderInLayer(GameObject obj, int orderInLayer)
        {
            GameFinder.GetDrawableSurface(obj).GetComponent<DrawableHolder>().OrderInLayer = orderInLayer;
        }

        /// <summary>
        /// Change the description of a surface.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <param name="description">The new description.</param>
        public static void ChangeDescription(GameObject obj, string description)
        {
            GameFinder.GetDrawableSurface(obj).GetComponent<DrawableHolder>().Description = description;
            if (GameFinder.GetDrawableSurface(obj).TryGetDrawableSurface(out DrawableSurface surface))
            {
                surface.Description = description;
            }
        }

        /// <summary>
        /// Change the current selected page of a surface.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <param name="page">The page to switch to.</param>
        public static void ChangeCurrentPage(GameObject obj, int page)
        {
            GameObject surface = GameFinder.GetDrawableSurface(obj);
            DrawableHolder holder = surface.GetComponent<DrawableHolder>();
            if (holder.CurrentPage != page)
            {
                holder.CurrentPage = page;
                foreach (DrawableType type in DrawableConfigManager.GetDrawableConfig(surface).GetAllDrawableTypes())
                {
                    GameObject typeObj = GameFinder.FindChild(surface, type.Id);
                    if (typeObj.GetComponent<AssociatedPageHolder>().AssociatedPage == page)
                    {
                        typeObj.SetActive(true);
                    }
                    else
                    {
                        typeObj.SetActive(false);
                    };
                }
                holder.OrderInLayer = GetMaximumPageOrderInLayer(GameFinder.GetAttachedObjectsObject(surface));
            }
            if (GameFinder.GetDrawableSurface(obj).TryGetDrawableSurface(out DrawableSurface surf))
            {
                surf.CurrentPage = page;
            }
            if (GameFinder.GetHighestParent(obj).GetComponentInChildren<SurfacePageController>() != null)
            {
                GameFinder.GetHighestParent(obj).GetComponentInChildren<SurfacePageController>().UpdatePage();
            }
        }

        private static int GetMaximumPageOrderInLayer(GameObject attachedObject)
        {
            int max = 1;
            if (attachedObject != null)
            {
                OrderInLayerValueHolder[] holders = attachedObject.GetComponentsInChildren<OrderInLayerValueHolder>();
                max = holders.Count() > 0 ?
                    max = holders.Aggregate((t1, t2) => t1.OrderInLayer > t2.OrderInLayer ? t1 : t2).OrderInLayer + 1 : 1;
            }
            return max;
        }

        /// <summary>
        /// Change the maximum page size of a surface.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <param name="maxPage">The new maximum page size.</param>
        public static void ChangeMaxPage(GameObject obj, int maxPage)
        {
            GameFinder.GetDrawableSurface(obj).GetComponent<DrawableHolder>().MaxPageSize = maxPage;
        }

        /// <summary>
        /// Change the visibility of a drawable.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <param name="visibility">The visibility.</param>
        /// <remarks>Must be called last. 
        /// If this method is called before the <see cref="DrawableType"> objects are restored, 
        /// the correct object will not be hidden, causing errors.</remarks>
        public static void ChangeVisibility(GameObject obj, bool visibility)
        {
            GameFinder.GetHighestParent(obj).SetActive(visibility);
            if (GameFinder.GetDrawableSurface(obj).TryGetDrawableSurface(out DrawableSurface surface))
            {
                surface.Visibility = visibility;
            }
        }

        /// <summary>
        /// Combines all edit method together.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <param name="config">The configuration which holds the values for the changing.</param>
        public static void Change(GameObject obj, DrawableConfig config)
        {
            GameObject surface = GameFinder.GetDrawableSurface(obj);
            GameObject surfaceParent = GameFinder.GetDrawableSurfaceParent(surface);

            if (surface != null && surface.CompareTag(Tags.Drawable))
            {
                ChangeColor(surface, config.Color);
                ChangeLighting(surfaceParent, config.Lighting);
                ChangeOrderInLayer(surface, config.OrderInLayer);
                ChangeDescription(surface, config.Description);
                ChangeVisibility(surface, config.Visibility);
                ChangeCurrentPage(surface, config.CurrentPage);
                ChangeMaxPage(surface, config.MaxPageSize);
            }
        }

        /// <summary>
        /// Query to check if the drawable surface have a description.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <returns>True if the surface have a description.</returns>
        public static bool HasDescription(GameObject obj)
        {
            DrawableConfig config = DrawableConfigManager.GetDrawableConfig(GameFinder.GetDrawableSurface(obj));
            return !string.IsNullOrWhiteSpace(config.Description) && !string.IsNullOrEmpty(config.Description);
        }

        /// <summary>
        /// Gets the description of a drawable surface.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <returns>The description of the drawable.</returns>
        public static string GetDescription(GameObject obj)
        {
            DrawableConfig config = DrawableConfigManager.GetDrawableConfig(GameFinder.GetDrawableSurface(obj));
            return config.Description;
        }

        /// <summary>
        /// Query to check if the drawable surface have an active lighting.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <returns>True if the surface have an active lighting.</returns>
        public static bool IsLighting(GameObject obj)
        {
            return DrawableConfigManager.GetDrawableConfig(GameFinder.GetDrawableSurface(obj)).Lighting;
        }

        /// <summary>
        /// Query to check if the drawable is visible.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <returns>True if the surface is visible.</returns>
        public static bool IsVisible(GameObject obj)
        {
            return DrawableConfigManager.GetDrawableConfig(GameFinder.GetDrawableSurface(obj)).Visibility;
        }
    }
}