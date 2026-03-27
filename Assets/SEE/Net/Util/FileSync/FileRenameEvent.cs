using System;
using Newtonsoft.Json;

namespace SEE.Net.Util.FileSync
{
    /// <summary>
    /// File event when a file is renamed.
    /// </summary>
    [Serializable]
    public class FileRenameEvent : FileEvent
    {
        /// <summary>
        /// The new content of the file.
        /// </summary>
        [JsonProperty(PropertyName = "newFileName")]
        public string NewFileName;
    }
}
