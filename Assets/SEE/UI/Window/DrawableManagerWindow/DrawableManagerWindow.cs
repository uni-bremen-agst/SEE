using SEE.Game.Drawable;
using SEE.UI.Window;
using SEE.Utils;
using System;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.UI.Window.DrawableManagerWindow
{
    /// <summary>
    /// A window that represents a manager for managing drawable surfaces. 
    /// The window contains a scrollable list of expandable items.
    /// </summary>
    public partial class DrawableManagerWindow : BaseWindow /// TODO:implement Observer for changes.
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
        /// Transform of the object containing the items of the drawable manager window.
        /// </summary>
        private RectTransform items;

        /// <summary>
        /// TODO: BRauchen wir?
        /// </summary>
        private IDisposable subscription;

        /// <summary>
        /// Adds the drawable surfaces of the visualization to the drawable manager view.
        /// </summary>
        private void AddDrawableSurfaces()
        {
            foreach (GameObject surface in ValueHolder.DrawableSurfaces)
            {
                AddItem(surface);
            }
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
        /*
        private void OnDestroy()
        {
            subscription.Dispose(); 
        }
        */


        #region BaseWindow
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
        #endregion
    }
}