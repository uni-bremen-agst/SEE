using System.Collections.Generic;

namespace SEE.Game.UI.ConfigurationDialog
{
    /// <summary>
    /// A UI dialog consisting of several groups, each with configurable properties.
    /// </summary>
    public class ConfigurationDialog: PlatformDependentComponent
    {
        protected IList<ConfigurationGroup> Groups;
    }
}