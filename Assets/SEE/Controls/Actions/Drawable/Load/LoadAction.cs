using HighlightPlus;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.GO;
using SEE.UI;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.UI.Notification;
using SEE.Utils;
using SEE.Utils.History;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Controls.Actions.Drawable.Load
{
    /// <summary>
    /// Adds the <see cref="DrawableType"/> to the scene from one or more drawable configs saved
    /// in a file on the disk.
    /// </summary>
    public class LoadAction : DrawableAction
    {
        /// <summary>
        /// Represents how the file was loaded.
        /// </summary>
        public enum LoadState
        {
            /// <summary>
            /// Loaded the drawable(s) from file one-to-one into the same drawable.
            /// </summary>
            Regular,

            /// <summary>
            /// Loaded the drawable(s) from the given file to one specific drawable.
            /// </summary>
            Specific
        }

        /// <summary>
        /// Represents how the page indices of loaded drawable types should be handled.
        /// </summary>
        public enum LoadPageMode
        {
            /// <summary>
            /// Keeps the page indices stored in the file.
            /// </summary>
            KeepStoredPages,

            /// <summary>
            /// Loads all drawable types onto the currently selected page of the target surface.
            /// </summary>
            CurrentSelectedPage
        }

        /// <summary>
        /// Handles execution, undo, and redo of the actual loading operation.
        /// </summary>
        private readonly LoadOperation loadOperation = new();

        /// <summary>
        /// Stores all information required to undo or redo the current load operation.
        /// </summary>
        private LoadOperation.State memento;

        /// <summary>
        /// Ensures that we save only once per click.
        /// </summary>
        private bool clicked;

        /// <summary>
        /// The selected drawable surface for specific loading.
        /// </summary>
        private GameObject selectedSurface;

        /// <summary>
        /// The instance for the drawable file browser.
        /// </summary>
        private DrawableFileBrowser browser;

        /// <summary>
        /// Creates the load menu and adds the necessary handlers for the buttons.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            /// The load button for loading onto the original drawable.
            UnityAction loadButtonCall = () =>
            {
                if (browser == null || !browser.IsOpen())
                {
                    browser = UICanvas.Canvas.AddOrGetComponent<DrawableFileBrowser>();
                    browser.LoadDrawableConfiguration(LoadState.Regular);
                    memento = new LoadOperation.State(LoadState.Regular);
                }
            };

            /// The load button for loading onto a specific drawable.
            UnityAction loadSpecificButtonCall = () =>
            {
                if (browser == null || !browser.IsOpen())
                {
                    if (selectedSurface != null)
                    {
                        browser = UICanvas.Canvas.AddOrGetComponent<DrawableFileBrowser>();
                        browser.LoadDrawableConfiguration(LoadState.Specific);
                        memento = new LoadOperation.State(LoadState.Specific);
                    }
                    else
                    {
                        ShowNotification.Warn(
                            "No drawable selected.",
                            "Select a drawable to load specifically.");
                    }
                }
            };

            /// The load button for loading onto the currently selected page of a specific drawable.
            UnityAction loadSpecificCurrentPageButtonCall = () =>
            {
                if (browser == null || !browser.IsOpen())
                {
                    if (selectedSurface != null)
                    {
                        browser = UICanvas.Canvas.AddOrGetComponent<DrawableFileBrowser>();
                        browser.LoadDrawableConfiguration(
                            LoadState.Specific,
                            LoadPageMode.CurrentSelectedPage);

                        memento = new LoadOperation.State(
                            LoadState.Specific,
                            LoadPageMode.CurrentSelectedPage);
                    }
                    else
                    {
                        ShowNotification.Warn(
                            "No drawable selected.",
                            "Select a drawable to load onto its current page.");
                    }
                }
            };

            LoadMenu.Instance.Enable(
                loadButtonCall,
                loadSpecificButtonCall,
                loadSpecificCurrentPageButtonCall);
        }

        /// <summary>
        /// Stops the <see cref="LoadAction"/>, closes an open file browser,
        /// destroys the load menu, and removes the current surface highlight.
        /// </summary>
        public override void Stop()
        {
            base.Stop();

            browser?.Close();
            browser = null;

            LoadMenu.Instance.Destroy();
            selectedSurface?.Destroy<HighlightEffect>();
        }

        /// <summary>
        /// Manages the player's interaction with the mode <see cref="ActionStateType.Load"/>.
        /// It provides the user with the available loading options and manages the target surface.
        /// </summary>
        /// <returns>Whether this action is finished.</returns>
        public override bool Update()
        {
            Cancel();
            bool result = false;

            if (!Raycasting.IsMouseOverGUI())
            {
                if (Selector.SelectQueryHasOrIsDrawableSurface(out RaycastHit raycastHit)
                    && !clicked
                    && (browser == null || !browser.IsOpen()))
                {
                    clicked = true;
                    ManageHighlightEffect(
                        GameFinder.GetDrawableSurface(raycastHit.collider.gameObject));
                }

                if (SEEInput.MouseUp(MouseButton.Left))
                {
                    clicked = false;
                }

                if (browser != null
                    && browser.TryGetFilePath(out string filePath)
                    && memento != null
                    && loadOperation.Execute(memento, selectedSurface, filePath))
                {
                    CurrentState = IReversibleAction.Progress.Completed;
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Deactivates the selected drawable.
        /// </summary>
        private void Cancel()
        {
            if (SEEInput.Cancel()
                && selectedSurface != null
                && selectedSurface.GetComponent<HighlightEffect>() != null
                && (browser == null || !browser.IsOpen()))
            {
                ShowNotification.Info(
                    "Unselect drawable",
                    "The marked drawable was unselected.");

                selectedSurface.Destroy<HighlightEffect>();
                selectedSurface = null;
            }
        }

        /// <summary>
        /// Manages the highlight effect for drawable surfaces.
        /// Only one surface can be highlighted at a time.
        /// Selecting an already highlighted surface deselects it.
        /// </summary>
        /// <param name="surface">The drawable surface whose highlight should be toggled.</param>
        private void ManageHighlightEffect(GameObject surface)
        {
            if (surface.GetComponent<HighlightEffect>() == null)
            {
                selectedSurface?.Destroy<HighlightEffect>();
                selectedSurface = surface;
                Highlighter.EnableGlowOverlay(selectedSurface);
            }
            else
            {
                Destroyer.Destroy(surface.GetComponent<HighlightEffect>());
                selectedSurface = null;
            }
        }

        /// <summary>
        /// Reverts this instance of the action.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            loadOperation.Undo(memento);
        }

        /// <summary>
        /// Repeats this instance of the action.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            loadOperation.Redo(memento);
        }

        /// <summary>
        /// A new instance of <see cref="LoadAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>New instance of <see cref="LoadAction"/>.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new LoadAction();
        }

        /// <summary>
        /// A new instance of <see cref="LoadAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>New instance of <see cref="LoadAction"/>.</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Load"/>.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Load;
        }

        /// <summary>
        /// Returns the IDs of the drawable surfaces and drawable objects affected by this load action.
        /// </summary>
        /// <returns>
        /// The IDs of the affected drawable surfaces and drawable objects,
        /// or an empty set if no load operation has been prepared.
        /// </returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento == null)
            {
                return new();
            }

            HashSet<string> changedObjects = new();

            if (memento.Mode == LoadState.Regular)
            {
                foreach (DrawableConfig config in memento.Configs.Drawables)
                {
                    changedObjects.Add(config.ID);

                    foreach (DrawableType type in config.GetAllDrawableTypes())
                    {
                        changedObjects.Add(type.ID);
                    }
                }
            }
            else
            {
                changedObjects.Add(memento.SpecificSurface.ID);

                foreach (DrawableConfig config in memento.Configs.Drawables)
                {
                    foreach (DrawableType type in config.GetAllDrawableTypes())
                    {
                        changedObjects.Add(type.ID);
                    }
                }
            }

            return changedObjects;
        }
    }
}
