using System;
using Newtonsoft.Json;

namespace SEE.Net.Util.FileSync
{
    /// <summary>
    /// Event message when a file was updated.
    /// </summary>
    [Serializable]
    public class FileUpdateEvent
    {
        /// <summary>
        /// The relative path to the project dir.
        /// </summary>
        [JsonProperty(PropertyName = "fileName")]
        public string FileName;

        /// <summary>
        /// The project type.
        /// This will be one of the first subdirs in the Multiplayer directory.
        /// </summary>
        [JsonProperty(PropertyName = "projectType")]
        public string ProjectType;

        /// <summary>
        /// The new content of the file.
        /// </summary>
        [JsonProperty(PropertyName = "content")]
        public string fileContent;

    }
}
