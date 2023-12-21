using HighlightPlus;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.UI.Drawable;
using SEE.Game.UI.Menu.Drawable;
using SEE.Game.UI.Notification;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Saves one or more drawable configuration's to a file.
    /// </summary>
    public class SaveAction : AbstractPlayerAction
    {
        /// <summary>
        /// Represents if one or more drawables has been saved in the file.
        /// </summary>
        public enum SaveState
        {
            One,
            More,
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
            internal FilePath filePath;

            /// <summary>
            /// The drawables that should be saved.
            /// </summary>
            internal readonly DrawableConfig[] drawables;

            /// <summary>
            /// The state if one or more drawables has been saved in this file.
            /// </summary>
            internal readonly SaveState savedState;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="drawables">The drawables to save into this file</param>
            /// <param name="savedState">Represents if one or more drawables saved in this file.</param>
            internal Memento(DrawableConfig[] drawables, SaveState savedState)
            {
                this.drawables = drawables;
                this.savedState = savedState;
            }
        }

        /// <summary>
        /// Ensures that per click is only saved once.
        /// </summary>
        private bool clicked = false;

        /// <summary>
        /// List of all selected drawable for saving.
        /// </summary>
        private List<GameObject> selectedDrawables = new();

        /// <summary>
        /// The instance for the drawable file browser
        /// </summary>
        private DrawableFileBrowser browser;

        /// <summary>
        /// Stops the <see cref="SaveAction"/>.
        /// Destroys the save menu and if there are still highlight effect
        /// </summary>
        public override void Stop()
        {
            foreach (GameObject drawable in selectedDrawables)
            {
                if (drawable.GetComponent<HighlightEffect>() != null)
                {
                    Destroyer.Destroy(drawable.GetComponent<HighlightEffect>());
                }
            }
            SaveMenu.Disable();
        }

        /// <summary>
        /// Enables the save menu and adds the required Actions.
        /// </summary>
        public override void Awake()
        {
            /// The button for save the selected drawables.
            UnityAction saveButtonCall = () =>
            {
                if (browser == null || (browser != null && !browser.IsOpen()))
                {
                    if (selectedDrawables.Count > 0)
                    {
                        browser = GameObject.Find("UI Canvas").AddOrGetComponent<DrawableFileBrowser>();
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
                            browser.SaveDrawableConfiguration(SaveState.More);
                            DrawableConfig[] configs = new DrawableConfig[selectedDrawables.Count];
                            for (int i = 0; i < configs.Length; i++)
                            {
                                configs[i] = DrawableConfigManager.GetDrawableConfig(selectedDrawables[i]);
                            }
                            memento = new Memento(configs, SaveState.More);
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
                    browser = GameObject.Find("UI Canvas").AddOrGetComponent<DrawableFileBrowser>();
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
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            bool result = false;

            if (!Raycasting.IsMouseOverGUI())
            {
                /// Provides the selecting and deselecting of drawables for saving.
                DrawableSelection();

                /// Needed for select more drawables to save.
                if (Input.GetMouseButtonUp(0))
                {
                    clicked = false;
                }

                /// If a file to save was successfully chosen this block will be executed.
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
        /// This block marks the selected drawable and adds it to a list. 
        /// If it has already been selected, it is removed from the list, and the marking is cleared.
        /// For execution, no open file browser should exist.
        /// </summary>
        private void DrawableSelection()
        {
            if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
                && !clicked && Raycasting.RaycastAnything(out RaycastHit hit)
                && (GameFinder.hasDrawable(hit.collider.gameObject)
                    || hit.collider.gameObject.CompareTag(Tags.Drawable))
                && (browser == null || (browser != null && !browser.IsOpen())))
            {
                clicked = true;
                GameObject drawable = hit.collider.gameObject.CompareTag(Tags.Drawable) ?
                    hit.collider.gameObject : GameFinder.GetDrawable(hit.collider.gameObject);

                if (drawable.GetComponent<HighlightEffect>() == null)
                {
                    selectedDrawables.Add(drawable);
                    HighlightEffect effect = drawable.AddComponent<HighlightEffect>();
                    effect.highlighted = true;
                    effect.previewInEditor = false;
                    effect.outline = 0;
                    effect.glowQuality = HighlightPlus.QualityLevel.Highest;
                    effect.glow = 1.0f;
                    effect.glowHQColor = Color.yellow;
                    effect.overlay = 1.0f;
                    effect.overlayColor = Color.magenta;
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
        /// <param name="filePath">The chosen file path, where the save file should be placed.</param>
        /// <param name="result">The action state result.</param>
        private void Save(string filePath, ref bool result)
        {
            switch (memento.savedState)
            {
                case SaveState.One:
                    memento.filePath = new FilePath(filePath);
                    DrawableConfigManager.SaveDrawable(memento.drawables[0].GetDrawable(), memento.filePath);
                    ShowNotification.Info("Saved!",
                            "The selected drawable has been successfully saved to the file " + filePath);
                    currentState = ReversibleAction.Progress.Completed;
                    result = true;
                    break;
                case SaveState.More:
                case SaveState.All:
                    memento.filePath = new FilePath(filePath);
                    GameObject[] drawables = new GameObject[memento.drawables.Length];
                    for (int i = 0; i < drawables.Length; i++)
                    {
                        drawables[i] = memento.drawables[i].GetDrawable();
                    }
                    DrawableConfigManager.SaveDrawables(drawables, memento.filePath);
                    ShowNotification.Info("Saved!",
                            "The chosen drawables has been successfully saved to the file " + filePath);
                    currentState = ReversibleAction.Progress.Completed;
                    result = true;
                    break;
            }
        }

        /// <summary>
        /// Reverts this action, i.e., deletes the file in which the drawables configuration was saved.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            DrawableConfigManager.DeleteDrawables(memento.filePath);
        }

        /// <summary>
        /// Repeats this action, i.e., saves this drawables again with the same filename that was given by the player
        /// initially.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (memento.savedState == SaveState.One)
            {
                DrawableConfigManager.SaveDrawable(memento.drawables[0].GetDrawable(), memento.filePath);
            }
            else
            {
                GameObject[] drawables = new GameObject[memento.drawables.Length];
                for (int i = 0; i < drawables.Length; i++)
                {
                    drawables[i] = memento.drawables[i].GetDrawable();
                }
                DrawableConfigManager.SaveDrawables(drawables, memento.filePath);
            }
        }

        /// <summary>
        /// A new instance of <see cref="SaveAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="SaveAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new SaveAction();
        }

        /// <summary>
        /// A new instance of <see cref="SaveAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="SaveAction"/></returns>
        public override ReversibleAction NewInstance()
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
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>the filepath of the save file</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> { memento.filePath.ToString() };
        }
    }
}
