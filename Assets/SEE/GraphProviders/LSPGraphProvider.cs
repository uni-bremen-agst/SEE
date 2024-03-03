using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.Tools.LSP;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SEE.GraphProviders
{
    /// <summary>
    /// A graph provider that uses a language server to create a graph.
    /// </summary>
    public class LSPGraphProvider : GraphProvider
    {
        /// <summary>
        /// The path to the software project for which the graph shall be generated.
        /// </summary>
        [Tooltip("Path to the project to be analyzed."), RuntimeTab(GraphProviderFoldoutGroup), HideReferenceObjectPicker]
        public DirectoryPath ProjectPath = new();

        /// <summary>
        /// The language server to be used for the analysis.
        /// </summary>
        [Tooltip("The language server to be used for the analysis."),
         RuntimeTab(GraphProviderFoldoutGroup),
         HideReferenceObjectPicker, ValueDropdown(nameof(ServerDropdown), ExpandAllMenuItems = true)]
        public LSPServer Server = LSPServer.Pyright;

        /// <summary>
        /// If true, the communication between the language server and SEE will be logged.
        /// </summary>
        [Tooltip("If true, the communication between the language server and SEE will be logged."), RuntimeTab(GraphProviderFoldoutGroup)]
        [InfoBox("@\"Logfiles can be found in \" + System.IO.Path.GetTempPath()", InfoMessageType.Info, nameof(LogLSP))]
        public bool LogLSP;

        /// <summary>
        /// Returns the available language servers as a dropdown list, grouped by language.
        /// </summary>
        /// <returns>The available language servers as a dropdown list.</returns>
        private IEnumerable<ValueDropdownItem<LSPServer>> ServerDropdown()
        {
            return LSPLanguage.All.Select(language => (language, LSPServer.All.Where(server => server.Languages.Contains(language))))
                              .SelectMany(pair => pair.Item2.Select(server => new ValueDropdownItem<LSPServer>($"{pair.language.Name}/{server.Name}", server)));
        }

        public override GraphProviderKind GetKind()
        {
            return GraphProviderKind.LSP;
        }

        public override async UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city)
        {
            if (string.IsNullOrEmpty(ProjectPath.Path))
            {
                throw new ArgumentException("Empty project path.\n");
            }
            if (!Directory.Exists(ProjectPath.Path))
            {
                throw new ArgumentException($"Directory {ProjectPath.Path} does not exist.\n");
            }
            if (city == null)
            {
                throw new ArgumentException("The given city is null.\n");
            }

            LSPHandler handler = city.gameObject.AddOrGetComponent<LSPHandler>();
            handler.enabled = true;
            handler.Server = Server;
            handler.ProjectPath = ProjectPath.Path;
            handler.LogLSP = LogLSP;
            if (Application.isPlaying)
            {
                await handler.WaitUntilReadyAsync();
            }
            else
            {
                // Since OnEnable is not called in the editor, we have to initialize the handler manually.
                await handler.InitializeAsync();
            }

            SymbolInformationOrDocumentSymbolContainer result = await handler.Client.RequestDocumentSymbol(new DocumentSymbolParams
            {
                // TODO: Use root path to query all relevant filetypes.
                TextDocument = new TextDocumentIdentifier(Path.Combine(ProjectPath.Path, "src/token/mod.rs"))
            });
            foreach (SymbolInformationOrDocumentSymbol symbol in result)
            {
                if (symbol.IsDocumentSymbolInformation)
                {
                    Debug.LogError("This language server emits SymbolInformation, which is deprecated and not "
                                   + "supported by SEE. Please choose a language server that is capable of returning "
                                   + "hierarchic DocumentSymbols.\n");
                    break;
                }

                // TODO: Use algorithm 1 from master's thesis.
            }

            // We shut down the LSP server for now. If it is needed again, it can still be restarted.
            if (Application.isPlaying)
            {
                handler.enabled = false;
            }
            else
            {
                await handler.ShutdownAsync();
            }
            return null;
        }


        #region Config I/O

        /// <summary>
        /// The label for <see cref="ProjectPath"/> in the configuration file.
        /// </summary>
        private const string pathLabel = "path";

        /// <summary>
        /// The label for <see cref="Server"/> in the configuration file.
        /// </summary>
        private const string serverLabel = "server";

        /// <summary>
        /// The label for <see cref="LogLSP"/> in the configuration file.
        /// </summary>
        private const string logLSPLabel = "logLSP";

        protected override void SaveAttributes(ConfigWriter writer)
        {
            ProjectPath.Save(writer, pathLabel);
            writer.Save(Server.Name, serverLabel);
            writer.Save(LogLSP, logLSPLabel);
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            ProjectPath.Restore(attributes, pathLabel);
            Server = LSPServer.GetByName((string)attributes[serverLabel]);
            LogLSP = (bool)attributes[logLSPLabel];
        }

        #endregion
    }
}
