using SEE.Game;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Saves one or more drawable configuration's to a file.
    /// </summary>
    class SaveAction : AbstractPlayerAction
    {
        /// <summary>
        /// Represents if one or more drawables has been saved in the file.
        /// </summary>
        private enum SavedCounter
        {
            One,
            More
        }
        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="SaveAction"/>.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The name of the file in which the drawable's config has been written.
            /// </summary>
            internal readonly string fileName;

            /// <summary>
            /// The drawables configs that was saved here.
            /// </summary>
            internal readonly GameObject[] drawables;

            /// <summary>
            /// The counter if one or more drawables has been saved in this file.
            /// </summary>
            internal readonly SavedCounter savedCounter;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="fileName">The filename to save into this Memento</param>
            /// <param name="drawables">The drawables to save into this file</param>
            /// <param name="savedCounter">Represents if one or more drawables saved in this file.</param>
            internal Memento(string fileName, GameObject[] drawables, SavedCounter savedCounter)
            {
                this.fileName = fileName;
                this.drawables = drawables;
                this.savedCounter = savedCounter;
            }
        }

        /// <summary>
        /// Ensures that per click is only saved once.
        /// </summary>
        private static bool clicked = false;

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.Save"/>.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            bool result = false;

            if (!Raycasting.IsMouseOverGUI())
            {
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && !clicked &&
                    Raycasting.RaycastAnythingBackface(out RaycastHit raycastHit) &&// && // Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    (GameDrawableFinder.hasDrawable(raycastHit.collider.gameObject) || raycastHit.collider.gameObject.CompareTag(Tags.Drawable)))
                {
                    clicked = true;
                    GameObject drawable = raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ?
                        raycastHit.collider.gameObject : GameDrawableFinder.FindDrawable(raycastHit.collider.gameObject);
                    DrawableConfigManager.SaveDrawable(drawable, "Test");
                    memento = new Memento("Test", new GameObject[] { drawable }, SavedCounter.One);
                    currentState = ReversibleAction.Progress.Completed;
                    result = true;
                }
                if (Input.GetMouseButtonDown(1) || Input.GetMouseButton(1) && !clicked)
                {
                    clicked = true;
                    List<GameObject> drawables = new (GameObject.FindGameObjectsWithTag(Tags.Drawable));
                    DrawableConfigManager.SaveDrawables(drawables.ToArray(), "Test1");
                    memento = new Memento("Test1", drawables.ToArray(), SavedCounter.More);
                    currentState = ReversibleAction.Progress.Completed;
                    result = true;
                }

                if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
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
            DrawableConfigManager.DeleteDrawables(memento.fileName);
        }

        /// <summary>
        /// Repeats this action, i.e., saves this drawables again with the same filename that was given by the player
        /// initially.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (memento.savedCounter == SavedCounter.One)
            {
                DrawableConfigManager.SaveDrawable(memento.drawables[0], memento.fileName);
            } else
            {
                DrawableConfigManager.SaveDrawables(memento.drawables, memento.fileName);
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
