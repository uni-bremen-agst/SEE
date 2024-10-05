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
using SEE.UI.Menu.Drawable;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This class provides an action to clear a whole drawable.
    /// </summary>
    public class ClearAction : DrawableAction
    {
        /// <summary>
        /// Saves all the information needed to revert or repeat
        /// this action on one drawable type.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// The menu for this action.
        /// </summary>
        private ClearMenu menu;

        /// <summary>
        /// This class can store all the information needed to
        /// revert or repeat a <see cref="ClearAction"/>.
        /// </summary>
        private readonly struct Memento
        {
            /// <summary>
            /// The drawable surface on which the drawable type is displayed.
            /// </summary>
            public readonly DrawableConfig Surface;

            /// <summary>
            /// The executed clear type.
            /// </summary>
            public readonly ClearMenu.Type ClearType;

            /// <summary>
            /// Whether the pages are also deleted.
            /// </summary>
            public readonly bool DeletePage;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="surface">The drawable surface on which the drawable type is displayed</param>
            /// <param name="clearType">The type of the clearing action.</param>
            /// <param name="deletePage">Whether the pages should also be deleted.</param>
            public Memento(GameObject surface, ClearMenu.Type clearType, bool deletePage)
            {
                Surface = DrawableConfigManager.GetDrawableConfig(surface);
                ClearType = clearType;
                DeletePage = deletePage;
            }
        }

        /// <summary>
        /// Creates the menu.
        /// </summary>
        public override void Awake()
        {
            menu = new();
        }

        /// <summary>
        /// Destroys the menu.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            menu.Destroy();
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

                    if (menu.CurrentType == ClearMenu.Type.Current)
                    {
                        return DeleteCurrentDrawableChilds(GameFinder.GetDrawableSurface(hitObject));
                    } else
                    {
                        return DeleteAllDrawableChilds(GameFinder.GetDrawableSurface(hitObject));
                    }
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// Deletes all objects of the current selected page.
        /// And depending on the configuration of the menu the page will deleted.
        /// </summary>
        /// <param name="surface">The surface which current page should be cleared.</param>
        /// <returns>true if the page was successfully cleared, false if the page was already cleared.</returns>
        private bool DeleteCurrentDrawableChilds(GameObject surface)
        {
            DrawableHolder holder = surface.GetComponent<DrawableHolder>();
            if (GameFinder.GetAttachedObjectsObject(surface) != null
                && GameFinder.HaveChildren(GameFinder.GetAttachedObjectsObject(surface), false)
                || (holder.MaxPageSize > 1 && menu.ShouldDeletePage))
            {
                memento = new Memento(surface, menu.CurrentType, menu.ShouldDeletePage);
                if (!menu.ShouldDeletePage)
                {
                    ClearCurrent(surface, holder.CurrentPage); 
                }
                else
                {
                    DeleteCurrent(surface, holder.CurrentPage);
                }
                CurrentState = IReversibleAction.Progress.Completed;
                return true;
            }
            else
            {
                ShowNotification.Info("Page is empty.", "There are no objects to clear.");
                return false;
            }
        }

        /// <summary>
        /// Clears the current surface page.
        /// </summary>
        /// <param name="surface">The surface which current page should be cleared.</param>
        /// <param name="page">The page to be cleared.</param>
        private void ClearCurrent(GameObject surface, int page)
        {
            GameDrawableManager.DeleteTypesFromPage(surface, page);
            new SurfaceClearPageNetAction(DrawableConfigManager.GetDrawableConfig(surface), page).Execute();
        }

        /// <summary>
        /// Deletes the current surface page.
        /// </summary>
        /// <param name="surface">The surface which current page should be deleted.</param>
        /// <param name="page">The page to be deleted.</param>
        private void DeleteCurrent(GameObject surface, int page)
        {
            
            GameDrawableManager.RemovePage(surface, page);
            new SurfaceRemovePageNetAction(DrawableConfigManager.GetDrawableConfig(surface), page).Execute();
        }

        /// <summary>
        /// This method finds and deletes all drawable types that are placed on the given drawable.
        /// </summary>
        /// <param name="surface">is the drawable surface which should be cleaned.</param>
        /// <returns>true if the drawable was successfully cleaned, false if the drawable was already cleaned.</returns>
        private bool DeleteAllDrawableChilds(GameObject surface)
        {
            DrawableHolder holder = surface.GetComponent<DrawableHolder>();
            if (GameFinder.GetAttachedObjectsObject(surface) != null
                && GameFinder.HaveChildren(GameFinder.GetAttachedObjectsObject(surface), false)
                || (holder.MaxPageSize > 1 && menu.ShouldDeletePage))
            {
                memento = new Memento(surface, menu.CurrentType, menu.ShouldDeletePage);
                if (!menu.ShouldDeletePage)
                {
                    ClearAll(surface);
                } else
                {
                    DeleteAll(surface);
                }
                CurrentState = IReversibleAction.Progress.Completed;
                return true;
            } else
            {
                ShowNotification.Info("Drawable is empty.", "There are no objects to clear.");
                return false;
            }
        }

        /// <summary>
        /// Clears the drawable without deleting the pages.
        /// </summary>
        /// <param name="surface">The drawable to be cleared.</param>
        private void ClearAll(GameObject surface)
        {
            DrawableHolder holder = surface.GetComponent<DrawableHolder>();
            holder.OrderInLayer = 1;
            List<DrawableType> allDrawableTypes = DrawableConfigManager.GetDrawableConfig(surface).
                        GetAllDrawableTypes();

            foreach (DrawableType type in allDrawableTypes)
            {
                GameObject child = GameFinder.FindChild(surface, type.Id);
                new EraseNetAction(surface.name, GameFinder.GetDrawableSurfaceParentName(surface),
                    child.name).Execute();
                Destroyer.Destroy(child);
            }
        }

        /// <summary>
        /// Clears the drawable with deleting all pages. 
        /// </summary>
        /// <param name="surface">The drawable to be cleared.</param>
        private void DeleteAll(GameObject surface)
        {
            DrawableHolder holder = surface.GetComponent<DrawableHolder>();
            holder.OrderInLayer = 1;
            for (int i = holder.MaxPageSize - 1; i >= 0; i--)
            {
                GameDrawableManager.RemovePage(surface, i);
                new SurfaceRemovePageNetAction(DrawableConfigManager.GetDrawableConfig(surface), i).Execute();
            }
        }

        /// <summary>
        /// Reverts this action, i.e., restores the deleted drawable elements of the selected drawable.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            GameObject surface = memento.Surface.GetDrawableSurface();
            foreach (DrawableType type in memento.Surface.GetAllDrawableTypes())
            {
                DrawableType.Restore(type, surface);
            }
            GameDrawableManager.ChangeCurrentPage(surface, memento.Surface.CurrentPage, true);
            GameDrawableManager.ChangeMaxPage(surface, memento.Surface.MaxPageSize);
            new SynchronizeSurface(DrawableConfigManager.GetDrawableConfig(surface), true).Execute();
        }

        /// <summary>
        /// Repeats this action, i.e., deletes again the objects.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            GameObject surface = memento.Surface.GetDrawableSurface();
            if (memento.ClearType == ClearMenu.Type.Current)
            {
                if (!memento.DeletePage)
                {
                    ClearCurrent(surface, memento.Surface.CurrentPage);
                } else
                {
                    DeleteCurrent(surface, memento.Surface.CurrentPage);
                }
            } else
            {
                if (!memento.DeletePage)
                {
                    ClearAll(surface);
                } else
                {
                    DeleteAll(surface);
                }
            }
        }

        /// <summary>
        /// A new instance of <see cref="ClearAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="ClearAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new ClearAction();
        }

        /// <summary>
        /// A new instance of <see cref="ClearAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="ClearAction"/></returns>
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
            return ActionStateTypes.Clear;
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
