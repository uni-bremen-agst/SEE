using UnityEngine;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// This class provides various reused queries for the drawable actions.
    /// </summary>
    public static class Queries
    {
        /// <summary>
        /// Registers the users left mouse button input.
        /// </summary>
        /// <returns>true if the user uses the left mouse button.</returns>
        public static bool LeftMouseInteraction()
        {
            return Input.GetMouseButton(0);
        }

        /// <summary>
        /// Registers the users left mouse down input.
        /// </summary>
        /// <remarks>
        /// This is only <c>true</c> in the exact frame the mouse button is pressed down.
        /// </remarks>
        /// <returns>True if the user uses left mouse down.</returns>
        public static bool LeftMouseDown()
        {
            return Input.GetMouseButtonDown(0);
        }

        /// <summary>
        /// Registers the users right mouse button input.
        /// </summary>
        /// <returns>true if the user uses the left mouse button.</returns>
        public static bool RightMouseInteraction()
        {
            return Input.GetMouseButton(1);
        }

        /// <summary>
        /// Registers the uses mouse button up input (release the selected button).
        /// </summary>
        /// <param name="state">The mouse button which should be observed.</param>
        /// <returns>true if the user releases the selected mouse button.</returns>
        public static bool MouseUp(MouseButton state)
        {
            return Input.GetMouseButtonUp((int)state);
        }

        /// <summary>
        /// Registers the uses mouse button input (button holded).
        /// </summary>
        /// <param name="state">The mouse button which should be observed.</param>
        /// <returns>true if the user holds the selected mouse button.</returns>
        public static bool MouseHold(MouseButton state)
        {
            return Input.GetMouseButton((int)state);
        }

        /// <summary>
        /// Registers the uses mouse down button input (button down).
        /// </summary>
        /// <param name="state">The mouse down button which should be observed.</param>
        /// <returns>true if the user press the selected mouse button.</returns>
        public static bool MouseDown(MouseButton state)
        {
            return Input.GetMouseButtonDown((int)state);
        }

        /// <summary>
        /// Registers the use of the mouse wheel for scrolling down.
        /// </summary>
        /// <returns>True if the user scrolls down.</returns>
        public static bool ScrollDown()
        {
            return Input.mouseScrollDelta.y <= -0.1;
        }

        /// <summary>
        /// Registers the use of the mouse wheel for scrolling up.
        /// </summary>
        /// <returns>True if the user scrolls up.</returns>
        public static bool ScrollUp()
        {
            return Input.mouseScrollDelta.y >= 0.1;
        }

        /// <summary>
        /// Checks if the given drawable surface object is the same object as the other one.
        /// </summary>
        /// <param name="surface">The drawable surface to be checked.</param>
        /// <param name="other">>The other object.</param>
        /// <returns>True if the drawable surface is the same as the other object.</returns>
        public static bool SameDrawableSurface(GameObject surface, GameObject other)
        {
            return surface != null && GameFinder.GetDrawableSurface(other).Equals(surface);
        }

        /// <summary>
        /// Checks if the given drawable surface is null or the same object as the other <see cref="GameObject"/>.
        /// </summary>
        /// <param name="surface">The drawable surface to be checked.</param>
        /// <param name="other">The other object.</param>
        /// <returns>True if the drawable surface is null or the same as the other object.</returns>
        public static bool DrawableSurfaceNullOrSame(GameObject surface, GameObject other)
        {
            return surface == null || SameDrawableSurface(surface, other);
        }
    }
}
