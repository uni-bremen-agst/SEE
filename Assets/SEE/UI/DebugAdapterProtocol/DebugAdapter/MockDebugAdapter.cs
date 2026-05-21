using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using SEE.UI.PropertyDialog;
using SimpleFileBrowser;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SEE.UI.DebugAdapterProtocol.DebugAdapter
{
    /// <summary>
    /// Configuration for the mock debug adapter.
    /// <para>
    /// Repository: <seealso href="https://github.com/microsoft/vscode-mock-debug"/><br/>
    /// Properties: <seealso href="https://github.com/microsoft/vscode-mock-debug/blob/main/package.json#L146"/>
    /// </para>
    /// </summary>
    public class MockDebugAdapter : DebugAdapter
    {
        #region Adapter Configuration
        /// <summary>
        /// The name.
        /// </summary>
        public override string Name => "Mock";

        /// <summary>
        /// The working directory of the debug adapter.
        /// </summary>
        public override string AdapterWorkingDirectory { get; set; } = Path.Combine(AdapterDirectory, "vscode-mock-debug", "out");
        /// <summary>
        /// The executable (file name) of the debug adapter.
        /// </summary>
        public override string AdapterFileName { get; set; } = "node";

        /// <summary>
        /// The arguments of the debug adapter.
        /// </summary>
        public override string AdapterArguments { get; set; } = "debugAdapter.js";
        #endregion

        #region Launch Configuration
        /// <summary>
        /// The noDebug property.
        /// </summary>
        private bool launchNoDebug;

        /// <summary>
        /// The program property.
        /// </summary>
        private string launchProgram = Path.Combine(AdapterDirectory, "vscode-mock-debug", "sampleWorkspace", "readme.md");

        /// <summary>
        /// The stopOnEntry property.
        /// </summary>
        private bool launchStopOnEntry = true;

        /// <summary>
        /// The trace property.
        /// </summary>
        private bool launchTrace = false;

        /// <summary>
        /// The compileError property.
        /// </summary>
        private bool launchCompileError = false;

        /// <summary>
        /// The input field for the noDebug property.
        /// </summary>
        private BooleanProperty launchNoDebugProperty;

        /// <summary>
        /// The input field for the program property.
        /// </summary>
        private FilePathProperty launchProgramProperty;

        /// <summary>
        /// The input field for the stopOnEntry property.
        /// </summary>
        private BooleanProperty launchStopOnEntryProperty;

        /// <summary>
        /// The input field for the trace property.
        /// </summary>
        private BooleanProperty launchTraceProperty;

        /// <summary>
        /// The input field for the compileError property.
        /// </summary>
        private BooleanProperty launchCompileErrorProperty;
        #endregion

        #region Adapter Methods
        public override void SetupLaunchConfig(GameObject go, PropertyGroup group)
        {
            launchProgramProperty = go.AddComponent<FilePathProperty>();
            launchProgramProperty.Name = "Program";
            launchProgramProperty.Filters = new[] { new FileBrowser.Filter("Markdown", ".md", ".markdown") };
            launchProgramProperty.Value = launchProgram;
            launchProgramProperty.Description = "Absolute path to a text file.";
            group.AddProperty(launchProgramProperty);

            launchNoDebugProperty = go.AddComponent<BooleanProperty>();
            launchNoDebugProperty.Name = "No Debug";
            launchNoDebugProperty.Description = "Whether the program should be launched without debugging.";
            launchNoDebugProperty.Value = launchNoDebug;
            group.AddProperty(launchNoDebugProperty);

            launchStopOnEntryProperty = go.AddComponent<BooleanProperty>();
            launchStopOnEntryProperty.Name = "Stop on Entry";
            launchStopOnEntryProperty.Description = "Automatically stop on entry.";
            launchStopOnEntryProperty.Value = launchStopOnEntry;
            group.AddProperty(launchStopOnEntryProperty);

            launchTraceProperty = go.AddComponent<BooleanProperty>();
            launchTraceProperty.Name = "Trace";
            launchTraceProperty.Description = "Enable logging of the Debug Adapter Protocol.";
            launchTraceProperty.Value = launchTrace;
            group.AddProperty(launchTraceProperty);

            launchCompileErrorProperty = go.AddComponent<BooleanProperty>();
            launchCompileErrorProperty.Name = "Compile Error";
            launchCompileErrorProperty.Description = "Simulates a compile error in 'launch' request.";
            launchCompileErrorProperty.Value = launchCompileError;
            group.AddProperty(launchCompileErrorProperty);
        }

        public override void SaveLaunchConfig()
        {
            launchProgram = launchProgramProperty.Value;
            launchStopOnEntry = launchStopOnEntryProperty.Value;
            launchTrace = launchTraceProperty.Value;
            launchCompileError = launchCompileErrorProperty.Value;
        }

        public override LaunchRequest GetLaunchRequest()
        {
            return new()
            {
                NoDebug = launchNoDebug,
                ConfigurationProperties = new()
                {
                    {"program", launchProgram},
                    {"stopOnEntry", launchStopOnEntry },
                    {"trace", launchTrace },
                    {"compileError", launchCompileError ? "show" : null },
                }
            };
        }
        #endregion
    }
}
