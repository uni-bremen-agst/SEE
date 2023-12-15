using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.UI.Notification;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This class provides an action to clean a whole drawable.
    /// </summary>
    public class CleanerAction : AbstractPlayerAction
    {
        /// <summary>
        /// Saves all the information needed to revert or repeat 
        /// this action on one drawable type.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This class can store all the information needed to 
        /// revert or repeat a <see cref="CleanerAction"/>.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The drawable on that the drawable type is displayed.
            /// </summary>
            public readonly DrawableConfig drawable;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="drawable">The drawable on that the drawable type is displayed</param>
            public Memento(GameObject drawable)
            {
                this.drawable = DrawableConfigManager.GetDrawableConfig(drawable);
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the action <see cref="ActionStateType.Cleaner"/>.
        /// It cleans a complete drawable. In other words: it's deletes all drawable types that are on a selected drawable.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                if (Input.GetMouseButtonDown(0) &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHit))
                {
                    GameObject hittedObject = raycastHit.collider.gameObject;

                    if (hittedObject.CompareTag(Tags.Drawable))
                    {
                        return DeleteDrawableChilds(hittedObject);
                    } else if (GameFinder.hasDrawable(hittedObject))
                    {
                        return DeleteDrawableChilds(GameFinder.GetDrawable(hittedObject));
                    }
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// This method finds and deletes all drawable types that are placed on the given drawable.
        /// </summary>
        /// <param name="drawable">is the drawable which should be cleaned.</param>
        /// <returns>true if the drawable was successfull cleaned, false if the drawable was already cleaned</returns>
        private bool DeleteDrawableChilds(GameObject drawable)
        {
            if (GameFinder.GetAttachedObjectsObject(drawable) != null)
            {
                List<DrawableType> allDrawableTypes = DrawableConfigManager.GetDrawableConfig(drawable).
                    GetAllDrawableTypes();
                if (allDrawableTypes.Count == 0)
                {
                    return false;
                }

                foreach (DrawableType type in allDrawableTypes)
                {
                    GameObject child = GameFinder.FindChild(drawable, type.id);
                    new EraseNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), 
                        child.name).Execute();
                    Destroyer.Destroy(child);
                }

                memento = new Memento(drawable);
                currentState = ReversibleAction.Progress.Completed;
                return true;
            } else
            {
                ShowNotification.Info("Drawable is empty.", "There are no objects to clear.");
                return false;
            }
        }

        /// <summary>
        /// Reverts this action, i.e., restores the deleted drawable types of the selected drawable.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            foreach(DrawableType type in memento.drawable.GetAllDrawableTypes())
            {
                GameObject drawable = memento.drawable.GetDrawable();
                DrawableType.Restore(type, drawable);
            }
        }

        /// <summary>
        /// Repeats this action, i.e., deletes again all the drawable types that are placed on the selected drawable.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            foreach (DrawableType type in memento.drawable.GetAllDrawableTypes())
            {
                GameObject toDelete = GameFinder.FindChild(memento.drawable.GetDrawable(), type.id); ;
                new EraseNetAction(memento.drawable.ID, memento.drawable.ParentID, type.id).Execute();
                Destroyer.Destroy(toDelete);
            }
        }

        /// <summary>
        /// A new instance of <see cref="CleanerAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="CleanerAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new CleanerAction();
        }

        /// <summary>
        /// A new instance of <see cref="CleanerAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="CleanerAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Cleaner"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Cleaner;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>id's of the deletes drawable types</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.drawable == null)
            {
                return new HashSet<string>();
            }
            else
            {
                HashSet<string> changedObjects = new ();
                changedObjects.Add(memento.drawable.ID);
                foreach(DrawableType type in memento.drawable.GetAllDrawableTypes())
                {
                    changedObjects.Add(type.id);
                }
                return changedObjects;
            }
        }
    }
}