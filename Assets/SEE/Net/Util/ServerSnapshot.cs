using System;
using Newtonsoft.Json;

namespace SEE.Net.Util
{
    [Serializable]
    public class ServerSnapshot
    {
        [JsonProperty(PropertyName = "id")]
        public Guid Id;

        [JsonProperty(PropertyName = "cityName")]
        public string CityName;

        [JsonProperty(PropertyName = "size")]
        public long Size;

        [JsonProperty(PropertyName = "creationTime")]
        public DateTime CreationTime;
    }
}
