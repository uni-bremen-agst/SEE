using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using UnityEngine;

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
        /// <param name="hittedObject">The object where the sticky note was placed. only necressary for spawning.</param>
        public static void Enable(GameObject stickyNoteHolder, GameObject hittedObject = null)
        {
            xRotationMenu = PrefabInstantiator.InstantiatePrefab(xRotationMenuPrefab,
                GameObject.Find("UI Canvas").transform, false);
            Vector3 oldPos = stickyNoteHolder.transform.position;
            GameObject drawable = GameFinder.FindDrawable(stickyNoteHolder);
            string drawableParentID = GameFinder.GetDrawableParentName(drawable);
            GameFinder.FindChild(xRotationMenu, "Laying").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                if (hittedObject != null)
                {
                    GameStickyNoteManager.SetRotateX(stickyNoteHolder, 90, oldPos, hittedObject.name.Equals("Floor"));
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
                EnableYRotation(stickyNoteHolder, hittedObject != null);
            });
        }

        /// <summary>
        /// Enables the menu for y - rotation.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note that should be rotated.</param>
        private static void EnableYRotation(GameObject stickyNoteHolder, bool spawnMode = true)
        {
            yRotationMenu = PrefabInstantiator.InstantiatePrefab(yRotationMenuPrefab,
                GameObject.Find("UI Canvas").transform, false);
            RotationSliderController slider = yRotationMenu.GetComponentInChildren<RotationSliderController>();
            Vector3 oldPos = stickyNoteHolder.transform.position;
            SliderListener(slider, stickyNoteHolder, oldPos, spawnMode);
            GameObject drawable = GameFinder.FindDrawable(stickyNoteHolder);
            string drawableParentID = GameFinder.GetDrawableParentName(drawable);


            GameFinder.FindChild(yRotationMenu, "0").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                slider.AssignValue(0);
                GameStickyNoteManager.SetRotateY(stickyNoteHolder, 0, oldPos);
                if (!spawnMode)
                {
                    new StickyNoteRoateYNetAction(drawable.name, drawableParentID, 0, oldPos).Execute();
                }
            });

            GameFinder.FindChild(yRotationMenu, "90").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                slider.AssignValue(90);
                GameStickyNoteManager.SetRotateY(stickyNoteHolder, 90, oldPos);
                if (!spawnMode)
                {
                    new StickyNoteRoateYNetAction(drawable.name, drawableParentID, 90, oldPos).Execute();
                }
            });

            GameFinder.FindChild(yRotationMenu, "180").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                slider.AssignValue(180);
                GameStickyNoteManager.SetRotateY(stickyNoteHolder, 180, oldPos);
                if (!spawnMode)
                {
                    new StickyNoteRoateYNetAction(drawable.name, drawableParentID, 180, oldPos).Execute();
                }
            });

            GameFinder.FindChild(yRotationMenu, "270").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                slider.AssignValue(270);
                GameStickyNoteManager.SetRotateY(stickyNoteHolder, 270, oldPos);
                if (!spawnMode)
                {
                    new StickyNoteRoateYNetAction(drawable.name, drawableParentID, 270, oldPos).Execute();
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
                StickyNoteAction.finish = true;
            });
        }

        /// <summary>
        /// Adds the Handler for the y - Rotate Slider Controller.
        /// </summary>
        /// <param name="slider">The slider controller where the AddListener should be add.</param>
        /// <param name="stickyNote">The sticky note to rotate</param>
        private static void SliderListener(RotationSliderController slider, GameObject stickyNote, Vector3 oldPos, bool spawnMode)
        {
            GameObject drawable = GameFinder.FindDrawable(stickyNote);
            string drawableParentID = GameFinder.GetDrawableParentName(drawable);

            Transform transform = stickyNote.transform;

            slider.AssignValue(transform.localEulerAngles.y);
            slider.onValueChanged.AddListener(degree =>
            {
                GameStickyNoteManager.SetRotateY(stickyNote, degree, oldPos);
                if (!spawnMode)
                {
                    new StickyNoteRoateYNetAction(drawable.name, drawableParentID, degree, oldPos).Execute();
                }
            });
        }
    }
}