using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.UI.Menu;
using SEE.UI.Notification;
using SEE.UI.PopupMenu;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using Range = SEE.DataModel.DG.Range;

namespace SEE.UI.Window.CodeWindow
{
    public partial class CodeWindow
    {
        /// <summary>
        /// The context menu that this class manages.
        /// </summary>
        private PopupMenu.PopupMenu contextMenu;

        /// <summary>
        /// A list menu that's shown when the user needs to make some selection,
        /// such as when choosing a reference to navigate to.
        /// </summary>
        private SimpleListMenu simpleListMenu;

        /// <summary>
        /// Shows the context menu at the given <paramref name="position"/>, assuming the user right-clicked
        /// at the given <paramref name="line"/> and <paramref name="column"/>.
        /// </summary>
        /// <param name="line">The 0-indexed line where the user right-clicked.</param>
        /// <param name="column">The 0-indexed column where the user right-clicked.</param>
        /// <param name="position">The position where the context menu should be shown.</param>
        /// <param name="contextWord">The word at the given <paramref name="line"/> and <paramref name="column"/>.</param>
        private void ShowContextMenu(int line, int column, Vector2 position, string contextWord)
        {
            IList<PopupMenuAction> actions = new List<PopupMenuAction>();

            // TODO: Type Hierarchy and Call Hierarchy
            if (lspHandler.ServerCapabilities.ReferencesProvider != null)
            {
                actions.Add(new("Show References", ShowReferences, Icons.IncomingEdge));
            }
            if (lspHandler.ServerCapabilities.DeclarationProvider != null)
            {
                actions.Add(new("Go to Declaration", ShowDeclaration, Icons.OutgoingEdge));
            }
            if (lspHandler.ServerCapabilities.DefinitionProvider != null)
            {
                actions.Add(new("Go to Definition", ShowDefinition, Icons.OutgoingEdge));
            }
            if (lspHandler.ServerCapabilities.ImplementationProvider != null)
            {
                actions.Add(new("Go to Implementation", ShowImplementation, Icons.OutgoingEdge));
            }
            if (lspHandler.ServerCapabilities.TypeDefinitionProvider != null)
            {
                actions.Add(new("Go to Type Definition", ShowTypeDefinition, Icons.OutgoingEdge));
            }
            if (lspHandler.ServerCapabilities.CallHierarchyProvider != null)
            {
                actions.Add(new("Show Outgoing Calls", ShowOutgoingCalls, Icons.Sitemap));
            }
            if (lspHandler.ServerCapabilities.TypeHierarchyProvider != null)
            {
                actions.Add(new("Show Supertypes", ShowSupertypes, Icons.Sitemap));
            }

            if (actions.Count == 0)
            {
                return;
            }
            contextMenu.ShowWith(actions, position);

            return;

            #region Local Functions

            void ShowOutgoingCalls()
            {
                MenuEntriesForLocationsAsync(lspHandler.OutgoingCalls(_ => true, FilePath, line, column)
                                                       .Select(x => (x.Uri.ToUri(), Range.FromLspRange(x.Range), x.Name)))
                    .ContinueWith(entries => ShowEntries(entries, "Outgoing Calls")).Forget();
            }

            void ShowSupertypes()
            {
                MenuEntriesForLocationsAsync(lspHandler.Supertypes(_ => true, FilePath, line, column)
                                                       .Select(x => (x.Uri.ToUri(), Range.FromLspRange(x.Range), x.Name)))
                    .ContinueWith(entries => ShowEntries(entries, "Supertypes")).Forget();
            }

            void ShowReferences() =>
                ShowLocationsAsync(lspHandler.References(FilePath, line, column, includeDeclaration: true),
                                   "References").Forget();

            void ShowDeclaration() =>
                ShowLocationsAsync(lspHandler.Declaration(FilePath, line, column), "Declarations").Forget();

            void ShowDefinition() =>
                ShowLocationsAsync(lspHandler.Definition(FilePath, line, column), "Definitions").Forget();

            void ShowImplementation() =>
                ShowLocationsAsync(lspHandler.Implementation(FilePath, line, column), "Implementations").Forget();

            void ShowTypeDefinition() =>
                ShowLocationsAsync(lspHandler.TypeDefinition(FilePath, line, column), "Type Definitions").Forget();

            async UniTask ShowLocationsAsync(IUniTaskAsyncEnumerable<LocationOrLocationLink> locations, string name)
            {
                IList<MenuEntry> entries = await MenuEntriesForLocationsAsync(locations.Select(DeconstructLocation));
                ShowEntries(entries, name);
            }

            (Uri, Range, string) DeconstructLocation(LocationOrLocationLink location)
            {
                Range targetRange;
                Uri targetUri;
                if (location.IsLocation)
                {
                    Location loc = location.Location!;
                    targetRange = Range.FromLspRange(loc.Range);
                    targetUri = loc.Uri.ToUri();
                }
                else
                {
                    LocationLink locLink = location.LocationLink!;
                    targetRange = Range.FromLspRange(locLink.TargetRange);
                    targetUri = locLink.TargetUri.ToUri();
                }
                return (targetUri, targetRange, null);
            }

            // Generates menu entries for the given locations. Clicking on an entry will open the location.
            async UniTask<IList<MenuEntry>> MenuEntriesForLocationsAsync(IUniTaskAsyncEnumerable<(Uri, Range, string)> locations)
            {
                IList<MenuEntry> entries = new List<MenuEntry>();
                using (LoadingSpinner.ShowIndeterminate($"Loading {name} for \"{contextWord}\"..."))
                {
                    await foreach ((Uri targetUri, Range targetRange, string title) in locations)
                    {
                        Uri path = targetUri;
                        if (lspHandler.ProjectUri?.IsBaseOf(path) ?? false)
                        {
                            // Truncate path above the project's base path to make the result more readable.
                            path = lspHandler.ProjectUri.MakeRelativeUri(path);
                        }
                        entries.Add(new(SelectAction: () => OpenSelection(targetUri, targetRange),
                                        Title: title ?? $"{path}: {targetRange}",
                                        // TODO: Icon
                                        EntryColor: new Color(0.051f, 0.3608f, 0.1333f)));
                    }
                    await UniTask.SwitchToMainThread();
                }
                return entries;
            }

            // Opens a menu with the given menu entries.
            void ShowEntries(IList<MenuEntry> entries, string name)
            {
                switch (entries.Count)
                {
                    case 0:
                        ShowNotification.Info("No results", $"No {name} found for \"{contextWord}\".", log: false);
                        break;
                    case 1:
                        // We can directly open the only result.
                        entries.First().SelectAction();
                        break;
                    default:
                        // The user needs to select one of the results.
                        simpleListMenu.ClearEntries();
                        simpleListMenu.AddEntries(entries);
                        simpleListMenu.Title = name;
                        simpleListMenu.Description = $"Listing {name.ToLower()} for {contextWord}.";
                        simpleListMenu.Icon = Resources.Load<Sprite>("Materials/Notification/info");
                        simpleListMenu.ShowMenu = true;
                        break;
                }
            }

            // Opens the selection at the given uri and range. If the uri is the current file, we just
            // scroll to the range, otherwise we open a new (or existing) code window.
            void OpenSelection(Uri uri, Range range)
            {
                // If this is the current file, we can just scroll to the range.
                if (FilePath == uri.LocalPath)
                {
                    // TODO: Allow going back (button or keyboard shortcut)
                    ScrolledVisibleLine = range.Start.Line;
                }
                else
                {
                    // Otherwise, we need to open a different code window.
                    Assert.IsNotNull(AssociatedGraph);
                    CodeWindow window = ShowCodeAction.ShowCodeForPath(AssociatedGraph, uri.LocalPath, range,
                                                                       w => w.ScrolledVisibleLine = range.Start.Line);
                    if (window != null)
                    {
                        WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
                        if (!manager.Windows.Contains(window))
                        {
                            manager.AddWindow(window);
                        }
                        manager.ActiveWindow = window;
                    }
                }
            }

            #endregion
        }
    }
}
