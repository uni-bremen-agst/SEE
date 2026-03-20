using System;
using Newtonsoft.Json;

namespace SEE.Net.Util.FileSync
{
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
