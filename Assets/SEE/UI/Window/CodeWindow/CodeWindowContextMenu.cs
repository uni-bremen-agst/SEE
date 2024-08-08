using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.Tools.LSP;
using SEE.UI.Menu;
using SEE.UI.Notification;
using SEE.UI.PopupMenu;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using Range = SEE.DataModel.DG.Range;

namespace SEE.UI.Window.CodeWindow
{
    /// <summary>
    /// Partial class containing methods related to context menus and LSP navigation in code windows.
    /// In addition, this part contains a record representing a context menu handler for a code window.
    /// </summary>
    public partial class CodeWindow
    {
        /// <summary>
        /// A handler for the context menu of code windows that provides various navigation options,
        /// such as "Go to Definition" or "Find References".
        /// </summary>
        /// <param name="path">The path of the file that is displayed in the code window.</param>
        /// <param name="lspHandler">The LSP handler that provides the language server capabilities.</param>
        /// <param name="simpleListMenu">A list menu that's shown when the user needs to make some selection,
        /// such as when choosing a reference to navigate to.</param>
        /// <param name="contextMenu">The context menu that this class manages.</param>
        /// <param name="OpenSelection">A callback that opens the given URI and range in a new code window
        /// or scrolls to the range if the URI is the same as the current file.</param>
        private record ContextMenuHandler(string path, LSPHandler lspHandler, PopupMenu.PopupMenu contextMenu,
                                          SimpleListMenu simpleListMenu, Action<Uri, Range> OpenSelection)
        {
            /// <summary>
            /// Creates and initializes a new context menu handler for the given <paramref name="codeWindow"/>.
            /// </summary>
            /// <param name="codeWindow">The code window for which the context menu should be created.</param>
            /// <returns>The created context menu handler.</returns>
            public static ContextMenuHandler FromCodeWindow(CodeWindow codeWindow)
            {
                PopupMenu.PopupMenu contextMenu = codeWindow.gameObject.AddComponent<PopupMenu.PopupMenu>();
                SimpleListMenu simpleListMenu = codeWindow.gameObject.AddComponent<SimpleListMenu>();
                return new ContextMenuHandler(codeWindow.FilePath, codeWindow.lspHandler, contextMenu,
                                              simpleListMenu, codeWindow.OpenSelection);
            }

            /// <summary>
            /// Shows the context menu at the given <paramref name="position"/>, assuming the user right-clicked
            /// at the given <paramref name="line"/> and <paramref name="column"/>.
            /// </summary>
            /// <param name="line">The 0-indexed line where the user right-clicked.</param>
            /// <param name="column">The 0-indexed column where the user right-clicked.</param>
            /// <param name="position">The position where the context menu should be shown.</param>
            /// <param name="contextWord">The word at the given <paramref name="line"/> and <paramref name="column"/>.</param>
            public void Show(int line, int column, Vector2 position, string contextWord)
            {
                IList<PopupMenuAction> actions = new List<PopupMenuAction>();

                if (lspHandler.ServerCapabilities.ReferencesProvider != null)
                {
                    actions.Add(new("Find References", WithLineColumn(ShowReferences), Icons.MagnifyingGlass));
                }
                if (lspHandler.ServerCapabilities.DeclarationProvider != null)
                {
                    actions.Add(new("Go to Declaration", WithLineColumn(ShowDeclaration), Icons.IncomingEdge));
                }
                if (lspHandler.ServerCapabilities.DefinitionProvider != null)
                {
                    actions.Add(new("Go to Definition", WithLineColumn(ShowDefinition), Icons.IncomingEdge));
                }
                if (lspHandler.ServerCapabilities.TypeDefinitionProvider != null)
                {
                    actions.Add(new("Go to Type Definition", WithLineColumn(ShowTypeDefinition), Icons.IncomingEdge));
                }
                if (lspHandler.ServerCapabilities.ImplementationProvider != null)
                {
                    actions.Add(new("Go to Implementation", WithLineColumn(ShowImplementation), Icons.OutgoingEdge));
                }
                if (lspHandler.ServerCapabilities.CallHierarchyProvider != null)
                {
                    actions.Add(new("Show Outgoing Calls", WithLineColumn(ShowOutgoingCalls), Icons.Sitemap));
                }
                if (lspHandler.ServerCapabilities.TypeHierarchyProvider != null)
                {
                    actions.Add(new("Show Supertypes", WithLineColumn(ShowSupertypes), Icons.Sitemap));
                }

                if (actions.Count > 0)
                {
                    contextMenu.ShowWith(actions, position);
                }
                return;

                // Calls the given action with the line, column, and context word given to Show.
                Action WithLineColumn(Action<int, int, string> action) => () => action(line, column, contextWord);
            }

            /// <summary>
            /// Shows the outgoing calls for the given <paramref name="line"/> and <paramref name="column"/>.
            /// </summary>
            /// <param name="line">The 0-indexed line for which to show the outgoing calls.</param>
            /// <param name="column">The 0-indexed column for which to show the outgoing calls.</param>
            /// <param name="contextWord">The word at the given <paramref name="line"/> and <paramref name="column"/>.</param>
            private void ShowOutgoingCalls(int line, int column, string contextWord)
            {
                const string name = "Outgoing Calls";
                MenuEntriesForLocationsAsync(lspHandler.OutgoingCalls(_ => true, path, line, column)
                                                       .Select(x => (x.Uri.ToUri(), Range.FromLspRange(x.Range), x.Name)),
                                             name, contextWord)
                    .ContinueWith(entries => ShowEntries(entries, name, contextWord)).Forget();
            }

            /// <summary>
            /// Shows the outgoing calls for the given <paramref name="line"/> and <paramref name="column"/>.
            /// </summary>
            /// <param name="line">The 0-indexed line for which to show the outgoing calls.</param>
            /// <param name="column">The 0-indexed column for which to show the outgoing calls.</param>
            /// <param name="contextWord">The word at the given <paramref name="line"/> and <paramref name="column"/>.</param>
            private void ShowSupertypes(int line, int column, string contextWord)
            {
                const string name = "Supertypes";
                MenuEntriesForLocationsAsync(lspHandler.Supertypes(_ => true, path, line, column)
                                                       .Select(x => (x.Uri.ToUri(), Range.FromLspRange(x.Range), x.Name)),
                                             name, contextWord)
                    .ContinueWith(entries => ShowEntries(entries, name, contextWord)).Forget();
            }

            /// <summary>
            /// Shows the references for the given <paramref name="line"/> and <paramref name="column"/>.
            /// </summary>
            /// <param name="line">The 0-indexed line for which to show the references.</param>
            /// <param name="column">The 0-indexed column for which to show the references.</param>
            /// <param name="contextWord">The word at the given <paramref name="line"/> and <paramref name="column"/>.</param>
            private void ShowReferences(int line, int column, string contextWord) =>
                ShowLocationsAsync(lspHandler.References(path, line, column, includeDeclaration: true), "References", contextWord).Forget();

            /// <summary>
            /// Shows the declaration for the given <paramref name="line"/> and <paramref name="column"/>.
            /// </summary>
            /// <param name="line">The 0-indexed line for which to show the declaration.</param>
            /// <param name="column">The 0-indexed column for which to show the declaration.</param>
            /// <param name="contextWord">The word at the given <paramref name="line"/> and <paramref name="column"/>.</param>
            private void ShowDeclaration(int line, int column, string contextWord) =>
                ShowLocationsAsync(lspHandler.Declaration(path, line, column), "Declarations", contextWord).Forget();

            /// <summary>
            /// Shows the definition for the given <paramref name="line"/> and <paramref name="column"/>.
            /// </summary>
            /// <param name="line">The 0-indexed line for which to show the definition.</param>
            /// <param name="column">The 0-indexed column for which to show the definition.</param>
            /// <param name="contextWord">The word at the given <paramref name="line"/> and <paramref name="column"/>.</param>
            public void ShowDefinition(int line, int column, string contextWord) =>
                ShowLocationsAsync(lspHandler.Definition(path, line, column), "Definitions", contextWord).Forget();

            /// <summary>
            /// Shows the implementation for the given <paramref name="line"/> and <paramref name="column"/>.
            /// </summary>
            /// <param name="line">The 0-indexed line for which to show the implementation.</param>
            /// <param name="column">The 0-indexed column for which to show the implementation.</param>
            /// <param name="contextWord">The word at the given <paramref name="line"/> and <paramref name="column"/>.</param>
            private void ShowImplementation(int line, int column, string contextWord) =>
                ShowLocationsAsync(lspHandler.Implementation(path, line, column), "Implementations", contextWord).Forget();

            /// <summary>
            /// Shows the type definition for the given <paramref name="line"/> and <paramref name="column"/>.
            /// </summary>
            /// <param name="line">The 0-indexed line for which to show the type definition.</param>
            /// <param name="column">The 0-indexed column for which to show the type definition.</param>
            /// <param name="contextWord">The word at the given <paramref name="line"/> and <paramref name="column"/>.</param>
            private void ShowTypeDefinition(int line, int column, string contextWord) =>
                ShowLocationsAsync(lspHandler.TypeDefinition(path, line, column), "Type Definitions", contextWord).Forget();

            /// <summary>
            /// Opens a menu for the given <paramref name="locations"/> with the given <paramref name="name"/>,
            /// letting the user choose one of the locations to navigate to.
            /// </summary>
            /// <param name="locations">The locations to show in the menu.</param>
            /// <param name="name">The name of the locations, e.g. "Definitions".</param>
            /// <param name="contextWord">The word for which the locations are shown.</param>
            private async UniTask ShowLocationsAsync(IUniTaskAsyncEnumerable<LocationOrLocationLink> locations, string name, string contextWord)
            {
                IList<MenuEntry> entries = await MenuEntriesForLocationsAsync(locations.Select(DeconstructLocation), name, contextWord);
                ShowEntries(entries, name, contextWord);
            }

            /// <summary>
            /// Deconstructs the given <paramref name="location"/> into a URI, range, and title.
            /// </summary>
            /// <param name="location">The location to deconstruct.</param>
            /// <returns>A tuple containing the URI, range, and title of the location.</returns>
            private static (Uri, Range, string) DeconstructLocation(LocationOrLocationLink location)
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

            /// <summary>
            /// Generates menu entries for the given <paramref name="locations"/>.
            /// Clicking on an entry will invoke <see cref="OpenSelection"/>.
            /// </summary>
            /// <param name="locations">The locations to generate menu entries for.</param>
            /// <param name="name">The name of the locations, e.g. "Definitions".</param>
            /// <param name="contextWord">The word for which the locations are shown.</param>
            /// <returns>The generated menu entries.</returns>
            private async UniTask<IList<MenuEntry>> MenuEntriesForLocationsAsync(IUniTaskAsyncEnumerable<(Uri, Range, string)> locations, string name, string contextWord)
            {
                IList<MenuEntry> entries = new List<MenuEntry>();
                using (LoadingSpinner.ShowIndeterminate($"Loading {name} for \"{contextWord}\"..."))
                {
                    await foreach ((Uri targetUri, Range targetRange, string title) in locations)
                    {
                        Uri uri = targetUri;
                        if (lspHandler.ProjectUri?.IsBaseOf(uri) ?? false)
                        {
                            // Truncate path above the project's base path to make the result more readable.
                            uri = lspHandler.ProjectUri.MakeRelativeUri(uri);
                        }
                        entries.Add(new(SelectAction: () => OpenSelection(targetUri, targetRange),
                                        Title: title ?? $"{uri}: {targetRange}",
                                        Icon: Icons.Crosshairs,
                                        EntryColor: new Color(0.051f, 0.3608f, 0.1333f)));
                    }
                    await UniTask.SwitchToMainThread();
                }
                return entries;
            }

            /// <summary>
            /// Opens a menu with the given <paramref name="entries"/>.
            /// If there are no entries, a notification is shown informing the user that there are no results.
            /// If there is only one entry, it is directly opened.
            /// </summary>
            /// <param name="entries">The entries to show in the menu.</param>
            /// <param name="name">The name of the entries, e.g. "Definitions".</param>
            /// <param name="contextWord">The word for which the entries are shown.</param>
            private void ShowEntries(IList<MenuEntry> entries, string name, string contextWord)
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
        }

        /// <summary>
        /// Opens the selection at the given <paramref name="uri"/> and <paramref name="range"/>.
        /// If the URI is the same as the current file, the code window is scrolled to the range,
        /// otherwise a new code window is opened.
        /// </summary>
        /// <param name="uri">The URI of the file to open.</param>
        /// <param name="range">The range to scroll to or show in the new code window.</param>
        private void OpenSelection(Uri uri, Range range)
        {
            // When we're going somewhere else, we should deactivate the current tooltip first.
            Tooltip.Deactivate();
            if (FilePath == uri.LocalPath)
            {
                // If this is the current file, we can just scroll to the range.
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

    }
}
