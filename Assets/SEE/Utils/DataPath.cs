using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// A representation of paths for files and directories containing data
    /// that can be set absolute in the file system or relative to one of
    /// Unity's standard folders such as Assets, Project, etc.
    /// </summary>
    [Serializable]
    public abstract class DataPath
    {
        /// <summary>
        /// Defines how the path is to be interpreted. If it is absolute,
        /// nothing will be prepended to the path. In all other cases,
        /// a prefix will be prepended to the path. The kind of prefix
        /// is specified by the other root kinds.
        /// </summary>
        [Serializable]
        public enum RootKind
        {
            /// <summary>
            /// It is an absolute path. Nothing will be prepended.
            /// </summary>
            Absolute,
            /// <summary>
            /// Is is a path relative to the project folder (which is the parent
            /// of the Assets folder). Application.dataPath (excluding "/Assets"
            /// at the end) is the prefix.
            /// </summary>
            ProjectFolder,
            /// <summary>
            /// Is is a path relative to the Assets folder. Application.dataPath
            /// is the prefix.
            /// </summary>
            AssetsFolder,
            /// <summary>
            /// Is is a path relative to the streaming assets.
            /// Application.streamingAssetsPath is the prefix.
            /// </summary>
            StreamingAssets,
            /// <summary>
            /// Is is a path relative to the folder for persistent data.
            /// Application.persistentDataPath is the prefix.
            /// </summary>
            PersistentData,
            /// <summary>
            /// Is is a path relative to the temporary cache.
            /// Application.temporaryCachePath is the prefix.
            /// </summary>
            TemporaryCache,
        }

        public DataPath()
        {
            // intentionally left blank
        }

        /// <summary>
        /// Constructor where the kind of root and relative or absolute path is
        /// derived from given <paramref name="path"/> (analogously to <see cref="Set(string)"/>).
        /// </summary>
        /// <param name="path">path from which to derive the kind of root and relative/absolute path</param>
        public DataPath(string path)
        {
            Set(path);
        }

        /// <summary>
        /// Adjusts the root and path information of this data path based on the given <paramref name="path"/>.
        /// If none of Unity's path prefixes for standard folders match, <paramref name="path"/> is considered
        /// an absolute path. Otherwise the <see cref="Root"/> will be set depending on which of Unity's
        /// path prefixes matches (<seealso cref="RootKind"/>) and the relative path will be set to
        /// <paramref name="path"/> excluding the matched prefix.
        /// </summary>
        /// <param name="path">an absolute path</param>
        public void Set(string path)
        {
            if (path.Contains(Application.streamingAssetsPath))
            {
                Root = RootKind.StreamingAssets;
                relativePath = path.Replace(Application.streamingAssetsPath, string.Empty);
            }
            else if (path.Contains(Application.dataPath))
            {
                Root = RootKind.AssetsFolder;
                relativePath = path.Replace(Application.dataPath, string.Empty);
            }
            else if (path.Contains(Application.persistentDataPath))
            {
                Root = RootKind.PersistentData;
                relativePath = path.Replace(Application.persistentDataPath, string.Empty);
            }
            else if (path.Contains(Application.temporaryCachePath))
            {
                Root = RootKind.TemporaryCache;
                relativePath = path.Replace(Application.temporaryCachePath, string.Empty);
            }
            else if (path.Contains(ProjectFolder()))
            {
                Root = RootKind.ProjectFolder;
                relativePath = path.Replace(ProjectFolder(), string.Empty);
            }
            else
            {
                Root = RootKind.Absolute;
                absolutePath = path;
            }
        }

        /// <summary>
        /// Defines how the stored path is to be interpreted.
        /// </summary>
        [SerializeField] public RootKind Root = RootKind.AssetsFolder;

        /// <summary>
        /// If <paramref name="rootKind"/> is absolute, the empty string is returned.
        /// Otherwise yields Unity's folders as absolute paths depending upon
        /// <see cref="<paramref name="rootKind"/>; see also <seealso cref="RootKind"/>.
        /// The character / will be used as directory separator for that path.
        /// The last character in the path will never be the directory separator /.
        /// </summary>
        /// <param name="rootKind">the kind of root</param>
        /// <returns>root path</returns>
        private static string GetRootPath(RootKind rootKind)
        {
            return rootKind switch
            {
                RootKind.PersistentData => Application.persistentDataPath,
                RootKind.StreamingAssets => Application.streamingAssetsPath,
                RootKind.TemporaryCache => Application.temporaryCachePath,
                RootKind.AssetsFolder => Application.dataPath,
                RootKind.ProjectFolder => ProjectFolder(),
                RootKind.Absolute => string.Empty,
                _ => throw new NotImplementedException("Unhandled case " + rootKind)
            };
        }

        /// <summary>
        /// Returns the Unity project folder, which is the Assets folder excluding the
        /// "/Assets" at the end.
        /// </summary>
        /// <returns>Unity project folder</returns>
        public static string ProjectFolder()
        {
            return Regex.Replace(Application.dataPath, "/Assets$", string.Empty);
        }

        /// <summary>
        /// If the <see cref="Root"/> is absolute, the directory enclosing this
        /// path is returned (may be empty). The directory separator of the
        /// resulting absolute root path is the one that was used for setting the absolute
        /// path. If it was set on a different operating system, it may not be
        /// the one used for the operating system we are currently running on.
        ///
        /// Otherwise yields Unity's folders as absolute paths depending upon
        /// <paramref name="rootKind"/>; see also <seealso cref="RootKind"/>.
        /// The character / will then be used as directory separator for that path.
        /// The last character in the path will never be the directory separator /.
        /// </summary>
        /// <returns>root path</returns>
        public string RootPath
        {
            get
            {
                if (Root == RootKind.Absolute)
                {
                    string path = Path;
                    if (string.IsNullOrEmpty(path))
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return System.IO.Path.GetDirectoryName(path);
                    }
                }
                else
                {
                    return GetRootPath(Root);
                }
            }
        }

        /// <summary>
        /// The internal representation of property <see cref="RelativePath"/>.
        /// The internal representation of this path is always in the Unix style
        /// (or also Unity style), independent from the operating system we are currently
        /// running on.
        /// </summary>
        [SerializeField] private string relativePath = "";

        /// <summary>
        /// The path relative to the <see cref="Root"/>. The directory separator will be /.
        /// Retrieve this value only if <see cref="Root"/> is not the absolute path.
        /// </summary>
        public string RelativePath
        {
            get => relativePath;
            set => relativePath = value;
        }

        /// <summary>
        /// The internal representation of property <see cref="AbsolutePath"/>.
        /// </summary>
        [SerializeField] private string absolutePath = "";
        /// <summary>
        /// The absolute path. Retrieve this value only if <see cref="Root"/> is the absolute path.
        /// The directory separator used here is the exactly the same how it was set in the
        /// last assignment. It may be a Windows, Mac, or Unix separator, no matter on which
        /// operating system we are currently running on.
        /// </summary>
        public string AbsolutePath
        {
            get => absolutePath;
            set => absolutePath = value;
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
        public string Path
        {
            get
            {
                if (Root == RootKind.Absolute)
                {
                    if (string.IsNullOrEmpty(absolutePath))
                    {
                        return "";
                    }
                    else
                    {
                        return absolutePath;
                    }
                }
                else
                {
                    // Path is relative to root.
                    if (string.IsNullOrEmpty(relativePath))
                    {
                        return Filenames.OnCurrentPlatform(RootPath);
                    }
                    else if (!relativePath.StartsWith("/"))
                    {
                        return Filenames.OnCurrentPlatform(RootPath + "/" + relativePath);
                    }
                    else
                    {
                        return Filenames.OnCurrentPlatform(RootPath + relativePath);
                    }
                }
            }
        }

        /// <summary>
        /// The attribute label for the relative path of a DataPath in the stored configuration file.
        /// </summary>
        private const string RelativePathLabel = "RelativePath";
        /// <summary>
        /// The attribute label for the absolute path of a DataPath in the stored configuration file.
        /// </summary>
        private const string AbsolutePathLabel = "AbsolutePath";
        /// <summary>
        /// The attribute label for the root kind of a DataPath in the stored configuration file.
        /// </summary>
        private const string RootLabel = "Root";

        /// <summary>
        /// Saves the attributes of this <see cref="DataPath"/> using given <paramref name="writer"/>
        /// under the given <paramref name="label"/>.
        /// </summary>
        /// <param name="writer">used to save the attributes</param>
        /// <param name="label">the label for saved attributes</param>
        public void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Root.ToString(), RootLabel);
            writer.Save(RelativePath, RelativePathLabel);
            writer.Save(AbsolutePath, AbsolutePathLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Restores the state of the <see cref="DataPath"/> according to <paramref name="attributes"/>.
        ///
        /// Looks up the attributes of the data path in <paramref name="attributes"/> using
        /// the key <paramref name="label"/> and sets the internal attributes of this
        /// instance of <see cref="DataPath"/> according the looked up values.
        /// </summary>
        /// <param name="attributes">where to look up the values</param>
        /// <param name="label">the key for the lookup</param>
        public void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> path = dictionary as Dictionary<string, object>;
                {
                    string value = "";
                    if (ConfigIO.Restore(path, RelativePathLabel, ref value))
                    {
                        RelativePath = value;
                    }
                }
                {
                    string value = "";
                    if (ConfigIO.Restore(path, AbsolutePathLabel, ref value))
                    {
                        AbsolutePath = value;
                    }
                }
                ConfigIO.RestoreEnum(path, RootLabel, ref Root);
            }
        }
    }
}
