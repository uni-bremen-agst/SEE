using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using SEE.UI.PropertyDialog;
using SimpleFileBrowser;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SEE.UI.DebugAdapterProtocol.DebugAdapter
{
    public class MockDebugAdapter : DebugAdapter
    {

        public override string Name => "Mock";

        public override string AdapterWorkingDirectory { get; set; } = Path.Combine(AdapterDirectory, "mock", "out");
        public override string AdapterFileName { get; set; } = "node";
        public override string AdapterArguments { get; set; } = "debugAdapter.js";

        private bool launchNoDebug;
        // https://github.com/microsoft/vscode-mock-debug/blob/b8d3d3a436e94f73ca193bf0bfa7c5c416a8aa8c/package.json#L146
        private string launchProgram = Path.Combine(AdapterDirectory, "mock", "sampleWorkspace", "readme.md");
        private bool launchStopOnEntry = true;
        private bool launchTrace = false;
        private bool launchCompileError = false;

        private BooleanProperty launchNoDebugProperty;
        private FilePathProperty launchProgramProperty;
        private BooleanProperty launchStopOnEntryProperty;
        private BooleanProperty launchTraceProperty;
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