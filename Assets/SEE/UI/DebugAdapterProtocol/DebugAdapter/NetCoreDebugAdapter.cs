using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Newtonsoft.Json.Linq;
using SEE.UI.PropertyDialog;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SEE.UI.DebugAdapterProtocol.DebugAdapter
{
    public class NetCoreDebugAdapter : DebugAdapter
    {
        public override string Name => "Netcoredbg";

        public override string AdapterWorkingDirectory { get; set; } = Path.Combine(AdapterDirectory, "netcoredbg");
        public override string AdapterFileName { get; set; } = "netcoredbg.exe";
        public override string AdapterArguments { get; set; } = "--interpreter=vscode";

        private string launchName = ".NET Core Launch";
        private string launchType = "coreclr";
        private string launchPreLaunchTask = "build";
        private string launchProgram = "program.dll";
        private List<string> launchArgs = new();
        private string launchCwd = "";
        private Dictionary<string, string> launchEnv = new();
        private string launchConsole = "internalConsole";
        private bool launchStopAtEntry = true;
        private bool launchJustMyCode = false;
        private bool launchEnableStepFiltering = true;
        private string launchInternalConsoleOptions = "openOnSessionStart";
        private string launchSessionId = Guid.NewGuid().ToString();

        private StringProperty launchProgramProperty;
        private StringProperty launchCwdProperty;
        private SelectionProperty launchStopAtEntryProperty;

        public override void SetupLaunchConfig(GameObject go, PropertyGroup group)
        {
            launchProgramProperty = go.AddComponent<StringProperty>();
            launchProgramProperty.Name = "Program";
            launchProgramProperty.Value = launchProgram;
            group.AddProperty(launchProgramProperty);

            launchCwdProperty = go.AddComponent<StringProperty>();
            launchCwdProperty.Name = "Cwd";
            launchCwdProperty.Value = launchCwd;
            group.AddProperty(launchCwdProperty);

            launchStopAtEntryProperty = go.AddComponent<SelectionProperty>();
            launchStopAtEntryProperty.Name = "Stop at Entry";
            launchStopAtEntryProperty.AddOptions(new[]{ "Stop at entry", "Don't stop at entry"});
            launchStopAtEntryProperty.Value = launchStopAtEntry ? "Stop at entry" : "Don't stop at entry";
            group.AddProperty(launchStopAtEntryProperty);

        }

        public override void SaveLaunchConfig()
        {
            launchProgram = launchProgramProperty.Value;
            launchCwd = launchCwdProperty.Value;
            launchStopAtEntry = launchStopAtEntryProperty.Value == "true";
        }

        
        public override LaunchRequest GetLaunchRequest()
        {
            // https://github.com/Samsung/netcoredbg/blob/27606c317017beb81bc1b81846cdc460a7a6aed3/test-suite/NetcoreDbgTest/VSCode/VSCodeProtocolRequest.cs#L44
            return new()
            {
                ConfigurationProperties = new Dictionary<string, JToken>
                {
                    { "launchName", launchName },
                    { "launchType", launchType },
                    { "launchPreLaunchTask", launchPreLaunchTask },
                    { "launchProgram", launchProgram },
                    { "launchArgs", JToken.FromObject(launchArgs) },
                    { "launchCwd", launchCwd },
                    { "launchEnv", JToken.FromObject(launchEnv) },
                    { "launchConsole", launchConsole },
                    { "launchStopAtEntry", launchStopAtEntry },
                    { "launchJustMyCode", launchJustMyCode },
                    { "launchEnableStepFiltering", launchEnableStepFiltering },
                    { "launchInternalConsoleOptions", launchInternalConsoleOptions },
                    { "__sessionId", launchSessionId },
                }
            };
        }
    }
}