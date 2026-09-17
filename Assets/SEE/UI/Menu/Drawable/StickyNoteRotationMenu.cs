using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable.StickyNoteRotation;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable
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
        /// The prefab of the sticky note for the y rotation menu.
        /// </summary>
        private const string yRotationMenuPrefab = "Prefabs/UI/Drawable/StickyNoteYRotation";

        /// <summary>
        /// The instance for the sticky note for the y rotation menu.
        /// </summary>
        private static GameObject yRotationMenu;

        /// <summary>
        /// Whether this class has a finished rotation that hasn't been fetched yet.
        /// </summary>
        private static bool isFinished;

        /// <summary>
        /// Destroys the sticky note rotation menus.
        /// </summary>
        public static void Destroy()
        {
            if (yRotationMenu != null)
            {
                Destroyer.Destroy(yRotationMenu);
            }

            xRotationMenu.Destroy();
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

                    EnableYRotation(
                        stickyNoteHolder,
                        hitObject != null,
                        returnCall);
                });
        }

        /// <summary>
        /// Enables the menu for y-rotation.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note that should be rotated.</param>
        private static void EnableYRotation(GameObject stickyNoteHolder, bool spawnMode = true, UnityAction returnCall = null)
        {
            yRotationMenu = Menu.InstantiatePrefab(yRotationMenuPrefab);
            RotationSliderController slider = yRotationMenu.GetComponentInChildren<RotationSliderController>();
            SliderListener(slider, stickyNoteHolder, spawnMode);
            GameObject surface = GameFinder.GetDrawableSurface(stickyNoteHolder);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

            /// Initialize or disables the return button.
            YReturnButton(returnCall);

            /// Initialize the different degrees button
            Zero(stickyNoteHolder, slider, spawnMode, surface, surfaceParentName);
            Ninety(stickyNoteHolder, slider, spawnMode, surface, surfaceParentName);
            OneHundredEighty(stickyNoteHolder, slider, spawnMode, surface, surfaceParentName);
            TwoHundredSeventy(stickyNoteHolder, slider, spawnMode, surface, surfaceParentName);

            /// Initialize the back button, it returns to the xRotation menu.
            GameFinder.FindAttachedOrLocalDescendant(
                    yRotationMenu,
                    "Back")
                .GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
                {
                    yRotationMenu.SetActive(false);
                    xRotationMenu.Show();
                });

            /// Initalize the finish button. It closes the rotation menus.
            /// And set <see cref="isFinished"/> to true.
            GameFinder.FindAttachedOrLocalDescendant(yRotationMenu, "Finish").GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
            {
                xRotationMenu.Destroy();
                Destroyer.Destroy(yRotationMenu);
                isFinished = true;
            });
        }

        /// <summary>
        /// Initializes the zero button for the y-rotation.
        /// It sets the y Euler angle to 0°.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder to be rotated.</param>
        /// <param name="slider">The rotation slider.</param>
        /// <param name="spawnMode">Whether the menu was called from a spawn action.</param>
        /// <param name="surface">The drawable surface of the sticky note holder.</param>
        /// <param name="surfaceParentName">The sticky note id.</param>
        private static void Zero(GameObject stickyNoteHolder, RotationSliderController slider,
            bool spawnMode, GameObject surface, string surfaceParentName)
        {
            GameFinder.FindAttachedOrLocalDescendant(yRotationMenu, "0").GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
            {
                slider.AssignValue(0);
                GameStickyNoteManager.SetRotateY(stickyNoteHolder, 0);
                if (!spawnMode)
                {
                    new StickyNoteRoateYNetAction(surface.name, surfaceParentName, 0,
                               stickyNoteHolder.transform.position).Execute();
                }
            });
        }

        /// <summary>
        /// Initializes the ninety button for the y rotation.
        /// It sets the y Euler angle to 90°.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder to be rotated.</param>
        /// <param name="slider">The rotation slider.</param>
        /// <param name="spawnMode">Whether the menu was called from a spawn action.</param>
        /// <param name="surface">The drawable surface of the sticky note holder.</param>
        /// <param name="surfaceParentName">The sticky note id.</param>
        private static void Ninety(GameObject stickyNoteHolder, RotationSliderController slider,
            bool spawnMode, GameObject surface, string surfaceParentName)
        {
            GameFinder.FindAttachedOrLocalDescendant(yRotationMenu, "90").GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
                {
                    slider.AssignValue(90);
                    GameStickyNoteManager.SetRotateY(stickyNoteHolder, 90);
                    if (!spawnMode)
                    {
                        new StickyNoteRoateYNetAction(surface.name, surfaceParentName, 90,
                                       stickyNoteHolder.transform.position).Execute();
                    }
                });
        }

        /// <summary>
        /// Initializes the one hundred eighty button for the y-rotation.
        /// It sets the y Euler angle to 180°.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder to be rotated.</param>
        /// <param name="slider">The rotation slider.</param>
        /// <param name="spawnMode">Whether the menu was called from a spawn action.</param>
        /// <param name="surface">The drawable surface of the sticky note holder.</param>
        /// <param name="surfaceParentName">The sticky note id.</param>
        private static void OneHundredEighty(GameObject stickyNoteHolder, RotationSliderController slider,
            bool spawnMode, GameObject surface, string surfaceParentName)
        {
            GameFinder.FindAttachedOrLocalDescendant(yRotationMenu, "180").GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
                {
                    slider.AssignValue(180);
                    GameStickyNoteManager.SetRotateY(stickyNoteHolder, 180);
                    if (!spawnMode)
                    {
                        new StickyNoteRoateYNetAction(surface.name, surfaceParentName, 180,
                            stickyNoteHolder.transform.position).Execute();
                    }
                });
        }

        /// <summary>
        /// Initializes the two hundred seventy button for the y rotation.
        /// It sets the y euler angle to 270°.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder to be rotated.</param>
        /// <param name="slider">The rotation slider.</param>
        /// <param name="spawnMode">Whether the menu was called from a spawn action.</param>
        /// <param name="surface">The drawable surface of the sticky note holder.</param>
        /// <param name="surfaceParentName">The sticky note id.</param>
        private static void TwoHundredSeventy(GameObject stickyNoteHolder, RotationSliderController slider,
            bool spawnMode, GameObject surface, string surfaceParentName)
        {
            GameFinder.FindAttachedOrLocalDescendant(yRotationMenu, "270").GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
                {
                    slider.AssignValue(270);
                    GameStickyNoteManager.SetRotateY(stickyNoteHolder, 270);
                    if (!spawnMode)
                    {
                        new StickyNoteRoateYNetAction(surface.name, surfaceParentName, 270,
                            stickyNoteHolder.transform.position).Execute();
                    }
                });
        }

        /// <summary>
        /// Sets up the return button for the yRotation menu if the <paramref name="returnCall"/> is not null.
        /// Otherwise the button will disabled.
        /// </summary>
        /// <param name="returnCall">The return call action to return to the parent menu.</param>
        private static void YReturnButton(UnityAction returnCall)
        {
            if (returnCall != null)
            {
                GameFinder.FindAttachedOrLocalDescendant(yRotationMenu, "ReturnBtn").GetComponent<ButtonManagerBasic>()
                    .clickEvent.AddListener(returnCall);
            }
            else
            {
                GameFinder.FindAttachedOrLocalDescendant(yRotationMenu, "ReturnBtn").SetActive(false);
            }
        }

        /// <summary>
        /// Adds the handler for the y-Rotate Slider Controller.
        /// </summary>
        /// <param name="slider">The slider controller where the AddListener should be add.</param>
        /// <param name="stickyNote">The sticky note to rotate.</param>
        /// <param name="spawnMode">True, if the menu was called from the sticky note spawn action.</param>
        private static void SliderListener(RotationSliderController slider, GameObject stickyNote, bool spawnMode)
        {
            GameObject surface = GameFinder.GetDrawableSurface(stickyNote);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

            Transform transform = stickyNote.transform;

            slider.AssignValue(transform.localEulerAngles.y);
            slider.OnValueChanged.AddListener(degree =>
            {
                GameStickyNoteManager.SetRotateY(stickyNote, degree);
                if (!spawnMode)
                {
                    new StickyNoteRoateYNetAction(surface.name, surfaceParentName, degree, stickyNote.transform.position).Execute();
                }
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
        /// Returns true if the y rotation menu is enabled.
        /// </summary>
        /// <returns>True if the menu is not null and visible.</returns>
        public static bool IsYActive()
        {
            return yRotationMenu != null && yRotationMenu.activeInHierarchy;
        }

        /// <summary>
        /// Assigns a new degree to the <see cref="RotationSliderController"/> of the Y-Rotation Menu.
        /// </summary>
        /// <param name="degree">The new degree.</param>
        public static void AssignValueToYSlider(float degree)
        {
            if (yRotationMenu != null)
            {
                yRotationMenu.GetComponentInChildren<RotationSliderController>().AssignValue(degree);
            }
        }
    }
}
