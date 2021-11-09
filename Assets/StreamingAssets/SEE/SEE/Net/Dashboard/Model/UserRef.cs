using System;
using Valve.Newtonsoft.Json;

namespace SEE.Net.Dashboard.Model
{
    /// <summary>
    /// Reference to a certain user.
    /// </summary>
    [Serializable]
    public class UserRef
    {
        /// <summary>
        /// User name. Use this to refer to the same user.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string name;

        /// <summary>
        /// Use this for display of the user in a UI.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string displayName;

        /// <summary>
        /// User type.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string type;

        /// <summary>
        /// Whether this user is a so-called public readonly user.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly bool isPublic;

        public UserRef(string name, string displayName, string type, bool isPublic)
        {
            this.name = name;
            this.displayName = displayName;
            this.type = type;
            this.isPublic = isPublic;
        }
    }
}