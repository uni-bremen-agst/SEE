using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using SEE.UI.PropertyDialog;
using System.IO;
using UnityEngine;

namespace SEE.UI.DebugAdapterProtocol.DebugAdapter
{
    /// <summary>
    /// Represents an abstract debug adapter.
    /// <para>
    /// To start the debug adapter <see cref="AdapterDirectory"/>, <see cref="AdapterWorkingDirectory"/> and <see cref="AdapterFileName"/> must be defined.
    /// </para>
    /// <para>
    /// Properties used for the launch configuration should be created in the <see cref="SetupLaunchConfig"/> method.<br/>
    /// These properties are not predefined, as these are heavily dependent on the specific debug adapter.<br/>
    /// </para>
    /// </summary>
    public abstract class DebugAdapter
    {
        #region Adapter Configuration
        /// <summary>
        /// The name of the debug adapter.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The directory that contains debug adapters.
        /// </summary>
        protected static readonly string AdapterDirectory = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, "Adapters");

        /// <summary>
        /// The working directory of the adapter executable.
        /// </summary>
        public abstract string AdapterWorkingDirectory { get; set; }

        /// <summary>
        /// The file name of the adapter executable.
        /// </summary>
        public abstract string AdapterFileName { get; set; }

        /// <summary>
        /// The arguments passed to the adapter executable.
        /// </summary>
        public abstract string AdapterArguments { get; set; }
        #endregion

        #region Adapter Methods
        /// <summary>
        /// Creates input fields for the launch properties.
        /// </summary>
        /// <param name="go">Game object for components which create the input fields.</param>
        /// <param name="group">The property group.</param>
        public abstract void SetupLaunchConfig(GameObject go, PropertyGroup group);

        /// <summary>
        /// Saves properties of the launch request configuration.
        /// </summary>
        public abstract void SaveLaunchConfig();

        /// <summary>
        /// Returns the launch request.
        /// </summary>
        /// <returns>The launch request.</returns>
        public abstract LaunchRequest GetLaunchRequest();
        #endregion
    }
}
