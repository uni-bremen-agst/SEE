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
    /// <summary>
    /// Represents the NetCore debug adapter.
    /// <para>
    /// Repository: <seealso href="https://github.com/Samsung/netcoredbg"/><br/>
    /// Properties: https://github.com/Samsung/netcoredbg/blob/master/test-suite/NetcoreDbgTest/VSCode/VSCodeProtocolRequest.cs#L44
    /// </para>
    /// </summary>
    public class NetCoreDebugAdapter : DebugAdapter
    {
        #region Adapter Configuration
        public override string Name => "Netcore";

        public override string AdapterWorkingDirectory { get; set; } = Path.Combine(AdapterDirectory, "netcoredbg");

        public override string AdapterFileName { get; set; } = "netcoredbg.exe";

        public override string AdapterArguments { get; set; } = "--interpreter=vscode";

        #endregion

        #region Launch Configuration
        /// <summary>
        /// The <c>noDebug</c> property.
        /// </summary>
        private bool launchNoDebug = false;


        /// <summary>
        /// The <c>name</c> property.
        /// </summary>
        private string launchName = ".NET Core Launch";


        /// <summary>
        /// The <c>type</c> property.
        /// </summary>
        private string launchType = "coreclr";


        /// <summary>
        /// The <c>preLaunchTask</c> property.
        /// </summary>
        private string launchPreLaunchTask = "build";


        /// <summary>
        /// The <c>program</c> property.
        /// </summary>
        private string launchProgram = "program.dll";

        /// <summary>
        /// The <c>args</c> property.
        /// </summary>
        private List<string> launchArgs = new();

        /// <summary>
        /// The <c>cwd</c> property.
        /// </summary>
        private string launchCwd = Path.Combine("C:\\", "path", "containing", "dll");

        /// <summary>
        /// The <c>env</c> property.
        /// </summary>
        private Dictionary<string, string> launchEnv = new();

        /// <summary>
        /// The <c>console</c> property.
        /// </summary>
        private string launchConsole = null;

        /// <summary>
        /// The <c>stopAtEntry</c> property.
        /// </summary>
        private bool launchStopAtEntry = true;

        /// <summary>
        /// The <c>justMyCode</c> property.
        /// </summary>
        private bool launchJustMyCode = false;

        /// <summary>
        /// The <c>enableStepFiltering</c> property.
        /// </summary>
        private bool launchEnableStepFiltering = true;

        /// <summary>
        /// The <c>internalConsoleOptions</c> property.
        /// </summary>
        private string launchInternalConsoleOptions = null;

        /// <summary>
        /// The <c>__sessionId</c> property.
        /// </summary>
        private string launchSessionId = Guid.NewGuid().ToString();

        /// <summary>
        /// The input field for the <c>cwd</c> property.
        /// </summary>
        private FilePathProperty launchCwdProperty;

        /// <summary>
        /// The input field for the <c>program</c> property.
        /// </summary>
        private FilePathProperty launchProgramProperty;

        /// <summary>
        /// The input field for the <c>args</c> property.
        /// </summary>
        private StringProperty launchArgsProperty;

        /// <summary>
        /// The input field for the <c>stopAtEntry</c> property.
        /// </summary>
        private BooleanProperty launchStopAtEntryProperty;

        #endregion

        #region Adapter Methods

        public override void SetupLaunchConfig(GameObject go, PropertyGroup group)
        {
            launchCwdProperty = go.AddComponent<FilePathProperty>();
            launchCwdProperty.Name = "Cwd";
            launchCwdProperty.Description = "The working directory.";
            launchCwdProperty.PickMode = FileBrowser.PickMode.Folders;
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
            launchArgsProperty.Value = escapeList(launchArgs);
            group.AddProperty(launchArgsProperty);

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

        #endregion

        #region Utilities

        /// <summary>
        /// Escapes a list as a string.
        /// <seealso cref="parseList(string)"/>
        /// </summary>
        /// <param name="args">The list.</param>
        /// <returns>The escaped list.</returns>
        private static string escapeList(List<string> args)
        {
            if (args.Count == 0)
            {
                return "";
            }
            return "\"" + string.Join("\", \"",
                args.Select(arg => arg.Replace("\\", "\\\\").Replace("\"", "\\\""))) + "\"";
        }

        /// <summary>
        /// Parses a string to a list.
        /// <seealso cref="escapeList(List{string})"/>
        /// </summary>
        /// <param name="text">The string.</param>
        /// <returns>The parsed list.</returns>
        private static List<string> parseList(string text)
        {
            if (text.Length == 0)
            {
                return new();
            }

            List<string> arguments = new();
            StringBuilder currentArgument = new();
            bool inString = false;
            for (int i=0; i < text.Length; i++)
            {
                char c = text[i];
                // quotations marks mark the start and end of each argument
                if (c == '"')
                {
                    if (!inString)
                    {
                        currentArgument = new();
                    }
                    else
                    {
                        arguments.Add(currentArgument.ToString());
                    }
                    inString = !inString;
                }
                else if (inString)
                {
                    // backslash escapes the next character (allows quotation marks)
                    if (c == '\\')
                    {
                        i += 1;
                        c = text[i];
                    }
                    currentArgument.Append(c);
                }
                else
                {
                    // characters outside of quotations marks are ignored
                }
            }
            return arguments;
        }
        #endregion
    }
}
