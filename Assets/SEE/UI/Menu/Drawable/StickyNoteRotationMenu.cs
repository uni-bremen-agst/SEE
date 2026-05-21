using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
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
        /// The prefab of the sticky note for the x rotation menu.
        /// </summary>
        private const string xRotationMenuPrefab = "Prefabs/UI/Drawable/StickyNoteXRotation";

        /// <summary>
        /// The instance for the sticky note for the x rotation menu.
        /// </summary>
        private static GameObject xRotationMenu;

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
        /// Method to destroy the rotation menus.
        /// </summary>
        public static void Destroy()
        {
            if (yRotationMenu != null)
            {
                Destroyer.Destroy(yRotationMenu);
            }
            if (xRotationMenu != null)
            {
                Destroyer.Destroy(xRotationMenu);
            }
        }

        /// <summary>
        /// Enables the rotation menu. It beginns with the x-rotation menu.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note that should be rotated.</param>
        /// <param name="hitObject">The object where the sticky note was placed. Only necessary for spawning.</param>
        public static void Enable(GameObject stickyNoteHolder, GameObject hitObject = null,
            UnityAction returnCall = null)
        {
            xRotationMenu = Menu.InstantiatePrefab(xRotationMenuPrefab);
            GameObject surface = GameFinder.GetDrawableSurface(stickyNoteHolder);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

            /// Initialize the button for laying.
            LayingButton(stickyNoteHolder, hitObject, surface, surfaceParentName);

            /// Initialize the button for hanging.
            HangingButton(stickyNoteHolder, hitObject, surface, surfaceParentName);

            /// Initialize the next button, it opens the menu for the Y-rotation.
            GameFinder.FindChild(xRotationMenu, "Next").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                xRotationMenu.SetActive(false);
                EnableYRotation(stickyNoteHolder, hitObject != null, returnCall);
            });

            /// Initialize or disables the return button.
            XReturnButton(returnCall);
        }

        /// <summary>
        /// Initializes the laying button for the x rotation.
        /// It sets the x Euler angle to 90°.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder to be rotated.</param>
        /// <param name="hitObject">The hit object, is only in the spawn mode != null.</param>
        /// <param name="surface">The drawable surface of the sticky note holder.</param>
        /// <param name="surfaceParentName">The id of the sticky note parent.</param>
        private static void LayingButton(GameObject stickyNoteHolder, GameObject hitObject,
            GameObject surface, string surfaceParentName)
        {
            GameFinder.FindChild(xRotationMenu, "Laying").GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
                {
                    if (hitObject != null)
                    {
                        GameStickyNoteManager.SetRotateX(stickyNoteHolder, 90, stickyNoteHolder.transform.position,
                            hitObject.name.Equals("Floor"));
                    }
                    else
                    {
                        GameStickyNoteManager.SetRotateX(stickyNoteHolder, 90);
                        new StickyNoteRotateXNetAction(surface.name, surfaceParentName, 90).Execute();
                    }
                });
        }

        /// <summary>
        /// Initializes the hanging button for the x rotation.
        /// It sets the x Euler angle to 0°.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder to be rotated.</param>
        /// <param name="hitObject">The hit object, is only in the spawn mode != null.</param>
        /// <param name="surface">The drawable surface of the sticky note holder.</param>
        /// <param name="surfaceParentName">The id of the sticky note parent.</param>
        private static void HangingButton(GameObject stickyNoteHolder, GameObject hitObject,
            GameObject surface, string surfaceParentName)
        {
            GameFinder.FindChild(xRotationMenu, "Hanging").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GameStickyNoteManager.SetRotateX(stickyNoteHolder, 0);
                if (hitObject == null)
                {
                    new StickyNoteRotateXNetAction(surface.name, surfaceParentName, 0).Execute();
                }
            });
        }

        /// <summary>
        /// Sets up the return button for the xRotation menu if the <paramref name="returnCall"/> is not null.
        /// Otherwise the button will disabled.
        /// </summary>
        /// <param name="returnCall">The return call action to return to the parent menu.</param>
        private static void XReturnButton(UnityAction returnCall)
        {
            if (returnCall != null)
            {
                GameFinder.FindChild(xRotationMenu, "ReturnBtn").GetComponent<ButtonManagerBasic>()
                    .clickEvent.AddListener(returnCall);
            }
            else
            {
                GameFinder.FindChild(xRotationMenu, "ReturnBtn").SetActive(false);
            }
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
            GameFinder.FindChild(yRotationMenu, "Back").GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
            {
                yRotationMenu.SetActive(false);
                xRotationMenu.SetActive(true);
            });

            /// Initalize the finish button. It closes the rotation menus.
            /// And set <see cref="isFinished"/> to true.
            GameFinder.FindChild(yRotationMenu, "Finish").GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
            {
                Destroyer.Destroy(xRotationMenu);
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
            GameFinder.FindChild(yRotationMenu, "0").GetComponent<ButtonManagerBasic>()
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
            GameFinder.FindChild(yRotationMenu, "90").GetComponent<ButtonManagerBasic>()
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
            GameFinder.FindChild(yRotationMenu, "180").GetComponent<ButtonManagerBasic>()
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
            GameFinder.FindChild(yRotationMenu, "270").GetComponent<ButtonManagerBasic>()
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
                GameFinder.FindChild(yRotationMenu, "ReturnBtn").GetComponent<ButtonManagerBasic>()
                    .clickEvent.AddListener(returnCall);
            }
            else
            {
                GameFinder.FindChild(yRotationMenu, "ReturnBtn").SetActive(false);
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
