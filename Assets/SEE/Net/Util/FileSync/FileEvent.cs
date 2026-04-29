using Newtonsoft.Json;

namespace SEE.Net.Util.FileSync
{
    /// <summary>
    /// Base class for file events.
    /// </summary>
    public class FileEvent
    {
        /// <summary>
        /// The path of the file, relative to the project directory.
        /// </summary>
        [JsonProperty(PropertyName = "fileName")]
        public string FileName;

        /// <summary>
        /// The type of project (e.g. CodeCity, ReflexionCity) the file belongs to.
        ///
        /// The project directory is located in the Multiplayer directory with the same name as this variable.
        /// </summary>
        [JsonProperty(PropertyName = "projectType")]
        public string ProjectType;
    }
}
