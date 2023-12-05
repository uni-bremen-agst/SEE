using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.SEE.Game.UI.Drawable
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
        public static void Enable(GameObject stickyNoteHolder, GameObject hittedObject = null, UnityAction returnCall = null)
        {
            xRotationMenu = PrefabInstantiator.InstantiatePrefab(xRotationMenuPrefab,
                GameObject.Find("UI Canvas").transform, false);
            GameObject drawable = GameFinder.GetDrawable(stickyNoteHolder);
            string drawableParentID = GameFinder.GetDrawableParentName(drawable);
            GameFinder.FindChild(xRotationMenu, "Laying").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                if (hittedObject != null)
                {
                    GameStickyNoteManager.SetRotateX(stickyNoteHolder, 90, stickyNoteHolder.transform.position, hittedObject.name.Equals("Floor"));
                } else
                {
                    GameStickyNoteManager.SetRotateX(stickyNoteHolder, 90);
                    new StickyNoteRotateXNetAction(drawable.name, drawableParentID, 90).Execute();
                }
            });

            GameFinder.FindChild(xRotationMenu, "Hanging").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GameStickyNoteManager.SetRotateX(stickyNoteHolder, 0);
                if (hittedObject == null)
                {
                    new StickyNoteRotateXNetAction(drawable.name, drawableParentID, 0).Execute();
                }
            });

            GameFinder.FindChild(xRotationMenu, "Next").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                xRotationMenu.SetActive(false);
                EnableYRotation(stickyNoteHolder, hittedObject != null, returnCall);
            });

            if (returnCall != null)
            {
                GameFinder.FindChild(xRotationMenu, "ReturnBtn").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(returnCall);
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
            SliderListener(slider, stickyNoteHolder, stickyNoteHolder.transform.position, spawnMode);
            GameObject drawable = GameFinder.GetDrawable(stickyNoteHolder);
            string drawableParentID = GameFinder.GetDrawableParentName(drawable);

            if (returnCall != null)
            {
                GameFinder.FindChild(yRotationMenu, "ReturnBtn").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(returnCall);
            }
            else
            {
                GameFinder.FindChild(yRotationMenu, "ReturnBtn").SetActive(false);
            }

            GameFinder.FindChild(yRotationMenu, "0").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                slider.AssignValue(0);
                GameStickyNoteManager.SetRotateY(stickyNoteHolder, 0, stickyNoteHolder.transform.position);
                if (!spawnMode)
                {
                    new StickyNoteRoateYNetAction(drawable.name, drawableParentID, 0, stickyNoteHolder.transform.position).Execute();
                }
            });

            GameFinder.FindChild(yRotationMenu, "90").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                slider.AssignValue(90);
                GameStickyNoteManager.SetRotateY(stickyNoteHolder, 90, stickyNoteHolder.transform.position);
                if (!spawnMode)
                {
                    new StickyNoteRoateYNetAction(drawable.name, drawableParentID, 90, stickyNoteHolder.transform.position).Execute();
                }
            });

            GameFinder.FindChild(yRotationMenu, "180").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                slider.AssignValue(180);
                GameStickyNoteManager.SetRotateY(stickyNoteHolder, 180, stickyNoteHolder.transform.position);
                if (!spawnMode)
                {
                    new StickyNoteRoateYNetAction(drawable.name, drawableParentID, 180, stickyNoteHolder.transform.position).Execute();
                }
            });

            GameFinder.FindChild(yRotationMenu, "270").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                slider.AssignValue(270);
                GameStickyNoteManager.SetRotateY(stickyNoteHolder, 270, stickyNoteHolder.transform.position);
                if (!spawnMode)
                {
                    new StickyNoteRoateYNetAction(drawable.name, drawableParentID, 270, stickyNoteHolder.transform.position).Execute();
                }
            });

            GameFinder.FindChild(yRotationMenu, "Back").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                yRotationMenu.SetActive(false);
                xRotationMenu.SetActive(true);
            });

            GameFinder.FindChild(yRotationMenu, "Finish").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                Destroyer.Destroy(xRotationMenu);
                Destroyer.Destroy(yRotationMenu);
                isFinish = true;
            });
        }

        /// <summary>
        /// Adds the Handler for the y - Rotate Slider Controller.
        /// </summary>
        /// <param name="slider">The slider controller where the AddListener should be add.</param>
        /// <param name="stickyNote">The sticky note to rotate</param>
        private static void SliderListener(RotationSliderController slider, GameObject stickyNote, Vector3 oldPos, bool spawnMode)
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