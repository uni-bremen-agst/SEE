using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Newtonsoft.Json.Linq;
using SEE.UI.PropertyDialog;
using SimpleFileBrowser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SEE.UI.DebugAdapterProtocol.DebugAdapter
{
    public class NetCoreDebugAdapter : DebugAdapter
    {
        public override string Name => "Netcore";

        public override string AdapterWorkingDirectory { get; set; } = Path.Combine(AdapterDirectory, "netcoredbg");
        public override string AdapterFileName { get; set; } = "netcoredbg.exe";
        public override string AdapterArguments { get; set; } = "--interpreter=vscode --engineLogging=RunWithUnity.log";

        private bool launchNoDebug = false;
        // https://github.com/Samsung/netcoredbg/blob/27606c317017beb81bc1b81846cdc460a7a6aed3/test-suite/NetcoreDbgTest/VSCode/VSCodeProtocolRequest.cs#L44
        private string launchName = ".NET Core Launch";
        private string launchType = "coreclr";
        private string launchPreLaunchTask = "build";
        private string launchProgram = "HelloCS2.dll";
        private List<string> launchArgs = new() { "Hello", "World"};
        private string launchCwd = Path.Combine("D:" + Path.DirectorySeparatorChar + "ferdi", "SampleProjects", "HelloCS2", "bin", "Debug", "net8.0");
        private Dictionary<string, string> launchEnv = new();
        private string launchConsole = "internalConsole";
        private bool launchStopAtEntry = true;
        private bool launchJustMyCode = false;
        private bool launchEnableStepFiltering = true;
        private string launchInternalConsoleOptions = "openOnSessionStart";
        private string launchSessionId = Guid.NewGuid().ToString();

        private FilePathProperty launchCwdProperty;
        private FilePathProperty launchProgramProperty;
        private StringProperty launchArgsProperty;
        private BooleanProperty launchStopAtEntryProperty;
        private BooleanProperty launchNoDebugProperty;

        public override void SetupLaunchConfig(GameObject go, PropertyGroup group)
        {
            launchCwdProperty = go.AddComponent<FilePathProperty>();
            launchCwdProperty.Name = "Cwd";
            launchCwdProperty.Description = "The working directory.";
            launchCwdProperty.PickMode = SimpleFileBrowser.FileBrowser.PickMode.Folders;
            launchCwdProperty.Value = launchCwd;
            group.AddProperty(launchCwdProperty);

            launchProgramProperty = go.AddComponent<FilePathProperty>();
            launchProgramProperty.Name = "Program";
            launchProgramProperty.Description = "The absolute path of a dll file.";
            launchProgramProperty.FallbackDirectory = launchCwd;
            launchProgramProperty.Filters = new[] { new FileBrowser.Filter("Dll", ".dll")};
            launchProgramProperty.Value = launchProgram;
            group.AddProperty(launchProgramProperty);

            launchArgsProperty = go.AddComponent<StringProperty>();
            launchArgsProperty.Name = "Args";
            launchArgsProperty.Description = "The program arguments.";
            launchArgsProperty.Value = espaceList(launchArgs);
            group.AddProperty(launchArgsProperty);

            launchNoDebugProperty = go.AddComponent<BooleanProperty>();
            launchNoDebugProperty.Name = "No Debug";
            launchNoDebugProperty.Description = "Launch the program without debugging.";
            launchNoDebugProperty.Value = launchNoDebug;
            group.AddProperty(launchNoDebugProperty);

            launchStopAtEntryProperty = go.AddComponent<BooleanProperty>();
            launchStopAtEntryProperty.Name = "Stop at Entry";
            launchStopAtEntryProperty.Description = "Automatically stops after launch.";
            launchStopAtEntryProperty.Value = launchStopAtEntry;
            group.AddProperty(launchStopAtEntryProperty);
        }

        public override void SaveLaunchConfig()
        {
            launchProgram = launchProgramProperty.Value;
            launchArgs = parseList(launchArgsProperty.Value);
            launchCwd = launchCwdProperty.Value;
            launchNoDebug = launchNoDebugProperty.Value;
            launchStopAtEntry = launchStopAtEntryProperty.Value;
        }

        
        public override LaunchRequest GetLaunchRequest()
        {
            return new()
            {
                NoDebug = launchNoDebug,
                ConfigurationProperties = new Dictionary<string, JToken>
                {
                    { "name", launchName },
                    { "type", launchType },
                    { "preLaunchTask", launchPreLaunchTask },
                    { "program", launchProgram.Replace("${cwd}", launchCwd) },
                    { "args", JToken.FromObject(launchArgs) },
                    { "cwd", launchCwd },
                    { "env", JToken.FromObject(launchEnv) },
                    { "console", launchConsole },
                    { "stopAtEntry", launchStopAtEntry },
                    { "justMyCode", launchJustMyCode },
                    { "enableStepFiltering", launchEnableStepFiltering },
                    { "internalConsoleOptions", launchInternalConsoleOptions },
                    { "__sessionId", launchSessionId },
                }
            };
        }

        /// <summary>
        /// Escapes a list as a string.
        /// <seealso cref="parseList(string)"/>
        /// </summary>
        /// <param name="args">The list.</param>
        /// <returns></returns>
        private static string espaceList(List<string> args)
        {
            if (args.Count == 0) return "";
            return "\"" + String.Join("\", \"", 
                args.Select(arg => arg.Replace("\\", "\\\\").Replace("\"", "\\\""))) + "\"";
        }

        /// <summary>
        /// Parses a string to a list.
        /// <seealso cref="espaceList(List{string})"/>
        /// </summary>
        /// <param name="text">The string.</param>
        /// <returns></returns>
        private static List<string> parseList(string text)
        {
            if (text.Length == 0) return new();

            List<string> arguments = new();
            StringBuilder currentArgument = new();
            bool inString = false;
            for (int i=0; i<text.Length; i++)
            {
                char c = text[i];
                // quotations marks mark the start and end of each argument
                if (c == '"')
                {
                    if (!inString)
                    {
                        currentArgument = new();
                    } else
                    {
                        arguments.Add(currentArgument.ToString());
                    }
                    inString = !inString;
                } else if (inString)
                {
                    // backslash escapes the next character (allows quotation marks)
                    if (c == '\\')
                    {
                        i += 1;
                        c = text[i];
                    }
                    currentArgument.Append(c); 
                } else
                {
                    // characters outside of quotations marks are ignored
                }
            }
            return arguments;
        }
    }
}