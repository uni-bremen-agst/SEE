using UnityEngine;

namespace Assets.SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// This class provides various reused queries for the drawable actions.
    /// </summary>
    public static class DrawableQuery
    {
        /// <summary>
        /// Registers the users left mouse button input.
        /// </summary>
        /// <returns>true if the user uses the left mouse button.</returns>
        public static bool LeftMouseInteraction()
        {
            return (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0));
        }
    }
}