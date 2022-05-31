using System;

namespace SEE.Utils
{
    /// <summary>
    /// A representation of paths for directories containing data that can be set absolute
    /// in the file system or relative to one of Unity's standard folders such as
    /// Assets, Project, etc.
    /// </summary>
    [Serializable]
    public class DirectoryPath : DataPath
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public DirectoryPath() : base()
        {
        }

        /// <summary>
        /// Constructor where the kind of root and relative or absolute path is
        /// derived from given <paramref name="path"/> (analogously to <see cref="Set(string)"/>).
        /// </summary>
        /// <param name="path">path from which to derive the kind of root and relative/absolute path</param>
        public DirectoryPath(string path) : base(path)
        {
        }
    }
}
