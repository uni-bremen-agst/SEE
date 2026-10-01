using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.MindMap;
using SEE.Game.Drawable.StickyNote;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using SEE.Utils.Paths;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable.Load
{
    /// <summary>
    /// Handles loading drawable configurations, including restoring their drawable objects
    /// and reverting or repeating completed load operations.
    /// </summary>
    internal sealed class LoadOperation
    {
        /// <summary>
        /// Stores all information required to undo or redo a completed load operation.
        /// </summary>
        internal sealed class State
        {
            /// <summary>
            /// The mode defining how the configurations are loaded.
            /// </summary>
            internal LoadAction.LoadState Mode { get; }

            /// <summary>
            /// The mode defining how loaded page indices are handled.
            /// </summary>
            internal LoadAction.LoadPageMode PageMode { get; }

            /// <summary>
            /// The configuration of the selected target surface for a specific load.
            /// </summary>
            internal DrawableConfig SpecificSurface { get; set; }

            /// <summary>
            /// The configurations loaded from the selected file.
            /// </summary>
            internal DrawablesConfigs Configs { get; set; }

            /// <summary>
            /// The drawable surfaces that were created while loading.
            /// </summary>
            internal List<DrawableConfig> AddedSurfaces { get; } = new();

            /// <summary>
            /// The configurations of existing surfaces before they were changed by loading.
            /// </summary>
            internal Dictionary<GameObject, DrawableConfig> OldConfigs { get; } = new();

            /// <summary>
            /// Creates a new state for a load operation.
            /// </summary>
            /// <param name="mode">The mode defining how the configurations are loaded.</param>
            /// <param name="pageMode">
            /// The mode defining how loaded page indices are handled.
            /// </param>
            internal State(LoadAction.LoadState mode,
                LoadAction.LoadPageMode pageMode = LoadAction.LoadPageMode.KeepStoredPages)
            {
                Mode = mode;
                PageMode = pageMode;
            }
        }

        /// <summary>
        /// Loads the drawable configurations from the given file according to the selected load mode.
        /// </summary>
        /// <param name="state">
        /// The state in which information required for undo and redo is stored.
        /// </param>
        /// <param name="selectedSurface">
        /// The currently selected drawable surface. This is required for a specific load.
        /// </param>
        /// <param name="filePath">The path of the file to load.</param>
        /// <returns>Whether a supported load mode was executed.</returns>
        internal bool Execute(State state, GameObject selectedSurface, string filePath)
        {
            switch (state.Mode)
            {
                case LoadAction.LoadState.Specific:
                    ExecuteSpecific(state, selectedSurface, filePath);
                    return true;

                case LoadAction.LoadState.Regular:
                    ExecuteRegular(state, filePath);
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Loads all drawable objects from the given file onto the selected drawable surface.
        /// </summary>
        /// <param name="state">
        /// The state in which information required for undo and redo is stored.
        /// </param>
        /// <param name="selectedSurface">The drawable surface onto which the objects are loaded.</param>
        /// <param name="filePath">The path of the file to load.</param>
        private static void ExecuteSpecific(State state, GameObject selectedSurface, string filePath)
        {
            state.SpecificSurface = DrawableConfigManager.GetDrawableConfig(selectedSurface);
            DrawablesConfigs configs = DrawableConfigManager.LoadDrawables(new DataPath(filePath));

            if (state.PageMode == LoadAction.LoadPageMode.CurrentSelectedPage)
            {
                int targetPage = selectedSurface.GetComponent<DrawableHolder>().CurrentPage;

                foreach (DrawableConfig drawableConfig in configs.Drawables)
                {
                    DrawableConfigManager.RemapAllTypesToPage(drawableConfig, targetPage);
                }
            }

            foreach (DrawableConfig drawableConfig in configs.Drawables)
            {
                Restore(state.SpecificSurface.GetDrawableSurface(), drawableConfig);
            }

            if (state.PageMode == LoadAction.LoadPageMode.CurrentSelectedPage)
            {
                int targetPage = selectedSurface.GetComponent<DrawableHolder>().CurrentPage;

                GameDrawablePageManager.ChangeCurrentPage(
                    state.SpecificSurface.GetDrawableSurface(),
                    targetPage);

                GameDrawablePageManager.ChangeMaxPage(
                    state.SpecificSurface.GetDrawableSurface(),
                    Mathf.Max(
                        selectedSurface.GetComponent<DrawableHolder>().MaxPageSize,
                        targetPage + 1));
            }
            else
            {
                GameDrawablePageManager.ChangeCurrentPage(
                    state.SpecificSurface.GetDrawableSurface(),
                    0);

                int max = DrawableConfigManager.GetDrawableConfig(selectedSurface)
                    .GetAllDrawableTypes()
                    .Select(type => type.AssociatedPage)
                    .DefaultIfEmpty(0)
                    .Max();

                GameDrawablePageManager.ChangeMaxPage(
                    state.SpecificSurface.GetDrawableSurface(),
                    max + 1);
            }

            state.Configs = configs;
        }

        /// <summary>
        /// Loads all drawable configurations from the given file onto their corresponding surfaces.
        /// Missing surfaces are restored as sticky notes.
        /// </summary>
        /// <param name="state">
        /// The state in which information required for undo and redo is stored.
        /// </param>
        /// <param name="filePath">The path of the file to load.</param>
        private static void ExecuteRegular(State state, string filePath)
        {
            DrawablesConfigs configs = DrawableConfigManager.LoadDrawables(new DataPath(filePath));

            foreach (DrawableConfig drawableConfig in configs.Drawables)
            {
                GameObject surfaceOfFile =
                    GameFinder.FindDrawableSurface(drawableConfig.ID, drawableConfig.ParentID);

                if (surfaceOfFile != null && GameFinder.IsStickyNote(surfaceOfFile))
                {
                    surfaceOfFile = null;
                    drawableConfig.ParentID = GameStickyNoteManager.CreateUnusedName();
                }

                if (surfaceOfFile == null)
                {
                    state.AddedSurfaces.Add(drawableConfig);

                    GameObject stickyNote = GameStickyNoteManager.Spawn(drawableConfig);
                    surfaceOfFile = GameFinder.GetDrawableSurface(stickyNote);

                    new StickyNoteSpawnNetAction(drawableConfig).Execute();
                }
                else
                {
                    state.OldConfigs.Add(
                        surfaceOfFile,
                        DrawableConfigManager.GetDrawableConfig(surfaceOfFile));
                }

                Restore(surfaceOfFile, drawableConfig);
                DrawableConfig.Restore(surfaceOfFile, drawableConfig);
            }

            state.Configs = configs;
        }

        /// <summary>
        /// Restores all drawable objects contained in the given configuration.
        /// </summary>
        /// <param name="surface">The drawable surface on which the objects are restored.</param>
        /// <param name="config">The configuration containing the objects to restore.</param>
        private static void Restore(GameObject surface, DrawableConfig config)
        {
            GameObject attachedObjects = GameFinder.GetAttachedObjectsObject(surface);

            if (attachedObjects != null)
            {
                GameMindMap.RenameMindMap(config, attachedObjects);
            }

            foreach (DrawableType type in config.GetAllDrawableTypes())
            {
                if (attachedObjects != null && type is not MindMapNodeConf)
                {
                    CheckAndChangeID(type, attachedObjects, DrawableType.GetPrefix(type));
                }

                DrawableType.Restore(type, surface);
            }
        }

        /// <summary>
        /// Changes the ID of the given configuration if an object with the same ID already
        /// exists on the drawable surface.
        /// </summary>
        /// <param name="configuration">The configuration whose ID is checked.</param>
        /// <param name="attachedObjects">
        /// The object containing the drawable objects attached to the surface.
        /// </param>
        /// <param name="prefix">The prefix used when creating a replacement ID.</param>
        private static void CheckAndChangeID(
            DrawableType configuration,
            GameObject attachedObjects,
            string prefix)
        {
            if (GameFinder.FindAttachedOrLocalDescendant(attachedObjects, configuration.ID) != null
                && !configuration.ID.Contains(ValueHolder.MindMapBranchLine))
            {
                string newName = prefix + "-" + RandomStrings.GetRandomString(8);

                while (GameFinder.FindAttachedOrLocalDescendant(attachedObjects, newName) != null)
                {
                    newName = prefix + "-" + RandomStrings.GetRandomString(8);
                }

                configuration.ID = newName;
            }
        }

        /// <summary>
        /// Destroys the drawable objects that were restored from the given configuration.
        /// </summary>
        /// <param name="attachedObjects">
        /// The object containing the drawable objects attached to the surface.
        /// </param>
        /// <param name="config">The configuration containing the objects to remove.</param>
        private static void DestroyLoadedObjects(GameObject attachedObjects, DrawableConfig config)
        {
            if (attachedObjects == null)
            {
                return;
            }

            GameObject surface = GameFinder.GetDrawableSurface(attachedObjects);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

            foreach (DrawableType type in config.GetAllDrawableTypes())
            {
                GameObject typeObject =
                    GameFinder.FindAttachedOrLocalDescendant(attachedObjects, type.ID);

                if (typeObject != null)
                {
                    new EraseNetAction(surface.name, surfaceParentName, typeObject.name).Execute();
                    Destroyer.Destroy(typeObject);
                }
            }

            int order = 1;

            foreach (DrawableType type
                     in DrawableConfigManager.GetDrawableConfig(surface).GetAllDrawableTypes())
            {
                if (type.OrderInLayer >= order)
                {
                    order = type.OrderInLayer + 1;
                }
            }

            surface.GetComponent<DrawableHolder>().OrderInLayer = order;
            new SynchronizeSurface(DrawableConfigManager.GetDrawableConfig(surface)).Execute();
        }

        /// <summary>
        /// Reverts the completed load operation.
        /// </summary>
        /// <param name="state">The state of the load operation to revert.</param>
        internal void Undo(State state)
        {
            switch (state.Mode)
            {
                case LoadAction.LoadState.Specific:
                    GameObject attachedObjects = GameFinder.GetAttachedObjectsObject(
                        state.SpecificSurface.GetDrawableSurface());

                    foreach (DrawableConfig config in state.Configs.Drawables)
                    {
                        DestroyLoadedObjects(attachedObjects, config);
                    }

                    break;

                case LoadAction.LoadState.Regular:
                    foreach (DrawableConfig config in state.Configs.Drawables)
                    {
                        if (state.AddedSurfaces.Contains(config))
                        {
                            GameObject surface =
                                GameFinder.FindDrawableSurface(config.ID, config.ParentID);

                            new StickyNoteDeleterNetAction(
                                DrawableConfigManager.GetDrawableConfig(surface)).Execute();

                            Destroyer.Destroy(surface.GetRootParent());
                        }
                        else
                        {
                            GameObject surface =
                                GameFinder.FindDrawableSurface(config.ID, config.ParentID);

                            GameObject attachedObject =
                                GameFinder.GetAttachedObjectsObject(surface);

                            DestroyLoadedObjects(attachedObject, config);
                        }
                    }

                    foreach (KeyValuePair<GameObject, DrawableConfig> pair in state.OldConfigs)
                    {
                        DrawableConfig.Restore(pair.Key, pair.Value);
                    }

                    break;
            }
        }

        /// <summary>
        /// Repeats the completed load operation.
        /// </summary>
        /// <param name="state">The state of the load operation to repeat.</param>
        internal void Redo(State state)
        {
            switch (state.Mode)
            {
                case LoadAction.LoadState.Specific:
                    GameObject specificSurface = state.SpecificSurface.GetDrawableSurface();

                    foreach (DrawableConfig config in state.Configs.Drawables)
                    {
                        Restore(specificSurface, config);
                    }

                    break;

                case LoadAction.LoadState.Regular:
                    foreach (DrawableConfig config in state.Configs.Drawables)
                    {
                        GameObject surface =
                            GameFinder.FindDrawableSurface(config.ID, config.ParentID);

                        if (surface == null)
                        {
                            surface = GameFinder.GetDrawableSurface(
                                GameStickyNoteManager.Spawn(config));

                            new StickyNoteSpawnNetAction(config).Execute();
                        }

                        Restore(surface, config);
                        DrawableConfig.Restore(surface, config);
                    }

                    break;
            }
        }
    }
}
