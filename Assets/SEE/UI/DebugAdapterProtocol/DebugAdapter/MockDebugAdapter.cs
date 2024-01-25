using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using SEE.UI.PropertyDialog;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SEE.UI.DebugAdapterProtocol.DebugAdapter
{
    public class MockDebugAdapter : DebugAdapter
    {

        public override string Name => "Mock";

        public override string AdapterWorkingDirectory { get; set; } = Path.Combine(AdapterDirectory, "vscode-mock-debug", "out");
        public override string AdapterFileName { get; set; } = "node";
        public override string AdapterArguments { get; set; } = "debugAdapter.js";

        // https://github.com/microsoft/vscode-mock-debug/blob/b8d3d3a436e94f73ca193bf0bfa7c5c416a8aa8c/package.json#L146
        private string launchProgram = Path.Combine(AdapterDirectory, "vscode-mock-debug", "sampleWorkspace", "readme.md");
        private bool launchStopOnEntry = true;
        private bool launchTrace = true;
        private string launchCompileError = launchCompileErrorOptions[0];

        private StringProperty launchProgramProperty;
        private SelectionProperty launchStopOnEntryProperty;
        private SelectionProperty launchTraceProperty;
        private SelectionProperty launchCompileErrorProperty;

        private static readonly string[] launchStopOnEntryOptions = new string[] { "Stop on entry - Enabled", "Stop on entry - Disabled" };
        private static readonly string[] launchTraceOptions = new string[] { "Logging - Enabled", "Logging - Disabled" };
        private static readonly string[] launchCompileErrorOptions = new string[] { "Fake compile error - Show", "Fake compile error - Hide" };

        public override void SetupLaunchConfig(GameObject go, PropertyGroup group)
        {
            launchProgramProperty = go.AddComponent<StringProperty>();
            launchProgramProperty.Name = "Program";
            launchProgramProperty.Value = launchProgram;
            launchProgramProperty.Description = "Absolute path to a text file.";
            group.AddProperty(launchProgramProperty);

            launchStopOnEntryProperty = go.AddComponent<SelectionProperty>();
            launchStopOnEntryProperty.Name = "Stop on Entry";
            launchStopOnEntryProperty.Description = "Automatically stop on entry.";
            launchStopOnEntryProperty.AddOptions(launchStopOnEntryOptions);
            launchStopOnEntryProperty.Value = launchStopOnEntry ? launchStopOnEntryOptions[0] : launchStopOnEntryOptions[1];
            group.AddProperty(launchStopOnEntryProperty);

            launchTraceProperty = go.AddComponent<SelectionProperty>();
            launchTraceProperty.Name = "Trace";
            launchTraceProperty.Description = "Enable logging of the Debug Adapter Protocol.";
            launchTraceProperty.AddOptions(launchTraceOptions);
            launchTraceProperty.Value = launchTrace ? launchTraceOptions[0] : launchTraceOptions[1];
            group.AddProperty(launchTraceProperty);

            launchCompileErrorProperty = go.AddComponent<SelectionProperty>();
            launchCompileErrorProperty.Name = "Compile Error";
            launchCompileErrorProperty.Description = "Simulates a compile error in 'launch' request.";
            launchCompileErrorProperty.AddOptions(launchCompileErrorOptions);
            launchCompileErrorProperty.Value = launchCompileError;
            group.AddProperty(launchCompileErrorProperty);

        }

        public override void SaveLaunchConfig()
        {
            launchProgram = launchProgramProperty.Value;
            launchStopOnEntry = launchStopOnEntryProperty.Value == launchStopOnEntryOptions[0];
            launchTrace = launchTraceProperty.Value == launchTraceOptions[0];
            launchCompileError = launchCompileErrorProperty.Value;
        }

        public override LaunchRequest GetLaunchRequest()
        {
            return new()
            {
                ConfigurationProperties = new()
                {
                    {"program", launchProgram},
                    {"stopOnEntry", launchStopOnEntry },
                    {"trace", launchTrace },
                    {"compileError", launchCompileError == launchCompileErrorOptions[0] ? "show" : "hide"},
                }
            };
        }
    }
}