using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.StickyNote;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable.StickyNoteRotation
{
    /// <summary>
    /// Provides the menu for configuring the x rotation of a sticky note.
    /// </summary>
    internal sealed class StickyNoteXRotationMenu
    {
        /// <summary>
        /// The prefab of the sticky note x rotation menu.
        /// </summary>
        private const string xRotationMenuPrefab =
            "Prefabs/UI/Drawable/StickyNoteXRotation";

        /// <summary>
        /// The instantiated sticky note x rotation menu.
        /// </summary>
        private GameObject xRotationMenu;

        /// <summary>
        /// Enables the x rotation menu for the given sticky note.
        /// </summary>
        /// <param name="stickyNoteHolder">
        /// The sticky note holder that should be rotated.
        /// </param>
        /// <param name="hitObject">
        /// The object on which the sticky note was placed.
        /// This is only required while spawning a sticky note.
        /// </param>
        /// <param name="returnCall">
        /// An optional action returning to the parent menu.
        /// </param>
        /// <param name="nextCall">
        /// The action invoked when the user continues to the y rotation menu.
        /// </param>
        internal void Enable(
            GameObject stickyNoteHolder,
            GameObject hitObject,
            UnityAction returnCall,
            UnityAction nextCall)
        {
            xRotationMenu =
                Menu.InstantiatePrefab(xRotationMenuPrefab);

            GameObject surface =
                GameFinder.GetDrawableSurface(stickyNoteHolder);

            string surfaceParentName =
                GameFinder.GetDrawableSurfaceParentName(surface);

            SetUpLayingButton(
                stickyNoteHolder,
                hitObject,
                surface,
                surfaceParentName);

            SetUpHangingButton(
                stickyNoteHolder,
                hitObject,
                surface,
                surfaceParentName);

            SetUpNextButton(nextCall);
            SetUpReturnButton(returnCall);
        }

        /// <summary>
        /// Shows the x rotation menu if it has already been instantiated.
        /// </summary>
        internal void Show()
        {
            if (xRotationMenu != null)
            {
                xRotationMenu.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the x rotation menu if it has already been instantiated.
        /// </summary>
        internal void Hide()
        {
            if (xRotationMenu != null)
            {
                xRotationMenu.SetActive(false);
            }
        }

        /// <summary>
        /// Destroys the x rotation menu if it has been instantiated.
        /// </summary>
        internal void Destroy()
        {
            if (xRotationMenu != null)
            {
                Destroyer.Destroy(xRotationMenu);
            }
        }

        /// <summary>
        /// Sets up the button that places the sticky note in a laying
        /// orientation by assigning an x rotation of 90 degrees.
        /// </summary>
        /// <param name="stickyNoteHolder">
        /// The sticky note holder that should be rotated.
        /// </param>
        /// <param name="hitObject">
        /// The hit object used while spawning the sticky note.
        /// </param>
        /// <param name="surface">
        /// The drawable surface containing the sticky note.
        /// </param>
        /// <param name="surfaceParentName">
        /// The name of the parent of the drawable surface.
        /// </param>
        private void SetUpLayingButton(
            GameObject stickyNoteHolder,
            GameObject hitObject,
            GameObject surface,
            string surfaceParentName)
        {
            GameFinder.FindAttachedOrLocalDescendant(
                    xRotationMenu,
                    "Laying")
                .GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
                {
                    if (hitObject != null)
                    {
                        GameStickyNoteTransform.SetRotateX(
                            stickyNoteHolder,
                            90,
                            stickyNoteHolder.transform.position,
                            hitObject.name.Equals("Floor"));
                    }
                    else
                    {
                        GameStickyNoteTransform.SetRotateX(
                            stickyNoteHolder,
                            90);

                        new StickyNoteRotateXNetAction(
                            surface.name,
                            surfaceParentName,
                            90).Execute();
                    }
                });
        }

        /// <summary>
        /// Sets up the button that places the sticky note in a hanging
        /// orientation by assigning an x rotation of zero degrees.
        /// </summary>
        /// <param name="stickyNoteHolder">
        /// The sticky note holder that should be rotated.
        /// </param>
        /// <param name="hitObject">
        /// The hit object used while spawning the sticky note.
        /// </param>
        /// <param name="surface">
        /// The drawable surface containing the sticky note.
        /// </param>
        /// <param name="surfaceParentName">
        /// The name of the parent of the drawable surface.
        /// </param>
        private void SetUpHangingButton(
            GameObject stickyNoteHolder,
            GameObject hitObject,
            GameObject surface,
            string surfaceParentName)
        {
            GameFinder.FindAttachedOrLocalDescendant(
                    xRotationMenu,
                    "Hanging")
                .GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
                {
                    GameStickyNoteTransform.SetRotateX(
                        stickyNoteHolder,
                        0);

                    if (hitObject == null)
                    {
                        new StickyNoteRotateXNetAction(
                            surface.name,
                            surfaceParentName,
                            0).Execute();
                    }
                });
        }

        /// <summary>
        /// Sets up the button that continues to the y rotation menu.
        /// </summary>
        /// <param name="nextCall">
        /// The action invoked when the next button is pressed.
        /// </param>
        private void SetUpNextButton(UnityAction nextCall)
        {
            GameFinder.FindAttachedOrLocalDescendant(
                    xRotationMenu,
                    "Next")
                .GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(nextCall);
        }

        /// <summary>
        /// Sets up the return button if a return action is available.
        /// Otherwise, the button is hidden.
        /// </summary>
        /// <param name="returnCall">
        /// The optional action returning to the parent menu.
        /// </param>
        private void SetUpReturnButton(UnityAction returnCall)
        {
            GameObject returnButton =
                GameFinder.FindAttachedOrLocalDescendant(
                    xRotationMenu,
                    "ReturnBtn");

            if (returnCall != null)
            {
                returnButton
                    .GetComponent<ButtonManagerBasic>()
                    .clickEvent.AddListener(returnCall);
            }
            else
            {
                returnButton.SetActive(false);
            }
        }
    }
}
