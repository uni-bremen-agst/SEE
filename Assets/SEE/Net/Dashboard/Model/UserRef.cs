using System;
using Newtonsoft.Json;

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
        [JsonProperty(PropertyName = "name", Required = Required.Always)]
        public readonly string Name;

        /// <summary>
        /// Use this for display of the user in a UI.
        /// </summary>
        [JsonProperty(PropertyName = "displayName", Required = Required.Always)]
        public readonly string DisplayName;

        /// <summary>
        /// User type.
        /// </summary>
        [JsonProperty(PropertyName = "type", Required = Required.Always)]
        public readonly string Type;

        /// <summary>
        /// Whether this user is a so-called public readonly user.
        /// </summary>
        [JsonProperty(PropertyName = "isPublic", Required = Required.Always)]
        public readonly bool IsPublic;

        public UserRef(string name, string displayName, string type, bool isPublic)
        {
            this.Name = name;
            this.DisplayName = displayName;
            this.Type = type;
            this.IsPublic = isPublic;
        }
    }
}