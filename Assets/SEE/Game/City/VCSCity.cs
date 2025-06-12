using System.Collections.Generic;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// A city based on the data of a version control system (VCS).
    /// </summary>
    public abstract class VCSCity : SEECity
    {
        /// <summary>
        /// Name of the Inspector foldout group for the version control system
        /// (VCS) setttings.
        /// </summary>
        protected const string VCSFoldoutGroup = "VCS";

        /// <summary>
        /// The path to the VCS containing the two revisions to be compared.
        /// </summary>
        [ShowInInspector, Tooltip("The path to the VCS."),
            TabGroup(VCSFoldoutGroup), RuntimeTab(VCSFoldoutGroup)]
        public DataPath VCSPath = new();

        #region Config I/O

        //--------------------------------
        // Configuration file input/output
        //--------------------------------

        /// <summary>
        /// Label of attribute <see cref="VCSPath"/> in the configuration file.
        /// </summary>
        private const string vcsPathLabel = "VCSPath";

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            VCSPath.Save(writer, vcsPathLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            VCSPath.Restore(attributes, vcsPathLabel);
        }

        #endregion Config I/O
    }
}
