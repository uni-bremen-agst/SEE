using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Newtonsoft.Json.Linq;
using SEE.UI.PropertyDialog;
using SimpleFileBrowser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using UnityEngine;

namespace SEE.UI.DebugAdapterProtocol.DebugAdapter
{
    /// <summary>
    /// Represents the JavaScript debug adapter.
    /// <para>
    /// Repository: <seealso href="https://github.com/microsoft/vscode-js-debug"/>
    /// </para>
    /// </summary>
    public class JavaScriptDebugAdapter : DebugAdapter
    {
        #region Adapter Configuration
        public override string Name => "JavaScript";

        public override string AdapterWorkingDirectory { get; set; } = Path.Combine(AdapterDirectory, "js-debug", "src");
        public override string AdapterFileName { get; set; } = "node";

        public override string AdapterArguments { get; set; } = $"dapDebugServer.js {GetAvailablePort()}";
        #endregion

        #region Launch Configuration
        /// <summary>
        /// The <c>noDebug</c> property.
        /// </summary>
        private bool launchNoDebug = false;

        /// <summary>
        /// The <c>type</c> property.
        /// </summary>
        private string launchType = "pwa-node";

        /// <summary>
        /// The <c>name</c> property.
        /// </summary>
        private string launchName = "Launch file";

        /// <summary>
        /// The <c>cwd</c> property.
        /// </summary>
        private string launchCwd = Path.Combine("C:\\", "path", "containing", "program");

        /// <summary>
        /// The <c>program</c> property.
        /// </summary>
        private string launchProgram = "file.js";

        /// <summary>
        /// The input field for the <c>noDebug</c> property.
        /// </summary>
        private BooleanProperty launchNoDebugProperty;

        /// <summary>
        /// The input field for the <c>cwd</c> property.
        /// </summary>
        private FilePathProperty launchCwdProperty;

        /// <summary>
        /// The input field for the <c>program</c> property.
        /// </summary>
        private FilePathProperty launchProgramProperty;
        #endregion

        #region Adapter Methods
        public override LaunchRequest GetLaunchRequest()
        {
            return new LaunchRequest()
            {
                NoDebug = launchNoDebug,
                ConfigurationProperties = new Dictionary<string, JToken>
                {
                    {"type", launchType },
                    {"name", launchName },
                    {"cwd", launchCwd },
                    {"program", launchProgram }
                }
            };
        }

        public override void SaveLaunchConfig()
        {
            launchNoDebug = launchNoDebugProperty.Value;
            launchCwd = launchCwdProperty.Value;
            launchProgram = launchProgramProperty.Value;
        }

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
            launchProgramProperty.Description = "The path of a javascript file.";
            launchProgramProperty.FallbackDirectory = launchCwd;
            launchProgramProperty.Filters = new[] { new FileBrowser.Filter("JavaScript", ".js") };
            launchProgramProperty.Value = launchProgram;
            group.AddProperty(launchProgramProperty);

            launchNoDebugProperty = go.AddComponent<BooleanProperty>();
            launchNoDebugProperty.Name = "No Debug";
            launchNoDebugProperty.Description = "Whether the program should be launched without debugging.";
            launchNoDebugProperty.Value = launchNoDebug;
            group.AddProperty(launchNoDebugProperty);

        }
        #endregion

        #region Utilities
        /// <summary>
        /// Returns the first available port.
        ///
        /// <para>
        /// <see href="https://gist.github.com/jrusbatch/4211535"/>
        /// </para>
        /// </summary>
        /// <param name="startingPort">The starting port.</param>
        /// <returns>The port number.</returns>
        private static int GetAvailablePort(int startingPort = 1000)
        {
            IPEndPoint[] endPoints;
            List<int> portArray = new List<int>();

            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();

            //getting active connections
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
            portArray.AddRange(from n in connections
                               where n.LocalEndPoint.Port >= startingPort
                               select n.LocalEndPoint.Port);

            //getting active tcp listners - WCF service listening in tcp
            endPoints = properties.GetActiveTcpListeners();
            portArray.AddRange(from n in endPoints
                               where n.Port >= startingPort
                               select n.Port);

            //getting active udp listeners
            endPoints = properties.GetActiveUdpListeners();
            portArray.AddRange(from n in endPoints
                               where n.Port >= startingPort
                               select n.Port);

            portArray.Sort();

            return Enumerable.Range(startingPort, UInt16.MaxValue)
                .FirstOrDefault(x => !portArray.Contains(x));
        }
        #endregion
    }
}
