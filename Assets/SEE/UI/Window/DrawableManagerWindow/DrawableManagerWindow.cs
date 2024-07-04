using Markdig.Helpers;
using SEE.DataModel;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.UI.Window;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.SEE.UI.Window.DrawableManagerWindow
{
    /// <summary>
    /// A window that represents a manager for managing drawable surfaces. 
    /// The window contains a scrollable list of expandable items.
    /// </summary>
    public partial class DrawableManagerWindow : BaseWindow, IObserver<ChangeEvent>
    {
        /// <summary>
        /// Path to the drawable manager window content prefab.
        /// </summary>
        private const string dmWindowPrefab = "Prefabs/UI/Drawable/Window/DrawableManagerView";

        /// <summary>
        /// Path to the drawable manager window item prefab.
        /// </summary>
        private const string dmItemPrefab = "Prefabs/UI/Drawable/Window/DrawableManagerViewItem";

        /// <summary>
        /// Path to the drawable manager window item without description prefab.
        /// </summary>
        private const string dmwdItemPrefab = "Prefabs/UI/Drawable/Window/DrawableManagerViewItemWithoutDescription";

        /// <summary>
        /// Path to the drawable manager window group item prefab.
        /// </summary>
        private const string groupItemPrefab = "Prefabs/UI/Drawable/Window/GroupViewItem";

        /// <summary>
        /// Transform of the object containing the items of the drawable manager window.
        /// </summary>
        private RectTransform items;

        /// <summary>
        /// The context menu that is displayed when the user uses the filter, group or sort buttons.
        /// </summary>
        private DrawableWindowContextMenu contextMenu;

        /// <summary>
        /// TODO: BRauchen wir?
        /// </summary>
        private IDisposable subscription;

        /// <summary>
        /// Adds the drawable surfaces of the visualization to the drawable manager view.
        /// </summary>
        /// <param name="surfaces">The surfaces to be added, if it is not set all surfaces of the scene will be added.</param>
        private void AddDrawableSurfaces(List<GameObject> surfaces = null)
        {
            /// Case group is active.
            if (contextMenu.grouper.IsActive)
            {
                AddGroup("Whiteboards", Icons.Whiteboard,
                    OrderList(contextMenu.grouper.GetWhiteboards(contextMenu.filter)),
                    WhiteboardColor);
                AddGroup("Sticky Notes", Icons.StickyNote,
                    OrderList(contextMenu.grouper.GetStickyNotes(contextMenu.filter)),
                    StickyNoteColor);
            }
            else /// Group is not active.
            {
                surfaces ??= contextMenu.filter.GetFilteredSurfaces();
                foreach (GameObject surface in Sort(surfaces))
                {
                    if (surface.CompareTag(Tags.Drawable))
                    {
                        AddItem(surface);
                    }
                }
            }
        }

        /// <summary>
        /// Sorts the list as defined or by default.
        /// </summary>
        /// <param name="surfaces">The surfaces</param>
        /// <returns>The sorted surfaces</returns>
        private List<GameObject> Sort(List<GameObject> surfaces)
        {
            return contextMenu.sorter.IsActive() ? contextMenu.sorter.ApplySort(surfaces) : OrderList(surfaces);
        }


        /// <summary>
        /// Orders a list by type of surface and then by the unqiue id.
        /// </summary>
        /// <param name="surfaces">The list of surfaces.</param>
        /// <returns>An ordered list.</returns>
        private List<GameObject> OrderList(List<GameObject> surfaces)
        {
            return surfaces.OrderBy(surface => GameFinder.IsWhiteboard(surface) ?
                    0 : GameFinder.IsStickyNote(surface) ? 1 : 2)
                .ThenBy(surface => GameFinder.GetUniqueID(surface))
                .ToList();
        }

        /// <summary>
        /// Clears the drawable manager view of all items.
        /// </summary>
        private void ClearManager()
        {
            foreach (Transform child in items)
            {
                if (child != null)
                {
                    Destroyer.Destroy(child.gameObject);
                }
            }
        }

        protected override void Start()
        {
            //subscription = 
            base.Start();
        }

        private void OnDestroy()
        {
            //subscription.Dispose();
            if (SurfaceColorMenu.IsOpen())
            {
                SurfaceColorMenu.Disable();
            }
        }



        #region BaseWindow & Observer
        public override void RebuildLayout()
        {
            // Nothing needs to be done.
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            // TODO: Should tree windows be sent over the network?
            throw new NotImplementedException();
        }

        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            throw new NotImplementedException();
        }

        public override WindowValues ToValueObject()
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            Destroyer.Destroy(this);
        }

        public void OnError(Exception error)
        {
            throw error;
        }

        public void OnNext(ChangeEvent value)
        {
            /// Rebuild tree when surface changes.
            switch(value)
            {
                default:
                    Rebuild();
                    break;
            }
        }
        #endregion
    }
}