using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable.StickyNoteRotation
{
    /// <summary>
    /// Provides the rotation menus for the sticky notes.
    /// </summary>
    public class StickyNoteRotationMenu
    {
        /// <summary>
        /// Manages the sticky note x rotation menu.
        /// </summary>
        private static readonly StickyNoteXRotationMenu xRotationMenu = new();

        /// <summary>
        /// Manages the sticky note y rotation menu.
        /// </summary>
        private static readonly StickyNoteYRotationMenu yRotationMenu = new();

        /// <summary>
        /// Whether this class has a finished rotation that hasn't been fetched yet.
        /// </summary>
        private static bool isFinished;

        /// <summary>
        /// Destroys the sticky note rotation menus.
        /// </summary>
        public static void Destroy()
        {
            xRotationMenu.Destroy();
            yRotationMenu.Destroy();
        }

        /// <summary>
        /// Enables the sticky note rotation menu.
        /// The rotation configuration starts with the x rotation menu.
        /// </summary>
        /// <param name="stickyNoteHolder">
        /// The sticky note that should be rotated.
        /// </param>
        /// <param name="hitObject">
        /// The object on which the sticky note was placed.
        /// This is only required while spawning a sticky note.
        /// </param>
        /// <param name="returnCall">
        /// An optional action returning to the parent menu.
        /// </param>
        public static void Enable(
            GameObject stickyNoteHolder,
            GameObject hitObject = null,
            UnityAction returnCall = null)
        {
            xRotationMenu.Enable(
                stickyNoteHolder,
                hitObject,
                returnCall,
                () =>
                {
                    xRotationMenu.Hide();

                    yRotationMenu.Enable(
                        stickyNoteHolder,
                        hitObject != null,
                        returnCall,
                        () =>
                        {
                            yRotationMenu.Hide();
                            xRotationMenu.Show();
                        },
                        () =>
                        {
                            xRotationMenu.Destroy();
                            yRotationMenu.Destroy();
                            isFinished = true;
                        });
                });
        }

        /// <summary>
        /// If <see cref="isFinished"/> is true, the <paramref name="finish"/> will be the state.
        /// Otherwise it will be false.
        /// </summary>
        /// <param name="finish">The finish state.</param>
        /// <returns><see cref="isFinished"/>.</returns>
        public static bool TryGetFinish(out bool finish)
        {
            if (isFinished)
            {
                finish = isFinished;
                isFinished = false;
                return true;
            }

            finish = false;
            return false;
        }

        /// <summary>
        /// Returns whether the y rotation menu is enabled.
        /// </summary>
        /// <returns>
        /// True if the y rotation menu is active; otherwise, false.
        /// </returns>
        public static bool IsYActive()
        {
            return yRotationMenu.IsActive();
        }

        /// <summary>
        /// Assigns a new degree to the y rotation slider.
        /// </summary>
        /// <param name="degree">
        /// The new rotation value in degrees.
        /// </param>
        public static void AssignValueToYSlider(float degree)
        {
            yRotationMenu.AssignValueToSlider(degree);
        }
    }
}
