using Assets.SEE.Game.Drawable.ValueHolders;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
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
            if (GameFinder.GetDrawableSurface(obj) != null
                && GameFinder.GetDrawableSurface(obj).GetComponent<MeshRenderer>() != null)
            {
                GameFinder.GetDrawableSurface(obj).GetComponent<MeshRenderer>().material.color = color;
            }
        }

        /// <summary>
        /// Change the lighting of a drawable.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <param name="state">The state of the light. true = on; false = off.</param>
        public static void ChangeLightning(GameObject obj, bool state)
        {
            Transform transform = GameFinder.GetDrawableSurfaceParent(obj).transform;
            transform.GetComponentInChildren<Light>().enabled = state;
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
                ChangeLightning(surfaceParent, config.Lightning);
                ChangeOrderInLayer(surface, config.OrderInLayer);
                ChangeDescription(surface, config.Description);
                ChangeVisibility(surface, config.Visibility);
            }
        }
    }
}