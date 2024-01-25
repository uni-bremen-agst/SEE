using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using SEE.UI.PropertyDialog;
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
        private string launchCompileError = "default"; // default, show, hide

        private StringProperty launchProgramProperty;

        public override void SetupLaunchConfig(GameObject go, PropertyGroup group)
        {
            launchProgramProperty = go.AddComponent<StringProperty>();
            launchProgramProperty.Name = "Program";
            launchProgramProperty.Value = launchProgram;
            launchProgramProperty.Description = "Absolute path to a text file.";
            group.AddProperty(launchProgramProperty);
        }

        public override void SaveLaunchConfig()
        {
            launchProgram = launchProgramProperty.Value;
        }

        public override LaunchRequest GetLaunchRequest()
        {
            return new()
            {

            };
        }
    }
}