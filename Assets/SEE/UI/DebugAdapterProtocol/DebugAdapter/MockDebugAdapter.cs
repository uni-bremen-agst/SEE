using Assets.SEE.UI.PropertyDialog;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using SEE.UI.PropertyDialog;
using SimpleFileBrowser;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SEE.UI.DebugAdapterProtocol.DebugAdapter
{
    /// <summary>
    /// Configration for the mock debug adapter.
    /// <para>
    /// Repository: <seealso href="https://github.com/microsoft/vscode-mock-debug"/><br/>
    /// Properties: <seealso href="https://github.com/microsoft/vscode-mock-debug/blob/main/package.json#L146"/>
    /// </para>
    /// </summary>
    public class MockDebugAdapter : DebugAdapter
    {
        public override string Name => "Mock";

        public override string AdapterWorkingDirectory { get; set; } = Path.Combine(AdapterDirectory, "mock", "out");
        public override string AdapterFileName { get; set; } = "node";
        public override string AdapterArguments { get; set; } = "debugAdapter.js";


        /// <summary>
        /// The <c>noDebug</c> property.
        /// </summary>
        private bool launchNoDebug;
        
        /// <summary>
        /// The <c>program</c> property.
        /// </summary>
        private string launchProgram = Path.Combine(AdapterDirectory, "mock", "sampleWorkspace", "readme.md");
        
        /// <summary>
        /// The <c>stopOnEntry</c> property.
        /// </summary>
        private bool launchStopOnEntry = true;

        /// <summary>
        /// The <c>trace</c> property.
        /// </summary>
        private bool launchTrace = false;

        /// <summary>
        /// The <c>compileError</c> property.
        /// </summary>
        private bool launchCompileError = false;

        /// <summary>
        ///     The input field for the <c>noDebug</c> property.
        /// </summary>
        private BooleanProperty launchNoDebugProperty;

        /// <summary>
        ///     The input field for the <c>program</c> property.
        /// </summary>
        private FilePathProperty launchProgramProperty;

        /// <summary>
        ///     The input field for the <c>stopOnEntry</c> property.
        /// </summary>
        private BooleanProperty launchStopOnEntryProperty;

        /// <summary>
        ///     The input field for the <c>trace</c> property.
        /// </summary>
        private BooleanProperty launchTraceProperty;

        /// <summary>
        ///     The input field for the <c>compileError</c> property.
        /// </summary>
        private BooleanProperty launchCompileErrorProperty;


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
    }
}