using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Notification;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using SEE.Utils.History;
using SEE.Game.Drawable.ValueHolders;
using SEE.Game.Drawable.ActionHelpers;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This class provides an action to clean a whole drawable.
    /// </summary>
    public class CleanerAction : DrawableAction
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
        private readonly struct Memento
        {
            /// <summary>
            /// The drawable surface on which the drawable type is displayed.
            /// </summary>
            public readonly DrawableConfig Surface;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="surface">The drawable surface on which the drawable type is displayed</param>
            public Memento(GameObject surface)
            {
                Surface = DrawableConfigManager.GetDrawableConfig(surface);
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the action <see cref="ActionStateType.Cleaner"/>.
        /// It cleans a complete drawable. In other words: it deletes all drawable types that are on a selected drawable.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                if (Queries.LeftMouseDown() 
                    && Raycasting.RaycastAnything(out RaycastHit raycastHit))
                {
                    GameObject hitObject = raycastHit.collider.gameObject;

                    if (hitObject.CompareTag(Tags.Drawable))
                    {
                        return DeleteDrawableChilds(hitObject);
                    } else if (GameFinder.HasDrawableSurface(hitObject))
                    {
                        return DeleteDrawableChilds(GameFinder.GetDrawableSurface(hitObject));
                    }
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// This method finds and deletes all drawable types that are placed on the given drawable.
        /// </summary>
        /// <param name="surface">is the drawable surface which should be cleaned.</param>
        /// <returns>true if the drawable was successfully cleaned, false if the drawable was already cleaned.</returns>
        private bool DeleteDrawableChilds(GameObject surface)
        {
            if (GameFinder.GetAttachedObjectsObject(surface) != null)
            {
                DrawableHolder holder = surface.GetComponent<DrawableHolder>();
                holder.OrderInLayer = 1;

                List<DrawableType> allDrawableTypes = DrawableConfigManager.GetDrawableConfig(surface).
                    GetAllDrawableTypes();
                if (allDrawableTypes.Count == 0)
                {
                    return false;
                }

                foreach (DrawableType type in allDrawableTypes)
                {
                    GameObject child = GameFinder.FindChild(surface, type.Id);
                    new EraseNetAction(surface.name, GameFinder.GetDrawableSurfaceParentName(surface),
                        child.name).Execute();
                    Destroyer.Destroy(child);
                }

                memento = new Memento(surface);
                CurrentState = IReversibleAction.Progress.Completed;
                return true;
            } else
            {
                ShowNotification.Info("Drawable is empty.", "There are no objects to clear.");
                return false;
            }
        }

        /// <summary>
        /// Reverts this action, i.e., restores the deleted drawable elements of the selected drawable.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            foreach(DrawableType type in memento.Surface.GetAllDrawableTypes())
            {
                GameObject surface = memento.Surface.GetDrawableSurface();
                DrawableType.Restore(type, surface);
            }
        }

        /// <summary>
        /// Repeats this action, i.e., deletes again all the drawable elements that are placed on the
        /// selected drawable.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            foreach (DrawableType type in memento.Surface.GetAllDrawableTypes())
            {
                GameObject toDelete = GameFinder.FindChild(memento.Surface.GetDrawableSurface(), type.Id); ;
                new EraseNetAction(memento.Surface.ID, memento.Surface.ParentID, type.Id).Execute();
                Destroyer.Destroy(toDelete);
            }
        }

        /// <summary>
        /// A new instance of <see cref="CleanerAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="CleanerAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new CleanerAction();
        }

        /// <summary>
        /// A new instance of <see cref="CleanerAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="CleanerAction"/></returns>
        public override IReversibleAction NewInstance()
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
        /// </summary>
        /// <returns>ids of the deletes drawable types</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.Surface == null)
            {
                return new HashSet<string>();
            }
            else
            {
                HashSet<string> changedObjects = new()
                {
                    memento.Surface.ID
                };
                foreach(DrawableType type in memento.Surface.GetAllDrawableTypes())
                {
                    changedObjects.Add(type.Id);
                }
                return changedObjects;
            }
        }
    }
}
