using HighlightPlus;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Notification;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SEE.Utils.Paths;
using SEE.Utils.History;
using SEE.Game.Drawable.ValueHolders;
using SEE.Game.Drawable.ActionHelpers;
using System.Linq;
using SEE.UI;
using System;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Adds the <see cref="DrawableType"/> to the scene from one or more drawable configs saved
    /// in a file on the disk.
    /// </summary>
    public class LoadAction : DrawableAction
    {
        /// <summary>
        /// Represents how the file was loaded.
        /// </summary>
        public enum LoadState
        {
            /// <summary>
            /// Loaded the drawable(s) from file one-to-one into the same drawable.
            /// </summary>
            Regular,
            /// <summary>
            /// Loaded the drawable(s) from the given file to one specific drawable.
            /// </summary>
            Specific
        }
        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="LoadAction"/>
        /// </summary>
        private class Memento
        {
            /// <summary>
            /// The load state of the action
            /// </summary>
            public readonly LoadState State;
            /// <summary>
            /// The specific chosen drawable surface (needed for LoadState.Specific)
            /// </summary>
            public DrawableConfig SpecificSurface;
            /// <summary>
            /// The drawable configurations.
            /// </summary>
            public DrawablesConfigs Configs;
            /// <summary>
            /// Are the drawable surfaces that are created during the loading process.
            /// </summary>
            public List<DrawableConfig> AddedSurface;
            /// <summary>
            /// Holder for the old <see cref="DrawableConfig"/>.
            /// </summary>
            public Dictionary<GameObject,  DrawableConfig> OldConfig;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="state">The kind how the file was loaded.</param>
            public Memento(LoadState state)
            {
                State = state;
                SpecificSurface = null;
                Configs = null;
                AddedSurface = new();
                OldConfig = new();
            }
        }

        /// <summary>
        /// Ensures that we save only once per click.
        /// </summary>
        private bool clicked = false;

        /// <summary>
        /// The selected drawable surface for specific loading.
        /// </summary>
        private GameObject selectedSurface;

        /// <summary>
        /// The instance for the drawable file browser.
        /// </summary>
        private DrawableFileBrowser browser;

        /// <summary>
        /// Creates the load menu and adds the neccressary handler for the buttons.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            /// The load button for loading onto the original drawable.
            UnityAction loadButtonCall = () =>
            {
                if (browser == null || (browser != null && !browser.IsOpen()))
                {
                    browser = UICanvas.Canvas.AddOrGetComponent<DrawableFileBrowser>();
                    browser.LoadDrawableConfiguration(LoadState.Regular);
                    memento = new(LoadState.Regular);
                }
            };

            /// The load button for loading onto a specific drawable.
            UnityAction loadSpecificButtonCall = () =>
            {
                if (browser == null || (browser != null && !browser.IsOpen()))
                {
                    if (selectedSurface != null)
                    {
                        browser = UICanvas.Canvas.AddOrGetComponent<DrawableFileBrowser>();
                        browser.LoadDrawableConfiguration(LoadState.Specific);
                        memento = new(LoadState.Specific);
                    }
                    else
                    {
                        ShowNotification.Warn("No drawable selected.", "Select a drawable to load specifically.");
                    }
                }
            };

            LoadMenu.Enable(loadButtonCall, loadSpecificButtonCall);
        }

        /// <summary>
        /// Stops the <see cref="LoadAction"/>.
        /// Destroys the load menu and if there are still an activ
        /// highlight effect
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            LoadMenu.Instance.Destroy();
            selectedSurface?.Destroy<HighlightEffect>();
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.Load"/>.
        /// It provides the user with two loading options.
        /// One is to load onto the original drawable,
        /// and the other is to load onto a specifically selected drawable.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        public override bool Update()
        {
            Cancel();
            bool result = false;

            if (!Raycasting.IsMouseOverGUI())
            {
                /// This block marks the selected drawable.
                /// If it has already been selected, the marking is cleared.
                /// For execution, no open file browser should exist.
                if (Selector.SelectQueryHasOrIsDrawableSurface(out RaycastHit raycastHit)
                    && !clicked
                    && (browser == null || (browser != null && !browser.IsOpen())))
                {
                    clicked = true;
                    ManageHighlightEffect(GameFinder.GetDrawableSurface(raycastHit.collider.gameObject));
                }

                /// It is needed to enable the switching of the drawable for the specific load.
                if (Queries.MouseUp(MouseButton.Left))
                {
                    clicked = false;
                }

                /// This block will be executed when a file was successfully chosen.
                if (browser != null && browser.TryGetFilePath(out string filePath) && memento != null)
                {
                    Load(ref result, filePath);
                }
            }
            return result;
        }

        /// <summary>
        /// Deactivates the selected drawable
        /// </summary>
        private void Cancel()
        {
            if (SEEInput.Cancel()
                && selectedSurface != null
                && selectedSurface.GetComponent<HighlightEffect>() != null
                && (browser == null || (browser != null && !browser.IsOpen())))
            {
                ShowNotification.Info("Unselect drawable", "The marked drawable was unselected.");
                selectedSurface.Destroy<HighlightEffect>();
                selectedSurface = null;
            }
        }

        /// <summary>
        /// Manages the highlight effect for drawables.
        /// Only one drawable can be highlighted at a time.
        /// When a new selection is made, the highlight of the previous drawable is cleared.
        /// Additionally, the option to deselect the drawable is provided.
        /// </summary>
        /// <param name="surface">The drawable surface to be highlighted</param>
        private void ManageHighlightEffect(GameObject surface)
        {
            if (surface.GetComponent<HighlightEffect>() == null)
            {
                selectedSurface?.Destroy<HighlightEffect>();
                selectedSurface = surface;
                GameHighlighter.EnableGlowOverlay(selectedSurface);
            }
            else
            {
                Destroyer.Destroy(surface.GetComponent<HighlightEffect>());
                selectedSurface = null;
            }
        }

        /// <summary>
        /// Executes the corresponding loading option based on the user's choice
        /// (load onto the original drawable / load onto a specific drawable).
        /// Additionally, when loading onto the original drawable,
        /// sticky notes are spawned if the drawable does not yet exist in the game world.
        /// </summary>
        /// <param name="result">The referenced bool Result variable from Update to represent the success of the action.</param>
        /// <param name="filePath">The chosen file path.</param>
        private void Load(ref bool result, string filePath)
        {
            switch (memento.State)
            {
                /// This block loads one drawable onto the specific chosen drawable.
                case LoadState.Specific:
                    memento.SpecificSurface = DrawableConfigManager.GetDrawableConfig(selectedSurface);
                    DrawablesConfigs configsSpecific = DrawableConfigManager.LoadDrawables(new DataPath(filePath));
                    foreach (DrawableConfig drawableConfig in configsSpecific.Drawables)
                    {
                        Restore(memento.SpecificSurface.GetDrawableSurface(), drawableConfig);
                    }
                    GameDrawableManager.ChangeCurrentPage(memento.SpecificSurface.GetDrawableSurface(), 0);
                    int max = DrawableConfigManager.GetDrawableConfig(selectedSurface).GetAllDrawableTypes()
                        .Aggregate((t1, t2) => t1.AssociatedPage > t2.AssociatedPage ? t1 : t2).AssociatedPage;
                    GameDrawableManager.ChangeMaxPage(memento.SpecificSurface.GetDrawableSurface(), max + 1);
                    memento.Configs = configsSpecific;
                    CurrentState = IReversibleAction.Progress.Completed;
                    result = true;
                    break;

                /// This block loads one or more drawables onto the drawables of the configuration.
                case LoadState.Regular:
                    DrawablesConfigs configs = DrawableConfigManager.LoadDrawables(new DataPath(filePath));
                    foreach (DrawableConfig drawableConfig in configs.Drawables)
                    {
                        GameObject surfaceOfFile = GameFinder.FindDrawableSurface(drawableConfig.ID, drawableConfig.ParentID);
                        /// If the sticky note already exists, create a new one.
                        if (surfaceOfFile != null && GameFinder.IsStickyNote(surfaceOfFile)) {
                            surfaceOfFile = null;
                            drawableConfig.ParentID = GameStickyNoteManager.CreateUnusedName();
                        }
                        /// If the drawable does not exist it will be spawned as a sticky note.
                        if (surfaceOfFile == null)
                        {
                            memento.AddedSurface.Add(drawableConfig);
                            GameObject stickyNote = GameStickyNoteManager.Spawn(drawableConfig);
                            surfaceOfFile = GameFinder.GetDrawableSurface(stickyNote);
                            new StickyNoteSpawnNetAction(drawableConfig).Execute();
                        } else
                        {
                            memento.OldConfig.Add(surfaceOfFile, DrawableConfigManager.GetDrawableConfig(surfaceOfFile));
                        }
                        Restore(surfaceOfFile, drawableConfig);
                        DrawableConfig.Restore(surfaceOfFile, drawableConfig);
                    }
                    memento.Configs = configs;
                    CurrentState = IReversibleAction.Progress.Completed;
                    result = true;
                    break;
            }
        }

        /// <summary>
        /// Restores all the <see cref="DrawableType"/> objects of the configuration.
        /// </summary>
        /// <param name="surface">The drawable surface on which the configuration should restore.</param>
        /// <param name="config">The configuration that holds the drawable type configuration to restore.</param>
        private void Restore(GameObject surface, DrawableConfig config)
        {
            GameObject attachedObject = GameFinder.GetAttachedObjectsObject(surface);
            if (attachedObject != null)
            {
                GameMindMap.RenameMindMap(config, attachedObject);
            }
            foreach (DrawableType type in config.GetAllDrawableTypes())
            {
                if (attachedObject != null && type is not MindMapNodeConf)
                {
                    CheckAndChangeID(type, attachedObject, DrawableType.GetPrefix(type));
                }
                DrawableType.Restore(type, surface);
            }
        }

        /// <summary>
        /// When the id of the given config already exist on the drawable, the id will be changed.
        /// </summary>
        /// <param name="conf">The configuration to restore.</param>
        /// <param name="attachedObjects">The objects that are attached on a drawable</param>
        /// <param name="prefix">The prefix for the drawable type object.</param>
        private void CheckAndChangeID (DrawableType conf, GameObject attachedObjects, string prefix)
        {
            if (GameFinder.FindChild(attachedObjects, conf.Id) != null
                && !conf.Id.Contains(ValueHolder.MindMapBranchLine))
            {
                string newName = prefix + "-" + RandomStrings.GetRandomString(8);
                while (GameFinder.FindChild(attachedObjects, newName) != null)
                {
                    newName = prefix + "-" + RandomStrings.GetRandomString(8);
                }
                conf.Id = newName;
            }
        }

        /// <summary>
        /// Destroys the objects that were loaded from the configuration.
        /// </summary>
        /// <param name="attachedObjects">The objects that are attached on a drawable</param>
        /// <param name="config">Configuration that contains all objects to be removed.</param>
        private void DestroyLoadedObjects(GameObject attachedObjects, DrawableConfig config)
        {
            if (attachedObjects != null)
            {
                GameObject surface = GameFinder.GetDrawableSurface(attachedObjects);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
                foreach (DrawableType type in config.GetAllDrawableTypes())
                {
                    GameObject typeObj = GameFinder.FindChild(attachedObjects, type.Id);
                    if (typeObj != null)
                    {
                        new EraseNetAction(surface.name, surfaceParentName, typeObj.name).Execute();
                        Destroyer.Destroy(typeObj);
                    }
                }

                int order = 1;
                foreach (DrawableType type in DrawableConfigManager.GetDrawableConfig(surface).GetAllDrawableTypes() )
                {
                    if (type.OrderInLayer >= order)
                    {
                        order = type.OrderInLayer + 1;
                    }
                }
                surface.GetComponent<DrawableHolder>().OrderInLayer = order;
                new SynchronizeSurface(DrawableConfigManager.GetDrawableConfig(surface)).Execute();
            }
        }

        /// <summary>
        /// Reverts this instance of the action, i.e., deletes the objects that were loaded from the file.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            switch (memento.State)
            {
                case LoadState.Specific:
                    GameObject attachedObjs = GameFinder.GetAttachedObjectsObject(
                        memento.SpecificSurface.GetDrawableSurface());
                    foreach (DrawableConfig config in memento.Configs.Drawables)
                    {
                        DestroyLoadedObjects(attachedObjs, config);
                    }
                    break;
                case LoadState.Regular:
                    foreach (DrawableConfig config in memento.Configs.Drawables)
                    {
                        /// Deletes the sticky note if it was created by the corresponding load action.
                        if (memento.AddedSurface.Contains(config))
                        {
                            GameObject surface = GameFinder.FindDrawableSurface(config.ID,
                                config.ParentID);
                            new StickyNoteDeleterNetAction(DrawableConfigManager.GetDrawableConfig(surface)).Execute();
                            Destroyer.Destroy(GameFinder.GetHighestParent(surface));
                        }
                        else
                        {
                            GameObject surface = GameFinder.FindDrawableSurface(config.ID, config.ParentID);
                            GameObject attachedObj = GameFinder.GetAttachedObjectsObject(surface);
                            DestroyLoadedObjects(attachedObj, config);
                        }
                    }
                    foreach (KeyValuePair<GameObject, DrawableConfig> pair in memento.OldConfig)
                    {
                        DrawableConfig.Restore(pair.Key, pair.Value);
                    }
                    break;
            }
        }

        /// <summary>
        /// Repeats this action, i.e., loads the configuration again.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            switch (memento.State)
            {
                case LoadState.Specific:
                    GameObject specificSurface = memento.SpecificSurface.GetDrawableSurface();
                    foreach (DrawableConfig config in memento.Configs.Drawables)
                    {
                        Restore(specificSurface, config);
                    }
                    break;
                case LoadState.Regular:
                    foreach (DrawableConfig config in memento.Configs.Drawables)
                    {
                        GameObject surface = GameFinder.FindDrawableSurface(config.ID, config.ParentID);
                        /// Spawns the sticky note if the drawable can't be found.
                        if (surface == null)
                        {
                            surface = GameFinder.GetDrawableSurface(GameStickyNoteManager.Spawn(config));
                            new StickyNoteSpawnNetAction(config).Execute();
                        }
                        Restore(surface, config);
                        DrawableConfig.Restore(surface, config);
                    }
                    break;
            }
        }

        /// <summary>
        /// A new instance of <see cref="LoadAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LoadAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new LoadAction();
        }

        /// <summary>
        /// A new instance of <see cref="LoadAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LoadAction"/></returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Load"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Load;
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
            if (memento == null)
            {
                return new();
            }
            else
            {
                HashSet<string> changedObjects = new();
                if (memento.State == LoadState.Regular)
                {
                    foreach(DrawableConfig config in memento.Configs.Drawables)
                    {
                        changedObjects.Add(config.ID);
                        foreach(DrawableType type in config.GetAllDrawableTypes())
                        {
                            changedObjects.Add(type.Id);
                        }
                    }
                }
                else
                {
                    changedObjects.Add(memento.SpecificSurface.ID);
                    foreach (DrawableConfig config in memento.Configs.Drawables)
                    {
                        foreach (DrawableType type in config.GetAllDrawableTypes())
                        {
                            changedObjects.Add(type.Id);
                        }
                    }
                }
                return changedObjects;
            }
        }
    }
}
