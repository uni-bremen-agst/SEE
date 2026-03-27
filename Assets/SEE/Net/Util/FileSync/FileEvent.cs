using Newtonsoft.Json;

namespace SEE.Net.Util.FileSync
{

    /// <summary>
    /// Base class for file events.
    /// </summary>
    public class FileEvent
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
    }
}
