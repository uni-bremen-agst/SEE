using Assets.SEE.Game.Drawable;
using HighlightPlus;
using Michsky.UI.ModernUIPack;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.UI.Notification;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

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
            internal readonly GameObject[] drawables;

            /// <summary>
            /// The state if one or more drawables has been saved in this file.
            /// </summary>
            internal readonly SaveState savedState;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="drawables">The drawables to save into this file</param>
            /// <param name="savedState">Represents if one or more drawables saved in this file.</param>
            internal Memento(GameObject[] drawables, SaveState savedState)
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
        /// The location where the text menu prefeb is placed.
        /// </summary>
        private const string saveMenuPrefab = "Prefabs/UI/Drawable/SaveMenu";

        /// <summary>
        /// The instance for the save menu.
        /// </summary>
        private GameObject saveMenu;

        /// <summary>
        /// The instance for the save single or more drawable button.
        /// </summary>
        private ButtonManagerBasic saveButton;

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
            Destroyer.Destroy(saveMenu);
        }

        /// <summary>
        /// Enables the save menu and adds the required Handler.
        /// </summary>
        public override void Awake()
        {
            saveMenu = PrefabInstantiator.InstantiatePrefab(saveMenuPrefab,
                GameObject.Find("UI Canvas").transform, false);
            saveButton = GameFinder.FindChild(saveMenu, "Save").GetComponent<ButtonManagerBasic>();
            saveButton.clickEvent.AddListener(() =>
            {
                if (browser == null || (browser != null && !browser.IsOpen()))
                {
                    if (selectedDrawables.Count > 0)
                    {
                        browser = GameObject.Find("UI Canvas").AddOrGetComponent<DrawableFileBrowser>();
                        if (selectedDrawables.Count == 1)
                        {
                            browser.SaveDrawableConfiguration(SaveState.One);
                            memento = new Memento(new GameObject[] { selectedDrawables[0] }, SaveState.One);
                        }
                        else 
                        {
                            browser.SaveDrawableConfiguration(SaveState.More);
                            memento = new Memento(selectedDrawables.ToArray(), SaveState.More);
                        }
                    } else
                    {
                        ShowNotification.Warn("No drawable selected.", "Select one or more drawables to save.");
                    }
                }
            });
            ButtonManagerBasic saveAllButton = GameFinder.FindChild(saveMenu, "SaveAll").GetComponent<ButtonManagerBasic>();
            saveAllButton.clickEvent.AddListener(() =>
            {
                if (browser == null || (browser != null && !browser.IsOpen()))
                {
                    browser = GameObject.Find("UI Canvas").AddOrGetComponent<DrawableFileBrowser>();
                    browser.SaveDrawableConfiguration(SaveState.All);
                    List<GameObject> drawables = new(GameObject.FindGameObjectsWithTag(Tags.Drawable));
                    memento = new Memento(drawables.ToArray(), SaveState.All);
                }
            });
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
                /// This block marks the selected drawable and adds it to a list. If it has already been selected, it is removed from the list, and the marking is cleared.
                /// For execution, no open file browser should exist.
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
                    && !clicked &&
                    Raycasting.RaycastAnythingBackface(out RaycastHit hit) &&
                    (GameFinder.hasDrawable(hit.collider.gameObject) || hit.collider.gameObject.CompareTag(Tags.Drawable))
                    && (browser == null || (browser != null && !browser.IsOpen())))
                {
                    clicked = true;
                    GameObject drawable = hit.collider.gameObject.CompareTag(Tags.Drawable) ?
                        hit.collider.gameObject : GameFinder.FindDrawable(hit.collider.gameObject);

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

                /// Needed for select more drawables to save.
                if (Input.GetMouseButtonUp(0))
                {
                    clicked = false;
                }

                /// If a file to save was successfully chosen this block will be executed.
                /// It saves the selected drawable/drawables in the chosen file.
                if (browser != null && browser.TryGetFilePath(out string filePath) && memento != null)
                {
                    switch (memento.savedState)
                    {
                        case SaveState.One:
                            memento.filePath = new FilePath(filePath);
                            DrawableConfigManager.SaveDrawable(memento.drawables[0], memento.filePath);
                            currentState = ReversibleAction.Progress.Completed;
                            result = true;
                            break;
                        case SaveState.More:
                            memento.filePath = new FilePath(filePath);
                            DrawableConfigManager.SaveDrawables(memento.drawables, memento.filePath);
                            currentState = ReversibleAction.Progress.Completed;
                            result = true;
                            break;
                        case SaveState.All:
                            memento.filePath = new FilePath(filePath);
                            DrawableConfigManager.SaveDrawables(memento.drawables, memento.filePath);
                            currentState = ReversibleAction.Progress.Completed;
                            result = true;
                            break;
                    }
                    browser = null;
                }
            }
            return result;
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
                DrawableConfigManager.SaveDrawable(memento.drawables[0], memento.filePath);
            }
            else
            {
                DrawableConfigManager.SaveDrawables(memento.drawables, memento.filePath);
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
