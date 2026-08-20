using System;
using Newtonsoft.Json;

namespace SEE.Net.Util
{
    /// <summary>
    /// Represents a snapshot object from the server.
    /// </summary>
    [Serializable]
    public class ServerSnapshot
    {
        /// <summary>
        /// Id of the snapshot.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public Guid Id;

        /// <summary>
        /// The city name the snapshot was created for.
        /// </summary>
        [JsonProperty(PropertyName = "cityName")]
        public string CityName;

        /// <summary>
        /// Size of the snapshot file.
        /// </summary>
        [JsonProperty(PropertyName = "size")]
        public long Size;

        /// <summary>
        /// Creation time of the snapshot.
        /// </summary>
        [JsonProperty(PropertyName = "creationTime")]
        public DateTime CreationTime;
    }
}
