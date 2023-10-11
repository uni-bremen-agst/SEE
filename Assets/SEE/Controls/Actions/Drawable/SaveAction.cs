using SEE.Game;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;
using OpenAI.Files;
using HighlightPlus;
using Sirenix.OdinInspector;
using Assets.SEE.Game.UI.Drawable;
using static SEE.Controls.Actions.Drawable.LoadAction;
using System.IO;

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
            /// The drawables configs that was saved here.
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
        private static bool clicked = false;

        /// <summary>
        /// The file path of the to saved file.
        /// </summary>
        public static string filePath = "";

        /// <summary>
        /// List of all selected drawable for saving.
        /// </summary>
        private static List<GameObject> selectedDrawables = new();

        private static DrawableFileBrowser browser;

        /// <summary>
        /// Stops the <see cref="SaveAction"/>.
        /// Resets the file path.
        /// </summary>
        public override void Stop()
        {
            filePath = "";
            Reset();
        }

        /// <summary>
        /// This method will called, when the Action will be leaved and some drawable are still selected.
        /// </summary>
        public static void Reset()
        {
            selectedDrawables.Clear();
            browser = null;
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.Save"/>.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            bool result = false;

            if (!Raycasting.IsMouseOverGUI())
            {
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) &&
                    Raycasting.RaycastAnythingBackface(out RaycastHit raycastHit) && selectedDrawables.Count == 0 &&
                    (GameDrawableFinder.hasDrawable(raycastHit.collider.gameObject) || raycastHit.collider.gameObject.CompareTag(Tags.Drawable))
                    && (browser == null || (browser != null && !browser.IsOpen())))
                {
                    GameObject drawable = raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ?
                        raycastHit.collider.gameObject : GameDrawableFinder.FindDrawable(raycastHit.collider.gameObject);
                    browser = GameObject.Find("UI Canvas").AddComponent<DrawableFileBrowser>();
                    browser.SaveDrawableConfiguration(SaveState.One);
                    memento = new Memento(new GameObject[] { drawable }, SaveState.One);
                    currentState = ReversibleAction.Progress.InProgress;
                }

                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && !clicked &&
                Raycasting.RaycastAnythingBackface(out RaycastHit hit) &&
                (GameDrawableFinder.hasDrawable(hit.collider.gameObject) || hit.collider.gameObject.CompareTag(Tags.Drawable))
                && (browser == null || (browser != null && !browser.IsOpen())))
                {
                    clicked = true;
                    GameObject drawable = hit.collider.gameObject.CompareTag(Tags.Drawable) ?
                        hit.collider.gameObject : GameDrawableFinder.FindDrawable(hit.collider.gameObject);

                    if (drawable.GetComponent<HighlightEffect>() == null)
                    {
                        selectedDrawables.Add(drawable);
                        HighlightEffect effect = drawable.AddComponent<HighlightEffect>();
                        drawable.AddComponent<HighlightEffectDestroyer>().SetAllowedState(GetActionStateType());
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
                        Destroyer.Destroy(drawable.GetComponent<HighlightEffectDestroyer>());
                        selectedDrawables.Remove(drawable);
                    }
                }
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && selectedDrawables.Count > 0
                    && (browser == null || (browser != null && !browser.IsOpen())))
                {
                    browser = GameObject.Find("UI Canvas").AddComponent<DrawableFileBrowser>();
                    browser.SaveDrawableConfiguration(SaveState.More);
                    memento = new Memento(selectedDrawables.ToArray(), SaveState.More);
                    currentState = ReversibleAction.Progress.InProgress;
                }

                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && !Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift)
                    && (browser == null || (browser != null && !browser.IsOpen())))
                {
                    //clicked = true;s
                    browser = GameObject.Find("UI Canvas").AddComponent<DrawableFileBrowser>();
                    browser.SaveDrawableConfiguration(SaveState.All);
                    List<GameObject> drawables = new(GameObject.FindGameObjectsWithTag(Tags.Drawable));
                    memento = new Memento(drawables.ToArray(), SaveState.All);
                    currentState = ReversibleAction.Progress.InProgress;
                }

                if (!filePath.Equals("") && memento != null)
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
                            foreach(GameObject selectedDrawable in selectedDrawables)
                            {
                                Destroyer.Destroy(selectedDrawable.GetComponent<HighlightEffect>());
                                Destroyer.Destroy(selectedDrawable.GetComponent<HighlightEffectDestroyer>());
                            }
                            selectedDrawables.Clear();
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

                if (Input.GetMouseButtonUp(0))
                {
                    clicked = false;
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
        /// <returns>an empty set</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>();
        }
    }
}
