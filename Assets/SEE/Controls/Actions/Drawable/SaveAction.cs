using HighlightPlus;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Notification;
using SEE.GO;
using SEE.UI.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SEE.UI.Menu.Drawable;
using SEE.Utils.Paths;
using SEE.Utils.History;
using Assets.SEE.Game.Drawable.ActionHelpers;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Saves one or more drawable configurations to a file.
    /// </summary>
    public class SaveAction : DrawableAction
    {
        /// <summary>
        /// Represents if one, multiple, or all drawables have been saved in the file.
        /// </summary>
        public enum SaveState
        {
            One,
            Multiple,
            All
        }

        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="SaveAction"/>.
        /// </summary>
        private class Memento
        {
            /// <summary>
            /// The path of the file in which the drawable's config has been written.
            /// </summary>
            internal FilePath FilePath;

            /// <summary>
            /// The drawables that should be saved.
            /// </summary>
            internal readonly DrawableConfig[] Drawables;

            /// <summary>
            /// The state if one or more drawables has been saved in this file.
            /// </summary>
            internal readonly SaveState SavedState;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="drawables">The drawables to save into this file</param>
            /// <param name="savedState">Represents if one or more drawables saved in this file.</param>
            internal Memento(DrawableConfig[] drawables, SaveState savedState)
            {
                Drawables = drawables;
                SavedState = savedState;
            }
        }

        /// <summary>
        /// Ensures that per click is only saved once.
        /// </summary>
        private bool clicked = false;

        /// <summary>
        /// List of all selected drawable for saving.
        /// </summary>
        private readonly List<GameObject> selectedDrawables = new();

        /// <summary>
        /// The instance for the drawable file browser.
        /// </summary>
        private DrawableFileBrowser browser;

        /// <summary>
        /// Stops the <see cref="SaveAction"/>.
        /// Destroys the save menu and if there are still highlight effect.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            foreach (GameObject drawable in selectedDrawables)
            {
                drawable.Destroy<HighlightEffect>();
            }
            SaveMenu.Disable();
        }

        /// <summary>
        /// Enables the save menu and adds the required actions.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            /// The button for save the selected drawables.
            UnityAction saveButtonCall = () =>
            {
                if (browser == null || (browser != null && !browser.IsOpen()))
                {
                    if (selectedDrawables.Count > 0)
                    {
                        browser = Surface.AddOrGetComponent<DrawableFileBrowser>();
                        if (selectedDrawables.Count == 1)
                        {
                            browser.SaveDrawableConfiguration(SaveState.One);
                            memento = new Memento(new DrawableConfig[]
                            {
                                DrawableConfigManager.GetDrawableConfig(selectedDrawables[0])
                            }, SaveState.One);
                        }
                        else
                        {
                            browser.SaveDrawableConfiguration(SaveState.Multiple);
                            DrawableConfig[] configs = new DrawableConfig[selectedDrawables.Count];
                            for (int i = 0; i < configs.Length; i++)
                            {
                                configs[i] = DrawableConfigManager.GetDrawableConfig(selectedDrawables[i]);
                            }
                            memento = new Memento(configs, SaveState.Multiple);
                        }
                    } else
                    {
                        ShowNotification.Warn("No drawable selected.", "Select one or more drawables to save.");
                    }
                }
            };

            /// The button for save all drawables in the world.
            UnityAction saveAllButtonCall = () =>
            {
                if (browser == null || (browser != null && !browser.IsOpen()))
                {
                    browser = Surface.AddOrGetComponent<DrawableFileBrowser>();
                    browser.SaveDrawableConfiguration(SaveState.All);
                    List<GameObject> drawables = new(GameObject.FindGameObjectsWithTag(Tags.Drawable));
                    DrawableConfig[] configs = new DrawableConfig[drawables.Count];
                    for (int i = 0; i < configs.Length; i++)
                    {
                        configs[i] = DrawableConfigManager.GetDrawableConfig(drawables[i]);
                    }
                    memento = new Memento(configs, SaveState.All);
                }
            };

            SaveMenu.Enable(saveButtonCall, saveAllButtonCall);
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.Save"/>.
        /// It saves one, more or all drawables of the scene.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        public override bool Update()
        {
            Cancel();
            bool result = false;

            if (!Raycasting.IsMouseOverGUI())
            {
                /// Provides the selecting and deselecting of drawables for saving.
                DrawableSelection();

                /// Needed for selecting multiple drawables to save.
                if (Queries.MouseUp(MouseButton.Left))
                {
                    clicked = false;
                }

                /// If a file to save was successfully chosen, this block will be executed.
                /// It saves the selected drawable/drawables in the chosen file.
                if (browser != null && browser.TryGetFilePath(out string filePath) && memento != null)
                {
                    Save(filePath, ref result);
                    browser = null;
                }
            }
            return result;
        }

        /// <summary>
        /// Deactivates the highlighting of the selected drawables and clears the selected
        /// drawable list.
        /// </summary>
        private void Cancel()
        {
            if (selectedDrawables.Count > 0 && Input.GetKeyDown(KeyCode.Escape)
                && (browser == null || (browser != null && !browser.IsOpen())))
            {
                ShowNotification.Info("Unselect drawables", "The marked drawables was unselected.");
                foreach (GameObject drawable in selectedDrawables)
                {
                    drawable.Destroy<HighlightEffect>();
                }
                selectedDrawables.Clear();
            }
        }

        /// <summary>
        /// This block marks the selected drawable and adds it to a list.
        /// If it has already been selected, it is removed from the list, and the marking is cleared.
        /// For execution, no open file browser should exist.
        /// </summary>
        private void DrawableSelection()
        {
            if (Selector.SelectQueryHasOrIsDrawable(out RaycastHit raycastHit)
                && !clicked
                && (browser == null || (browser != null && !browser.IsOpen())))
            {
                clicked = true;
                GameObject drawable = GameFinder.GetDrawable(raycastHit.collider.gameObject);

                if (drawable.GetComponent<HighlightEffect>() == null)
                {
                    selectedDrawables.Add(drawable);
                    GameHighlighter.EnableGlowOverlay(drawable);
                }
                else
                {
                    Destroyer.Destroy(drawable.GetComponent<HighlightEffect>());
                    selectedDrawables.Remove(drawable);
                }
            }
        }

        /// <summary>
        /// Saves the drawable configurations to a file.
        /// </summary>
        /// <param name="filePath">The chosen file path, where the saved file should be placed.</param>
        /// <param name="result">The action state result.</param>
        private void Save(string filePath, ref bool result)
        {
            memento.FilePath = new FilePath(filePath);
            GameObject[] drawables = new GameObject[memento.Drawables.Length];
            for (int i = 0; i < drawables.Length; i++)
            {
                drawables[i] = memento.Drawables[i].GetDrawable();
            }
            DrawableConfigManager.SaveDrawables(drawables, memento.FilePath);
            result = true;
            CurrentState = IReversibleAction.Progress.Completed;

            if (memento.SavedState == SaveState.One)
            {
                ShowNotification.Info("Saved!",
                            $"The selected drawable has been successfully saved to the file {filePath}.");
            }
            else if (memento.SavedState == SaveState.Multiple)
            {
                ShowNotification.Info("Saved!",
                        "The chosen " + drawables.Length + $" drawables have been successfully saved to the file {filePath}");
            } else
            {
                ShowNotification.Info("Saved!",
                        $"All drawables have been successfully saved to the file {filePath}");
            }
        }

        /// <summary>
        /// Reverts this action, i.e., deletes the file in which the drawables configuration
        /// was saved.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            DrawableConfigManager.DeleteDrawables(memento.FilePath);
        }

        /// <summary>
        /// Repeats this action, i.e., saves this drawables again with the same filename that
        /// was given by the player
        /// initially.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            GameObject[] drawables = new GameObject[memento.Drawables.Length];
            for (int i = 0; i < drawables.Length; i++)
            {
                drawables[i] = memento.Drawables[i].GetDrawable();
            }
            DrawableConfigManager.SaveDrawables(drawables, memento.FilePath);
        }

        /// <summary>
        /// A new instance of <see cref="SaveAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="SaveAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new SaveAction();
        }

        /// <summary>
        /// A new instance of <see cref="SaveAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="SaveAction"/></returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Save"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Save;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// </summary>
        /// <returns>the filepath of the save file</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new();
        }
    }
}
