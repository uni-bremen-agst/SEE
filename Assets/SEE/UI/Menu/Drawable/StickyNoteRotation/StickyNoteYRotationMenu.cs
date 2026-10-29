using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.StickyNote;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable.StickyNoteRotation
{
    /// <summary>
    /// Provides the menu for configuring the y rotation of a sticky note.
    /// </summary>
    internal sealed class StickyNoteYRotationMenu
    {
        /// <summary>
        /// The prefab of the sticky note y rotation menu.
        /// </summary>
        private const string yRotationMenuPrefab =
            "Prefabs/UI/Drawable/StickyNoteYRotation";

        /// <summary>
        /// The instantiated sticky note y rotation menu.
        /// </summary>
        private GameObject yRotationMenu;

        /// <summary>
        /// Enables the y rotation menu for the given sticky note.
        /// </summary>
        /// <param name="stickyNoteHolder">
        /// The sticky note holder that should be rotated.
        /// </param>
        /// <param name="spawnMode">
        /// Whether the menu was opened while spawning a sticky note.
        /// </param>
        /// <param name="returnCall">
        /// An optional action returning to the parent menu.
        /// </param>
        /// <param name="backCall">
        /// The action invoked when returning to the x rotation menu.
        /// </param>
        /// <param name="finishCall">
        /// The action invoked when the rotation configuration is finished.
        /// </param>
        internal void Enable(
            GameObject stickyNoteHolder,
            bool spawnMode,
            UnityAction returnCall,
            UnityAction backCall,
            UnityAction finishCall)
        {
            yRotationMenu =
                Menu.InstantiatePrefab(yRotationMenuPrefab);

            RotationSliderController slider =
                yRotationMenu.GetComponentInChildren<RotationSliderController>();

            SetUpSlider(
                slider,
                stickyNoteHolder,
                spawnMode);

            GameObject surface =
                GameFinder.GetDrawableSurface(stickyNoteHolder);

            string surfaceParentName =
                GameFinder.GetDrawableSurfaceParentName(surface);

            SetUpReturnButton(returnCall);

            SetUpDegreeButton(
                "0",
                0,
                stickyNoteHolder,
                slider,
                spawnMode,
                surface,
                surfaceParentName);

            SetUpDegreeButton(
                "90",
                90,
                stickyNoteHolder,
                slider,
                spawnMode,
                surface,
                surfaceParentName);

            SetUpDegreeButton(
                "180",
                180,
                stickyNoteHolder,
                slider,
                spawnMode,
                surface,
                surfaceParentName);

            SetUpDegreeButton(
                "270",
                270,
                stickyNoteHolder,
                slider,
                spawnMode,
                surface,
                surfaceParentName);

            SetUpBackButton(backCall);
            SetUpFinishButton(finishCall);
        }

        /// <summary>
        /// Hides the y rotation menu if it has already been instantiated.
        /// </summary>
        internal void Hide()
        {
            if (yRotationMenu != null)
            {
                yRotationMenu.SetActive(false);
            }
        }

        /// <summary>
        /// Destroys the y rotation menu if it has been instantiated.
        /// </summary>
        internal void Destroy()
        {
            if (yRotationMenu != null)
            {
                Destroyer.Destroy(yRotationMenu);
            }
        }

        /// <summary>
        /// Returns whether the y rotation menu currently exists and is visible.
        /// </summary>
        /// <returns>
        /// True if the y rotation menu is active; otherwise, false.
        /// </returns>
        internal bool IsActive()
        {
            return yRotationMenu != null
                   && yRotationMenu.activeInHierarchy;
        }

        /// <summary>
        /// Assigns a value to the rotation slider of the y rotation menu.
        /// </summary>
        /// <param name="degree">
        /// The rotation value in degrees.
        /// </param>
        internal void AssignValueToSlider(float degree)
        {
            if (yRotationMenu != null)
            {
                yRotationMenu
                    .GetComponentInChildren<RotationSliderController>()
                    .AssignValue(degree);
            }
        }

        /// <summary>
        /// Sets up one of the predefined y rotation buttons.
        /// </summary>
        /// <param name="buttonName">
        /// The name of the button in the menu hierarchy.
        /// </param>
        /// <param name="degree">
        /// The y rotation assigned by the button.
        /// </param>
        /// <param name="stickyNoteHolder">
        /// The sticky note holder that should be rotated.
        /// </param>
        /// <param name="slider">
        /// The y rotation slider.
        /// </param>
        /// <param name="spawnMode">
        /// Whether the sticky note is currently being spawned.
        /// </param>
        /// <param name="surface">
        /// The drawable surface containing the sticky note.
        /// </param>
        /// <param name="surfaceParentName">
        /// The name of the parent of the drawable surface.
        /// </param>
        private void SetUpDegreeButton(
            string buttonName,
            float degree,
            GameObject stickyNoteHolder,
            RotationSliderController slider,
            bool spawnMode,
            GameObject surface,
            string surfaceParentName)
        {
            GameFinder.FindAttachedOrLocalDescendant(
                    yRotationMenu,
                    buttonName)
                .GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
                {
                    slider.AssignValue(degree);

                    GameStickyNoteTransform.SetRotateY(
                        stickyNoteHolder,
                        degree);

                    if (!spawnMode)
                    {
                        new StickyNoteRoateYNetAction(
                            surface.name,
                            surfaceParentName,
                            degree,
                            stickyNoteHolder.transform.position)
                            .Execute();
                    }
                });
        }

        /// <summary>
        /// Sets up the y rotation slider.
        /// </summary>
        /// <param name="slider">
        /// The rotation slider that should be configured.
        /// </param>
        /// <param name="stickyNote">
        /// The sticky note that should be rotated.
        /// </param>
        /// <param name="spawnMode">
        /// Whether the sticky note is currently being spawned.
        /// </param>
        private void SetUpSlider(
            RotationSliderController slider,
            GameObject stickyNote,
            bool spawnMode)
        {
            GameObject surface =
                GameFinder.GetDrawableSurface(stickyNote);

            string surfaceParentName =
                GameFinder.GetDrawableSurfaceParentName(surface);

            Transform transform = stickyNote.transform;

            slider.AssignValue(transform.localEulerAngles.y);

            slider.OnValueChanged.AddListener(degree =>
            {
                GameStickyNoteTransform.SetRotateY(
                    stickyNote,
                    degree);

                if (!spawnMode)
                {
                    new StickyNoteRoateYNetAction(
                        surface.name,
                        surfaceParentName,
                        degree,
                        stickyNote.transform.position)
                        .Execute();
                }
            });
        }

        /// <summary>
        /// Sets up the button returning to the x rotation menu.
        /// </summary>
        /// <param name="backCall">
        /// The action invoked when the back button is pressed.
        /// </param>
        private void SetUpBackButton(UnityAction backCall)
        {
            GameFinder.FindAttachedOrLocalDescendant(
                    yRotationMenu,
                    "Back")
                .GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(backCall);
        }

        /// <summary>
        /// Sets up the button finishing the rotation configuration.
        /// </summary>
        /// <param name="finishCall">
        /// The action invoked when the finish button is pressed.
        /// </param>
        private void SetUpFinishButton(UnityAction finishCall)
        {
            GameFinder.FindAttachedOrLocalDescendant(
                    yRotationMenu,
                    "Finish")
                .GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(finishCall);
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
                    yRotationMenu,
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
