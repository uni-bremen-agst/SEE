using System;
using Newtonsoft.Json;

namespace SEE.Net.Util.FileSync
{
    /// <summary>
    /// Event message when a file was updated.
    /// </summary>
    [Serializable]
    public class FileUpdateEvent : FileEvent
    {
        /// <summary>
        /// The new content of the file.
        /// </summary>
        [JsonProperty(PropertyName = "content")]
        public string fileContent;

    }
}
