using SEE.Game;
using UnityEngine;
using static SEE.Game.Portal.IncludeDescendants;

namespace SEE.Extensions
{
    /// <summary>
    /// Provides extensions for <see cref="GameObject"/>s.
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Enables/disables the renderers of <paramref name="gameObject"/> and all its
        /// descendants so that they become visible/invisible.
        /// </summary>
        /// <param name="gameObject">Objects whose renderer (and those of its children) is to be enabled/disabled.</param>
        /// <param name="isVisible">Iff true, the renderers will be enabled.</param>
        /// <remarks>Applicable only to a <see cref="GameObject"/> and its descendants
        /// with a <see cref="Renderer"/></remarks>
        private static void SetVisible(this GameObject gameObject, bool isVisible)
        {
            gameObject.GetComponent<Renderer>().enabled = isVisible;
            foreach (Transform child in gameObject.transform)
            {
                SetVisible(child.gameObject, isVisible);
            }
        }

        /// <summary>
        /// Returns the full name of the game object, that is, its name and the
        /// names of its ancestors in the game-object hierarchy separated by /.
        /// If <paramref name="gameObject"/> is null, "<NULL>" will be returned.
        /// </summary>
        /// <param name="gameObject">Game object for which to retrieve the full name.
        /// Can be null.</param>
        public static string FullName(this GameObject gameObject)
        {
            if (gameObject == null)
            {
                return "<NULL>";
            }
            string result = gameObject.name;
            while (gameObject.transform.parent != null)
            {
                gameObject = gameObject.transform.parent.gameObject;
                result = gameObject.name + "/" + result;
            }
            return result;
        }

        /// <summary>
        /// Updates the portal of this game object by setting the portal boundaries of itself
        /// (and its descendants, depending on <paramref name="includeDescendants"/>)
        /// to the code city they're contained in.
        /// If they're not contained in a code city and <paramref name="warnOnFailure"/> is true,
        /// a warning log message will be emitted, otherwise nothing will happen.
        /// </summary>
        /// <param name="gameObject">The game object whose portal shall be updated.</param>
        /// <param name="warnOnFailure">
        /// Whether a warning log message shall be emitted if the <paramref name="gameObject"/>
        /// is not attached to any code city.
        /// </param>
        /// <param name="includeDescendants">
        /// Whether the portal of the descendants of this <paramref name="gameObject"/> shall be updated too.
        /// </param>
        /// <remarks>The <paramref name="gameObject"/> can be a game node or game edge
        /// or anything else that has a portal.</remarks>
        public static void UpdatePortal(this GameObject gameObject, bool warnOnFailure = false,
                                        Portal.IncludeDescendants includeDescendants = OnlySelf)
        {
            GameObject rootCity = gameObject.GetCodeCity();
            if (rootCity != null)
            {
                Portal.SetPortal(rootCity, gameObject, includeDescendants);
            }
            else if (warnOnFailure)
            {
                Debug.LogWarning("Couldn't update portal: No code city has been found"
                                 + $" attached to game object {gameObject.FullName()}.\n");
            }
        }
    }
}
