using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Newtonsoft.Json.Linq;
using SEE.UI.PropertyDialog;
using SimpleFileBrowser;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SEE.UI.DebugAdapterProtocol.DebugAdapter
{
    /// <summary>
    /// Represents the GDB debug adapter.
    /// <para>
    /// Website: <seealso href="https://sourceware.org/gdb/"/>
    /// </para>
    /// </summary>
    public class GDBDebugAdapter : DebugAdapter
    {
        #region Adapter Configuration
        public override string Name => "GDB";

        public override string AdapterWorkingDirectory { get; set; } = Path.Combine(AdapterDirectory, "gdb");

        public override string AdapterFileName { get; set; } = "gdb";

        public override string AdapterArguments { get; set; } = "-i dap";
        #endregion

        #region Launch Configuration
        /// <summary>
        /// The noDebug property.
        /// </summary>
        private bool launchNoDebug = false;

        /// <summary>
        /// The type property.
        /// </summary>
        private string launchType = "gdb";

        /// <summary>
        /// The name property.
        /// </summary>
        private string launchName = "Launch";

        /// <summary>
        /// The cwd property.
        /// </summary>
        private string launchCwd = Path.Combine("C:\\", "path", "containing", "program");

        /// <summary>
        /// The program property.
        /// </summary>
        private string launchProgram = "program.exe";

        /// <summary>
        /// The stopAtBeginningOfMainSubprogram property.
        /// </summary>
        private bool launchStopAtBeginningOfMainSubprogram = true;

        /// <summary>
        /// The input field for the noDebug property.
        /// </summary>
        private BooleanProperty launchNoDebugProperty;

        /// <summary>
        /// The input field for the cwd property.
        /// </summary>
        private FilePathProperty launchCwdProperty;

        /// <summary>
        /// The input field for the program property.
        /// </summary>
        private FilePathProperty launchProgramProperty;

        /// <summary>
        /// The input field for the stopAtBeginningOfMainSubprogram property.
        /// </summary>
        private BooleanProperty launchStopAtBeginningOfMainSubprogramProperty;
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
                    {"program", launchProgram },
                    {"stopAtBeginningOfMainSubprogram", launchStopAtBeginningOfMainSubprogram},
                }
            };
        }

        public override void SaveLaunchConfig()
        {
            launchNoDebug = launchNoDebugProperty.Value;
            launchCwd = launchCwdProperty.Value;
            launchProgram = launchProgramProperty.Value;
            launchStopAtBeginningOfMainSubprogram = launchStopAtBeginningOfMainSubprogramProperty.Value;
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
            launchProgramProperty.Filters = new[] { new FileBrowser.Filter("Executable", ".exe") };
            launchProgramProperty.Value = launchProgram;
            group.AddProperty(launchProgramProperty);

            launchNoDebugProperty = go.AddComponent<BooleanProperty>();
            launchNoDebugProperty.Name = "No Debug";
            launchNoDebugProperty.Description = "Whether the program should be launched without debugging.";
            launchNoDebugProperty.Value = launchNoDebug;
            group.AddProperty(launchNoDebugProperty);

            launchStopAtBeginningOfMainSubprogramProperty = go.AddComponent<BooleanProperty>();
            launchStopAtBeginningOfMainSubprogramProperty.Name = "No Debug";
            launchStopAtBeginningOfMainSubprogramProperty.Description = "Whether the program should stop at the beginning of main.";
            launchStopAtBeginningOfMainSubprogramProperty.Value = launchStopAtBeginningOfMainSubprogram;
            group.AddProperty(launchStopAtBeginningOfMainSubprogramProperty);
        }
        #endregion
    }
}
