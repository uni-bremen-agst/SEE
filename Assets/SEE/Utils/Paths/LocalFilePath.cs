using Sirenix.OdinInspector;
using System;

namespace SEE.Utils.Paths
{
    /// <summary>
    /// Represents the path to a file that exists locally on the disk.
    /// The path can be set absolute in the file system or relative to one of Unity's
    /// standard folders such as Assets, Project, etc.
    /// </summary>
    [Serializable]
    internal class LocalFilePath : FilePath
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public LocalFilePath() : base()
        {
        }

        /// <summary>
        /// Constructor where the kind of root and relative or absolute path is
        /// derived from given <paramref name="path"/> (analogously to <see cref="Set(string)"/>).
        /// </summary>
        /// <param name="path">path from which to derive the kind of root and relative/absolute path</param>
        public LocalFilePath(string path) : base(path)
        {
        }

        /// <summary>
        /// The stored full path.
        ///
        /// If the <see cref="Root"/> is absolute, the result is equivalent to <see cref="AbsolutePath"/>.
        ///
        /// Otherwise the prefix specified by <see cref="Root"/> will be prepended
        /// to <see cref="RelativePath"/>.
        /// The style of this path prefix is always the one of the current operating
        /// system we are running on, that is, the directory separator will be \
        /// on Windows and / on all other platforms.
        /// </summary>
        [ShowInInspector, FilePath(AbsolutePath = true)]
        public override string Path
        {
            get => Get();
            set => Set(value);
        }
    }
}
