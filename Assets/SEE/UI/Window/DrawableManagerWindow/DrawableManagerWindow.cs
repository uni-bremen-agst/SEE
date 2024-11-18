using Cysharp.Threading.Tasks;
using SEE.DataModel;
using SEE.DataModel.Drawable;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.GO;
using SEE.UI.Menu.Drawable;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace SEE.UI.Window.DrawableManagerWindow
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
        /// All disposables that need to be disposed when the window is destroyed (see <see cref="OnDestroy"/>).
        /// </summary>
        private readonly List<IDisposable> subscriptions = new();

        /// <summary>
        /// Adds the drawable surfaces of the visualization to the drawable manager view.
        /// </summary>
        /// <param name="surfaces">The surfaces to be added; if it is not set, all surfaces of the scene will be added.</param>
        private void AddDrawableSurfaces(List<GameObject> surfaces = null)
        {
            /// Case group is active.
            if (contextMenu.Grouper.IsActive)
            {
                AddGroup("Whiteboards", Icons.Whiteboard,
                    OrderList(contextMenu.Grouper.GetWhiteboards(contextMenu.Filter)),
                    WhiteboardColor);
                AddGroup("Sticky Notes", Icons.StickyNote,
                    OrderList(contextMenu.Grouper.GetStickyNotes(contextMenu.Filter)),
                    StickyNoteColor);
            }
            else /// Group is not active.
            {
                surfaces ??= contextMenu.Filter.GetFilteredSurfaces();
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
            return contextMenu.Sorter.IsActive() ? contextMenu.Sorter.ApplySort(surfaces) : OrderList(surfaces);
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
            surfaceItems.Clear();
        }

        /// <summary>
        /// Adds all <see cref="DrawableSurface"/>s attached to the <see cref="LocalPlayer"/>
        /// or contained in <see cref="ValueHolder.DrawableSurfaces"/> to <see cref="subscriptions"/>.
        /// </summary>
        protected override void Start()
        {
            if (LocalPlayer.TryGetDrawableSurfaces(out DrawableSurfaces surfaces))
            {
                subscriptions.Add(surfaces.Subscribe(this));
            }
            foreach(GameObject surface in ValueHolder.DrawableSurfaces)
            {
                subscriptions.Add(surface.GetComponent<DrawableSurfaceRef>().Surface.Subscribe(this));
            }
            base.Start();
        }

        /// <summary>
        /// If the menu is destroyed, disposes the observers and closes any open <see cref="SurfaceColorMenu"/>
        /// if necessary.
        /// </summary>
        protected override void OnDestroy()
        {
            foreach (IDisposable subscription in subscriptions)
            {
                subscription.Dispose();
            }

            if (SurfaceColorMenu.Instance.IsOpen())
            {
                SurfaceColorMenu.Instance.Destroy();
            }
            base.OnDestroy();
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

        /// <summary>
        /// Responds to an event.
        /// </summary>
        /// <param name="value">The event.</param>
        public void OnNext(ChangeEvent value)
        {
            switch(value)
            {
                case DescriptionChangeEvent:
                    Rebuild(contextMenu.Filter.GetFilteredSurfaces());
                    break;
                case ColorChangeEvent e:
                    if (surfaceItems.TryGetValue(GameFinder.GetUniqueID(e.Surface.CurrentObject), out GameObject item))
                    {
                        Transform foreground = item.transform.Find("Foreground");
                        TextMeshProUGUI colorMesh = foreground.Find("ColorBtn").gameObject.GetComponentInChildren<TextMeshProUGUI>();
                        colorMesh.color = e.Surface.Color;
                    }
                    break;
                case LightingChangeEvent e:
                    if (surfaceItems.TryGetValue(GameFinder.GetUniqueID(e.Surface.CurrentObject), out GameObject itemObj))
                    {
                        Transform foreground = itemObj.transform.Find("Foreground");
                        TextMeshProUGUI lightMesh = foreground.Find("LightingBtn").gameObject.GetComponentInChildren<TextMeshProUGUI>();
                        lightMesh.color = GetLightColor(e.Surface.Lighting);

                        if (e.Surface.Lighting && !contextMenu.Filter.IncludeHaveLighting
                        || !e.Surface.Lighting && !contextMenu.Filter.IncludeHaveNoLighting)
                        {
                            RemoveItem(itemObj);
                        }
                    }
                    break;
                case VisibilityChangeEvent e:
                    if (surfaceItems.TryGetValue(GameFinder.GetUniqueID(e.Surface.CurrentObject), out GameObject itemV))
                    {
                        Transform foreground = itemV.transform.Find("Foreground");
                        TextMeshProUGUI visibilityMesh = foreground.Find("VisibilityBtn").gameObject.GetComponentInChildren<TextMeshProUGUI>();
                        visibilityMesh.text = GetVisibilityText(e.Surface.Visibility);
                        visibilityMesh.color = GetVisibilityColor(e.Surface.Visibility);

                        if (e.Surface.Visibility && !contextMenu.Filter.IncludeIsVisible
                        || !e.Surface.Visibility && !contextMenu.Filter.IncludeIsInvisibile)
                        {
                            RemoveItem(itemV);
                        }
                    }
                    break;
                case PageChangeEvent e:
                    if (surfaceItems.TryGetValue(GameFinder.GetUniqueID(e.Surface.CurrentObject), out GameObject itemP))
                    {
                        Transform foreground = itemP.transform.Find("Foreground");
                        TextMeshProUGUI pageMesh = foreground.Find("PageBtn").gameObject.GetComponentInChildren<TextMeshProUGUI>();
                        pageMesh.text = e.Surface.CurrentPage.ToString();
                    }
                    break;
                case AddSurfaceEvent e:
                    if (!e.Surface.InitFinishIndicator)
                    {
                        WaitForInitAsync(e.Surface).Forget();
                    }
                    else
                    {
                        if (!subscriptions.Contains(e.Surface.Subscribe(this)))
                        {
                            subscriptions.Add(e.Surface.Subscribe(this));
                        }
                        Rebuild();
                    }
                    break;
                case RemoveSurfaceEvent e:
                    if (surfaceItems.TryGetValue(GameFinder.GetUniqueID(e.Surface.CurrentObject), out GameObject toDelete))
                    {
                        RemoveItem(toDelete);
                    }
                    break;
            }
        }

        /// <summary>
        /// Waits for the complete instantiation of a <see cref="DrawableSurface"> before adding the surface to the window.
        /// This prevents incorrect display and other resulting errors.
        /// </summary>
        /// <param name="surface">The surface to be displayed.</param>
        /// <returns>Nothing, it waits until the surface is instantiated.</returns>
        private async UniTask WaitForInitAsync(DrawableSurface surface)
        {
            while (!surface.InitFinishIndicator)
            {
                await UniTask.Yield();
            }
            if (!subscriptions.Contains(surface.Subscribe(this)))
            {
                subscriptions.Add(surface.Subscribe(this));
            }
            Rebuild();
            UICanvas.Refresh();
        }

        #endregion
    }
}
