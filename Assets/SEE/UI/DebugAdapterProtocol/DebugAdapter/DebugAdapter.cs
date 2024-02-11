using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using SEE.UI.PropertyDialog;
using System.IO;
using UnityEngine;

namespace SEE.UI.DebugAdapterProtocol.DebugAdapter
{
    public abstract class DebugAdapter
    {
        /// <summary>
        /// The directory that contains debug adapters.
        /// </summary>
        protected static readonly string AdapterDirectory = Path.Combine("D:\\" + "ferdi", "Adapters");

        /// <summary>
        /// The name.
        /// </summary>
        public abstract string Name { get; }
        
        /// <summary>
        /// The working directory.
        /// </summary>
        public abstract string AdapterWorkingDirectory { get; set; }
        
        /// <summary>
        /// The file name.
        /// </summary>
        public abstract string AdapterFileName { get; set; }
        
        /// <summary>
        /// The arguments.
        /// </summary>
        public abstract string AdapterArguments { get; set; }

        /// <summary>
        /// Adds properties to the launch request configuration.
        /// </summary>
        /// <param name="go"></param>
        /// <param name="group"></param>
        public abstract void SetupLaunchConfig(GameObject go, PropertyGroup group);

        /// <summary>
        /// Saves propertes of the launch request configuration.
        /// </summary>
        public abstract void SaveLaunchConfig();


        /// <summary>
        /// Returns the launch request.
        /// </summary>
        /// <returns>The launch request.</returns>
        public abstract LaunchRequest GetLaunchRequest();
    }
}