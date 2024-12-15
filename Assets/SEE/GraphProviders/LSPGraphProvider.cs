using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Game.City;
using SEE.Tools.LSP;
using SEE.UI;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils;
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
    public class LSPGraphProvider : SingleGraphProvider
    {
        /// <summary>
        /// The path to the software project for which the graph shall be generated.
        /// </summary>
        [Tooltip("Root path of the project to be analyzed."), RuntimeTab(GraphProviderFoldoutGroup), HideReferenceObjectPicker]
        public DataPath ProjectPath = new();

        /// <summary>
        /// The name of the language server to be used for the analysis.
        /// </summary>
        [Tooltip("The language server to be used for the analysis."),
         LabelText("Language Server"),
         OnValueChanged(nameof(SetLSPServerPath)),
         RuntimeTab(GraphProviderFoldoutGroup),
         ValueDropdown(nameof(ServerDropdown), ExpandAllMenuItems = false)]
        public string ServerName = $"{LSPLanguage.Python}/{LSPServer.Pyright}";

        /// <summary>
        /// The path to the language server executable.
        /// </summary>
        [Tooltip("The path to the language server executable."),
         ValidateInput(nameof(ValidServerExecutableMessage)),
         InlineButton(nameof(SetLSPServerPath), "\u21ba"),
         GUIColor(nameof(ExecutablePathColor)),
         FolderPath(AbsolutePath = true)]
        public string ServerPath;

        /// <summary>
        /// The paths within the project (recursively) containing the source code to be analyzed.
        /// </summary>
        [Tooltip("The paths within the project (recursively) containing the source code to be analyzed."),
         FolderPath(AbsolutePath = true, ParentFolder = "@" + nameof(ProjectPath) + ".Path"),
         InfoBox("If no source paths are specified, all directories within the project path "
                 + "containing source code will be considered.",
                 InfoMessageType.Info, "@" + nameof(SourcePaths) + ".Count == 0"),
         ValidateInput(nameof(ValidSourcePaths), "The source paths must be within the project path."),
         RuntimeTab(GraphProviderFoldoutGroup)]
        public List<string> SourcePaths = new();

        /// <summary>
        /// The paths within the project whose contents should be excluded from the analysis.
        /// </summary>
        [Tooltip("The paths within the project whose contents should be excluded from the analysis. "
             + "Inputs ending with $ will be considered as regular expressions."),
         FolderPath(AbsolutePath = true, ParentFolder = "@" + nameof(ProjectPath) + ".Path"),
         RuntimeTab(GraphProviderFoldoutGroup)]
        public List<string> ExcludedSourcePaths = new();

        // By default, all edge types are included, except for <see cref="EdgeKind.Definition"/>
        // and <see cref="EdgeKind.Declaration"/>, since nodes would otherwise often get a self-reference.
        [Title("Edge types"), Tooltip("The edge types to be included in the graph."), HideLabel]
        [EnumToggleButtons, FoldoutGroup("Import Settings")]
        public EdgeKind IncludedEdgeTypes = EdgeKind.All & ~(EdgeKind.Definition | EdgeKind.Declaration);

        /// <summary>
        /// If true, self-references will be avoided in the graph.
        /// </summary>
        [Tooltip("If true, self-references will be avoided in the graph."), FoldoutGroup("Import Settings")]
        [LabelWidth(200)]
        public bool AvoidSelfReferences = true;

        /// <summary>
        /// If true, references from a node to its direct parent will be avoided in the graph.
        /// </summary>
        [Tooltip("If true, references from a node to its direct parent will be avoided in the graph.")]
        [FoldoutGroup("Import Settings"), LabelWidth(200)]
        public bool AvoidParentReferences = true;

        /// <summary>
        /// The node types to be included in the graph.
        /// </summary>
        [Title("Node types"), Tooltip("The node types to be included in the graph."), HideLabel]
        [EnumToggleButtons, FoldoutGroup("Import Settings")]
        public NodeKind IncludedNodeTypes = NodeKind.All;

        /// <summary>
        /// The diagnostic levels to be included.
        /// </summary>
        [Title("Diagnostic levels"), Tooltip("The diagnostic levels to be included."), HideLabel]
        [EnumToggleButtons, FoldoutGroup("Import Settings")]
        public DiagnosticKind IncludedDiagnosticLevels = DiagnosticKind.All;

        /// <summary>
        /// If true, LSP functionality will be available in code windows.
        /// </summary>
        [Tooltip("If true, LSP functionality will be available in code windows."), RuntimeTab(GraphProviderFoldoutGroup)]
        [LabelWidth(150)]
        public bool UseInCodeWindows = true;

        /// <summary>
        /// If true, the communication between the language server and SEE will be logged.
        /// </summary>
        [Tooltip("If true, the communication between the language server and SEE will be logged."), RuntimeTab(GraphProviderFoldoutGroup)]
        [InfoBox("@\"Logfiles can be found in \" + System.IO.Path.GetTempPath() + "
                 + "\" under inputLogLsp.txt and outputLogLsp.txt\"", InfoMessageType.Info, nameof(LogLSP))]
        public bool LogLSP;

        /// <summary>
        /// The maximum time to wait for the language server to respond.
        /// </summary>
        [LabelText("Timeout (seconds)")]
        [Tooltip("The maximum time to wait for the language server to respond."
             + " Responses after the timeout will not be considered."), RuntimeTab(GraphProviderFoldoutGroup)]
        [InfoBox("No timeout will be applied.", InfoMessageType.Info, "@" + nameof(Timeout) + " == 0")]
        [Range(0, 10), Unit(Units.Second)]
        public double Timeout = 2;

        /// <summary>
        /// The language server to be used for the analysis.
        /// </summary>
        private LSPServer Server => LSPServer.GetByName(ServerName.Split('/')[1]);

        /// <summary>
        /// Returns whether all source paths are within the project path.
        /// </summary>
        private bool ValidSourcePaths => SourcePaths.All(path => path.StartsWith(ProjectPath.Path));

        /// <summary>
        /// Returns the available language servers as a dropdown list, grouped by language.
        /// </summary>
        /// <returns>The available language servers as a dropdown list.</returns>
        private IEnumerable<string> ServerDropdown()
        {
            return LSPLanguage.AllLspLanguages.Select(language => (language, LSPServer.All.Where(server => server.Languages.Contains(language))))
                              .SelectMany(pair => pair.Item2.Select(server => $"{pair.language}/{server}"))
                              .OrderBy(server => server);
        }

        /// <summary>
        /// Returns the color for the <see cref="ServerPath"/> field.
        /// </summary>
        /// <returns>The color for the <see cref="ServerPath"/> field.</returns>
        private Color ExecutablePathColor()
        {
            string message = "";
            if (ValidServerExecutableMessage(ServerPath, ref message))
            {
                return Color.green.Lighter();
            }
            else
            {
                return Color.red.Lighter().Lighter();
            }
        }

        /// <summary>
        /// Returns whether the server executable at the given <paramref name="executablePath"/> is valid,
        /// and sets the <paramref name="errorMessage"/> if it is not.
        /// </summary>
        /// <param name="executablePath">The path to the server executable.</param>
        /// <param name="errorMessage">The error message to be set if the server executable is invalid.</param>
        /// <returns>Whether the server executable is valid.</returns>
        private bool ValidServerExecutableMessage(string executablePath, ref string errorMessage)
        {
            if (string.IsNullOrEmpty(executablePath))
            {
                errorMessage = $"Executable {Server.ServerExecutable} not found. "
                    + $"See {Server.WebsiteURL} for installation instructions.";
                return false;
            }
            if (!File.Exists(executablePath))
            {
                errorMessage = $"Executable {Server.ServerExecutable} not accessible at this location. "
                    + $"See {Server.WebsiteURL} for installation instructions.";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the path to the executable of the language server.
        /// </summary>
        /// <returns>The path to the executable of the language server.</returns>
        private string GetExecutablePath()
        {
            string executableName = Server.ServerExecutable;
            if (File.Exists(executableName))
            {
                return Path.GetFullPath(executableName);
            }

            string[] paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
            return paths.Select(path => Path.Combine(path, executableName)).FirstOrDefault(File.Exists);
        }

        /// <summary>
        /// Sets the <see cref="ServerPath"/> to the default path of the language server executable.
        /// </summary>
        private void SetLSPServerPath() => ServerPath = GetExecutablePath();

        public override SingleGraphProviderKind GetKind()
        {
            return SingleGraphProviderKind.LSP;
        }

        public override async UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city,
                                                          Action<float> changePercentage = null,
                                                          CancellationToken token = default)
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

            if (city.gameObject.TryGetComponent(out LSPHandler oldHandler))
            {
                // We need to shut down the old handler before we can create a new one.
                if (!Application.isPlaying)
                {
                    using (LoadingSpinner.ShowIndeterminate("Shutting down old LSP handler..."))
                    {
                        await oldHandler.ShutdownAsync(token);
                    }
                }
                Destroyer.Destroy(oldHandler);
            }
            // Start with a small value to indicate that the process has started.
            changePercentage?.Invoke(float.Epsilon);

            LSPHandler handler = city.gameObject.AddComponent<LSPHandler>();
            handler.enabled = true;
            handler.Server = Server;
            handler.ProjectPath = ProjectPath.Path;
            handler.LogLSP = LogLSP;
            handler.UseInCodeWindows = UseInCodeWindows;
            handler.TimeoutSpan = TimeSpan.FromSeconds(Timeout);
            await handler.InitializeAsync(executablePath: ServerPath ?? Server.ServerExecutable, token);
            if (token.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }
            changePercentage?.Invoke(0.0001f);

            if (SourcePaths.Count == 0)
            {
                SourcePaths = new List<string> { ProjectPath.Path };
            }

            try
            {
                LSPImporter importer = new(handler, SourcePaths, ExcludedSourcePaths ?? new(),
                                           IncludedNodeTypes, IncludedDiagnosticLevels,
                                           IncludedEdgeTypes, AvoidSelfReferences, AvoidParentReferences);
                using (LoadingSpinner.ShowDeterminate("Creating graph using LSP...", out Action<float> updateProgress))
                {
                    await importer.LoadAsync(graph, x =>
                    {
                        changePercentage?.Invoke(x);
                        updateProgress(x);
                    }, token);
                }
            }
            catch (TimeoutException)
            {
                string message = "The language server did not respond in time.";
                if (LogLSP)
                {
                    message += $" Check the output log at {Path.GetTempPath()}outputLogLsp.txt";
                }
                else
                {
                    message += " Enable logging in the graph provider to see what went wrong.";
                }
                Debug.LogError(message + "\n");
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }

            // We shut down the LSP server for now. If it is needed again, it can still be restarted.
            if (Application.isPlaying)
            {
                handler.enabled = false;
            }
            else
            {
                handler.ShutdownAsync().Forget();
            }
            return graph;
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
            writer.Save(ServerName, serverLabel);
            writer.Save(LogLSP, logLSPLabel);
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            ProjectPath.Restore(attributes, pathLabel);
            ServerName = (string)attributes[serverLabel];
            LogLSP = (bool)attributes[logLSPLabel];
        }

        #endregion
    }
}
