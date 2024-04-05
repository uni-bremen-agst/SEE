using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.UI.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Game.UI.Menu.Drawable
{
    /// <summary>
    /// Provides the rotation menus for the sticky notes.
    /// </summary>
    public class StickyNoteRotationMenu
    {
        /// <summary>
        /// The prefab of the sticky note x rotation menu.
        /// </summary>
        private const string xRotationMenuPrefab = "Prefabs/UI/Drawable/StickyNoteXRotation";

        /// <summary>
        /// The instance for the sticky note chose x rotation menu.
        /// </summary>
        private static GameObject xRotationMenu;

        /// <summary>
        /// The prefab of the sticky note y rotation menu.
        /// </summary>
        private const string yRotationMenuPrefab = "Prefabs/UI/Drawable/StickyNoteYRotation";

        /// <summary>
        /// The instance for the sticky note chose y rotation menu.
        /// </summary>
        private static GameObject yRotationMenu;

        /// <summary>
        /// Whether this class has a finished rotation in store that wasn't yet fetched.
        /// </summary>
        private static bool isFinish;

        /// <summary>
        /// Method to disable the rotation menu's
        /// </summary>
        public static void Disable()
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
        /// Enables the rotation menu. It beginns with the x - rotation menu.
        /// </summary>
        /// <param name="stickyNoteHolder">the sticky note that should be rotated.</param>
        /// <param name="hittedObject">The object where the sticky note was placed. only necessary for spawning.</param>
        public static void Enable(GameObject stickyNoteHolder, GameObject hittedObject = null,
            UnityAction returnCall = null)
        {
            xRotationMenu = PrefabInstantiator.InstantiatePrefab(xRotationMenuPrefab,
                GameObject.Find("UI Canvas").transform, false);
            GameObject drawable = GameFinder.GetDrawable(stickyNoteHolder);
            string drawableParentID = GameFinder.GetDrawableParentName(drawable);

            /// Initialize the button for laying.
            LayingButton(stickyNoteHolder, hittedObject, drawable, drawableParentID);

            /// Initialize the button for haning.
            HangingButton(stickyNoteHolder, hittedObject, drawable, drawableParentID);

            /// Initialize the next button, it opens the menu for the Y-rotation.
            GameFinder.FindChild(xRotationMenu, "Next").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                xRotationMenu.SetActive(false);
                EnableYRotation(stickyNoteHolder, hittedObject != null, returnCall);
            });

            /// Initialize or disables the return button.
            XReturnButton(returnCall);
        }

        /// <summary>
        /// Initialize the laying button for the x rotation.
        /// It set's the x euler angle to 90°.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder to be rotated.</param>
        /// <param name="hittedObject">The hitted object, is only in the spawn mode != null.</param>
        /// <param name="drawable">The drawable of the sticky note holder</param>
        /// <param name="drawableParentID">The id of the sticky note</param>
        private static void LayingButton(GameObject stickyNoteHolder, GameObject hittedObject,
            GameObject drawable, string drawableParentID)
        {
            GameFinder.FindChild(xRotationMenu, "Laying").GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
                {
                    if (hittedObject != null)
                    {
                        GameStickyNoteManager.SetRotateX(stickyNoteHolder, 90, stickyNoteHolder.transform.position,
                            hittedObject.name.Equals("Floor"));
                    }
                    else
                    {
                        GameStickyNoteManager.SetRotateX(stickyNoteHolder, 90);
                        new StickyNoteRotateXNetAction(drawable.name, drawableParentID, 90).Execute();
                    }
                });
        }

        /// <summary>
        /// Initialize the hanging button for the x rotation.
        /// It set's the x euler angle to 0°.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder to be rotated.</param>
        /// <param name="hittedObject">The hitted object, is only in the spawn mode != null.</param>
        /// <param name="drawable">The drawable of the sticky note holder</param>
        /// <param name="drawableParentID">The id of the sticky note</param>
        private static void HangingButton(GameObject stickyNoteHolder, GameObject hittedObject,
            GameObject drawable, string drawableParentID)
        {
            GameFinder.FindChild(xRotationMenu, "Hanging").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GameStickyNoteManager.SetRotateX(stickyNoteHolder, 0);
                if (hittedObject == null)
                {
                    new StickyNoteRotateXNetAction(drawable.name, drawableParentID, 0).Execute();
                }
            });
        }

        /// <summary>
        /// Sets up the return button for the xRotation menu, if the <paramref name="returnCall"/> is not null.
        /// Otherwise the button will disabled.
        /// </summary>
        /// <param name="returnCall">The return call action to return to the parent menu</param>
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
        /// Enables the menu for y - rotation.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note that should be rotated.</param>
        private static void EnableYRotation(GameObject stickyNoteHolder, bool spawnMode = true, UnityAction returnCall = null)
        {
            yRotationMenu = PrefabInstantiator.InstantiatePrefab(yRotationMenuPrefab,
                GameObject.Find("UI Canvas").transform, false);
            RotationSliderController slider = yRotationMenu.GetComponentInChildren<RotationSliderController>();
            SliderListener(slider, stickyNoteHolder, spawnMode);
            GameObject drawable = GameFinder.GetDrawable(stickyNoteHolder);
            string drawableParentID = GameFinder.GetDrawableParentName(drawable);

            /// Initialize or disables the return button.
            YReturnButton(returnCall);

            /// Initialize the different degrees button
            Zero(stickyNoteHolder, slider, spawnMode, drawable, drawableParentID);
            Ninety(stickyNoteHolder, slider, spawnMode, drawable, drawableParentID);
            OneHundredEighty(stickyNoteHolder, slider, spawnMode, drawable, drawableParentID);
            TwoHundredSeventy(stickyNoteHolder, slider, spawnMode, drawable, drawableParentID);

            /// Initialize the back button, it returns to the xRotation menu.
            GameFinder.FindChild(yRotationMenu, "Back").GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
            {
                yRotationMenu.SetActive(false);
                xRotationMenu.SetActive(true);
            });

            /// Initalize the finish button. It closes the rotation menus.
            /// And set <see cref="isFinish"/> to true.
            GameFinder.FindChild(yRotationMenu, "Finish").GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
            {
                Destroyer.Destroy(xRotationMenu);
                Destroyer.Destroy(yRotationMenu);
                isFinish = true;
            });
        }

        /// <summary>
        /// Initialize the zero button for the y rotation.
        /// It set's the y euler angle to 0°.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder to be rotated.</param>
        /// <param name="slider">The rotation slider.</param>
        /// <param name="spawnMode">if the menu was called from spawn action.</param>
        /// <param name="drawable">The drawable of the sticky note holder.</param>
        /// <param name="drawableParentID">The sticky note id.</param>
        private static void Zero(GameObject stickyNoteHolder, RotationSliderController slider,
            bool spawnMode, GameObject drawable, string drawableParentID)
        {
            GameFinder.FindChild(yRotationMenu, "0").GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
            {
                slider.AssignValue(0);
                GameStickyNoteManager.SetRotateY(stickyNoteHolder, 0, stickyNoteHolder.transform.position);
                if (!spawnMode)
                {
                    new StickyNoteRoateYNetAction(drawable.name, drawableParentID, 0,
                               stickyNoteHolder.transform.position).Execute();
                }
            });
        }

        /// <summary>
        /// Initialize the ninety button for the y rotation.
        /// It set's the y euler angle to 90°.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder to be rotated.</param>
        /// <param name="slider">The rotation slider.</param>
        /// <param name="spawnMode">if the menu was called from spawn action.</param>
        /// <param name="drawable">The drawable of the sticky note holder.</param>
        /// <param name="drawableParentID">The sticky note id.</param>
        private static void Ninety(GameObject stickyNoteHolder, RotationSliderController slider,
            bool spawnMode, GameObject drawable, string drawableParentID)
        {
            GameFinder.FindChild(yRotationMenu, "90").GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
                {
                    slider.AssignValue(90);
                    GameStickyNoteManager.SetRotateY(stickyNoteHolder, 90, stickyNoteHolder.transform.position);
                    if (!spawnMode)
                    {
                        new StickyNoteRoateYNetAction(drawable.name, drawableParentID, 90,
                                       stickyNoteHolder.transform.position).Execute();
                    }
                });
        }

        /// <summary>
        /// Initialize the one hundred eighty button for the y rotation.
        /// It set's the y euler angle to 180°.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder to be rotated.</param>
        /// <param name="slider">The rotation slider.</param>
        /// <param name="spawnMode">if the menu was called from spawn action.</param>
        /// <param name="drawable">The drawable of the sticky note holder.</param>
        /// <param name="drawableParentID">The sticky note id.</param>
        private static void OneHundredEighty(GameObject stickyNoteHolder, RotationSliderController slider,
            bool spawnMode, GameObject drawable, string drawableParentID)
        {
            GameFinder.FindChild(yRotationMenu, "180").GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
                {
                    slider.AssignValue(180);
                    GameStickyNoteManager.SetRotateY(stickyNoteHolder, 180, stickyNoteHolder.transform.position);
                    if (!spawnMode)
                    {
                        new StickyNoteRoateYNetAction(drawable.name, drawableParentID, 180,
                            stickyNoteHolder.transform.position).Execute();
                    }
                });
        }

        /// <summary>
        /// Initialize the two hundred seventy button for the y rotation.
        /// It set's the y euler angle to 270°.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder to be rotated.</param>
        /// <param name="slider">The rotation slider.</param>
        /// <param name="spawnMode">if the menu was called from spawn action.</param>
        /// <param name="drawable">The drawable of the sticky note holder.</param>
        /// <param name="drawableParentID">The sticky note id.</param>
        private static void TwoHundredSeventy(GameObject stickyNoteHolder, RotationSliderController slider,
            bool spawnMode, GameObject drawable, string drawableParentID)
        {
            GameFinder.FindChild(yRotationMenu, "270").GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
                {
                    slider.AssignValue(270);
                    GameStickyNoteManager.SetRotateY(stickyNoteHolder, 270, stickyNoteHolder.transform.position);
                    if (!spawnMode)
                    {
                        new StickyNoteRoateYNetAction(drawable.name, drawableParentID, 270,
                            stickyNoteHolder.transform.position).Execute();
                    }
                });
        }

        /// <summary>
        /// Sets up the return button for the yRotation menu, if the <paramref name="returnCall"/> is not null.
        /// Otherwise the button will disabled.
        /// </summary>
        /// <param name="returnCall">The return call action to return to the parent menu</param>
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
        /// Adds the Handler for the y - Rotate Slider Controller.
        /// </summary>
        /// <param name="slider">The slider controller where the AddListener should be add.</param>
        /// <param name="stickyNote">The sticky note to rotate</param>
        /// <param name="spawnMode">true, if the menu was called from the sticky note spawn action.</param>
        private static void SliderListener(RotationSliderController slider, GameObject stickyNote, bool spawnMode)
        {
            GameObject drawable = GameFinder.GetDrawable(stickyNote);
            string drawableParentID = GameFinder.GetDrawableParentName(drawable);

            Transform transform = stickyNote.transform;

            slider.AssignValue(transform.localEulerAngles.y);
            slider.onValueChanged.AddListener(degree =>
            {
                GameStickyNoteManager.SetRotateY(stickyNote, degree, stickyNote.transform.position);
                if (!spawnMode)
                {
                    new StickyNoteRoateYNetAction(drawable.name, drawableParentID, degree, stickyNote.transform.position).Execute();
                }
            });
        }

        /// <summary>
        /// If <see cref="isFinish"/> is true, the <paramref name="finish"/> will be the state. Otherwise it will be false.
        /// </summary>
        /// <param name="finish">The finish state</param>
        /// <returns><see cref="isFinish"/></returns>
        public static bool TryGetFinish(out bool finish)
        {
            if (isFinish)
            {
                finish = isFinish;
                isFinish = false;
                return true;
            }

            finish = false;
            return false;
        }

        /// <summary>
        /// Returns the state if the y rotation menu is enabled.
        /// </summary>
        /// <returns>true, if the menu not null and visible.</returns>
        public static bool IsYActive()
        {
            return yRotationMenu != null && yRotationMenu.activeInHierarchy;
        }

        /// <summary>
        /// Assigns a new degree to the <see cref="RotationSliderController"/> of the Y-Rotation Menu.
        /// </summary>
        /// <param name="degree">The new degree</param>
        public static void AssignValueToYSlider(float degree)
        {
            if (yRotationMenu != null)
            {
                yRotationMenu.GetComponentInChildren<RotationSliderController>().AssignValue(degree);
            }
        }
    }
}