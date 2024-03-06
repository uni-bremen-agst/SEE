using SEE.Utils.Config;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using Network = SEE.Net.Network;

namespace SEE.Utils.Paths
{
    /// <summary>
    /// A representation of URL or local disk paths of files and directories containing data.
    /// Files and directories can be set absolute in the file system or relative to one of
    /// Unity's standard folders such as Assets, Project, etc. URLs can be relative to
    /// our server at <see cref="Network.ClientRestAPI"/> or relate to other servers.
    /// </summary>
    [Serializable]
    public class DataPath
    {
        /// <summary>
        /// Defines how the path is to be interpreted. If it is absolute or a URL,
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
            /// of the Assets folder). <see cref="Application.dataPath"/> (excluding "/Assets"
            /// at the end) is the prefix.
            /// </summary>
            ProjectFolder,
            /// <summary>
            /// Is is a path relative to the Assets folder. <see cref="Application.dataPath"/>
            /// is the prefix.
            /// </summary>
            AssetsFolder,
            /// <summary>
            /// Is is a path relative to the streaming assets.
            /// <see cref="Application.streamingAssetsPath"/> is the prefix.
            /// </summary>
            StreamingAssets,
            /// <summary>
            /// Is is a path relative to the folder for persistent data.
            /// <see cref="Application.persistentDataPath"/> is the prefix.
            /// </summary>
            PersistentData,
            /// <summary>
            /// Is is a path relative to the temporary cache.
            /// <see cref="Application.temporaryCachePath"/> is the prefix.
            /// </summary>
            TemporaryCache,
            /// <summary>
            /// The path is a universal resource identifier (URI).
            /// </summary>
            Url
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
        ///
        /// Note: This method should not be called for a <see cref="RootKind.Url"/>.
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
        /// If <see cref="Root"/> is <see cref="RootKind.Url"/>, the empty string
        /// is returned.
        ///
        /// If <see cref="Root"/> is <see cref="RootKind.Absolute"/>, the directory
        /// enclosing this path (i.e., the parent) is returned (may be empty).
        /// The directory separator of the resulting absolute root path is the
        /// one that was used for setting the absolute path. If it was set on
        /// a different operating system, it may not be the one used for the
        /// operating system we are currently running on.
        ///
        /// Otherwise yields Unity's folders as absolute paths depending upon
        /// <see cref="Root"/>; see also <seealso cref="RootKind"/>.
        /// The character / will then be used as directory separator for that path.
        /// The last character in the path will never be the directory separator /.
        ///
        /// IMPORTANT NOTE: This method is intended for situations in which a
        /// file system path is to be picked. It should not be used for
        /// <see cref="RootKind.Url"/>. If this path represents a <see cref="RootKind.Url"/>,
        /// the empty string is returned.
        /// </summary>
        /// <returns>root path</returns>
        public string RootFileSystemPath
        {
            get
            {
                if (Root is RootKind.Url)
                {
                    return string.Empty;
                }
                else if (Root is RootKind.Absolute)
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
        [SerializeField, HideInInspector] private string relativePath = "";

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
        [SerializeField, HideInInspector] private string absolutePath = "";
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
        [ShowInInspector, FilePath(AbsolutePath = true)]
        public string Path
        {
            get => Get();
            set => Set(value);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private string Get()
        {
            if (Root == RootKind.Url)
            {
                // absolutePath is set only for foreign servers, in which case relativePath
                // will be empty. If the absolutePath is empty, the relativePath is interpreted relative
                // to our server.
                Uri baseUri = absolutePath.Length > 0 ? new(absolutePath) : new(Network.ClientRestAPI);
                Uri relativeUri = new(relativePath, UriKind.Relative);
                return new Uri(baseUri, relativeUri).ToString();
            }
            else if (Root == RootKind.Absolute)
            {
                return absolutePath;
            }
            else
            {
                // Path is relative to root.
                if (string.IsNullOrEmpty(relativePath))
                {
                    return Filenames.OnCurrentPlatform(RootFileSystemPath);
                }
                else if (!relativePath.StartsWith("/"))
                {
                    return Filenames.OnCurrentPlatform(RootFileSystemPath + "/" + relativePath);
                }
                else
                {
                    return Filenames.OnCurrentPlatform(RootFileSystemPath + relativePath);
                }
            }
        }

        /// <summary>
        /// Adjusts the root and path information of this data path based on the given <paramref name="path"/>.
        ///
        /// If <paramref name="isURI"/> is true and the URI prefix matches <see cref="Network.ClientRestAPI"/>,
        /// the path will be stored as a relative path (where <see cref="Network.ClientRestAPI"/> is removed
        /// from <paramref name="path"/>. If the URI prefix does not match, <paramref name="path"/> will
        /// stored as relative or absolute path, respectively, depending upon <paramref name="path"/>
        /// interpreted as a universal resource identifier is relative or absolute. The <see cref="Root"/>
        /// will be <see cref="RootKind.Url"/>.
        ///
        /// If <paramref name="isURI"/> is false, the path is interpreted as a disk path.
        /// If none of Unity's path prefixes for standard folders match, <paramref name="path"/> is considered
        /// an absolute path. Otherwise the <see cref="Root"/> will be set depending on which of Unity's
        /// path prefixes matches (<seealso cref="RootKind"/>) and the relative path will be set to
        /// <paramref name="path"/> excluding the matched prefix.
        /// </summary>
        /// <param name="path">an absolute path</param>
        /// <exception cref="ArgumentNullException">thrown if <paramref name="path"/> is null</exception>
        /// <exception cref="UriFormatException">thrown if this path is supposed to be a <see cref="RootKind.Url"/>
        /// but <paramref name="path"/> does not conform to the URI syntax</exception>
        private void Set(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (Root == RootKind.Url)
            {
                // The constructor will check whether path is a valid URI
                // and if not, throw an exception.
                Uri uri = new(path);
                if (uri.IsAbsoluteUri)
                {
                    if (path.Contains(Network.ClientRestAPI))
                    {
                        // The path relates to our server.
                        absolutePath = string.Empty;
                        relativePath = path.Replace(Network.ClientRestAPI, string.Empty);
                    }
                    else
                    {
                        // The path relates to a different server.
                        absolutePath = path;
                        relativePath = string.Empty;
                    }
                }
                else
                {
                    // It is a relative path.
                    absolutePath = string.Empty;
                    relativePath = path;
                }
                // Summary: absolutePath is set only for foreign servers, in which case relativePath
                // will be empty. If the absolutePath is empty, the relativePath is interpreted relative
                // to our server.

            }
            else if (path.Contains(Application.streamingAssetsPath))
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
        /// Yields a stream containing the data retrieved from <see cref="Path"/>.
        ///
        /// If the data path represents a <see cref="RootKind.Url"/>, the file is
        /// downloaded from a server. Otherwise it is read from a local file.
        /// </summary>
        /// <returns>stream containing the data</returns>
        /// <exception cref="IOException">in case the data cannot be loaded</exception>
        public async Task<Stream> LoadAsync()
        {
            string path = Path;
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new IOException("Path is empty or null.");
            }
            if (Root == RootKind.Url)
            {
                return await LoadFromServerAsync(path);
            }
            else
            {
                if (File.Exists(Path))
                {
                    return File.OpenRead(path);
                }
                else
                {
                    throw new IOException($"Path '{path}' does not exist.");
                }
            }
        }

        /// <summary>
        /// Downloads and returns a file from the given <paramref name="url"/>.
        /// </summary>
        /// <param name="url">URL of the file to be downloaded</param>
        /// <returns>a stream containing the downloaded data</returns>
        /// <exception cref="IOException">if file cannot be downloaded</exception>
        private static async Task<Stream> LoadFromServerAsync(string url)
        {
            Uri uri = new(url);
            HttpClient client = new();
            HttpRequestMessage request = new(HttpMethod.Get, uri);
            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStreamAsync();
            }
            else
            {
                throw new IOException($"Failed to download from URI {uri}. Reason: {response.ReasonPhrase}. Status code: {response.StatusCode}.\n");
            }
        }

        public override string ToString()
        {
            return $"root={Root} relativePath={relativePath} absolutePath={absolutePath}";
        }

        #region Config I/O

        /// <summary>
        /// The attribute label for the relative path of a DataPath in the stored configuration file.
        /// </summary>
        private const string relativePathLabel = "RelativePath";
        /// <summary>
        /// The attribute label for the absolute path of a DataPath in the stored configuration file.
        /// </summary>
        private const string absolutePathLabel = "AbsolutePath";
        /// <summary>
        /// The attribute label for the root kind of a DataPath in the stored configuration file.
        /// </summary>
        private const string rootLabel = "Root";

        /// <summary>
        /// Saves the attributes of this <see cref="DataPath"/> using given <paramref name="writer"/>
        /// under the given <paramref name="label"/>.
        /// </summary>
        /// <param name="writer">used to save the attributes</param>
        /// <param name="label">the label for saved attributes</param>
        public void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Root.ToString(), rootLabel);
            writer.Save(RelativePath, relativePathLabel);
            writer.Save(AbsolutePath, absolutePathLabel);
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
                    if (ConfigIO.Restore(path, relativePathLabel, ref value))
                    {
                        RelativePath = value;
                    }
                }
                {
                    string value = "";
                    if (ConfigIO.Restore(path, absolutePathLabel, ref value))
                    {
                        AbsolutePath = value;
                    }
                }
                ConfigIO.RestoreEnum(path, rootLabel, ref Root);
            }
        }

        #endregion
    }
}
