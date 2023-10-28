using System;
using Newtonsoft.Json;

namespace SEE.Net.Dashboard.Model.Metric
{
    /// <summary>
    /// A Project Entity such as a Class, a Method, a File or a Module or the System Entity.
    /// </summary>
    [Serializable]
    public class Entity
    {
        /// <summary>
        /// The project-wide ID used to refer to this entity.
        /// </summary>
        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public readonly string ID;

        /// <summary>
        /// A non-unique name of the entity.
        /// </summary>
        [JsonProperty(PropertyName = "name", Required = Required.Always)]
        public readonly string Name;

        /// <summary>
        /// The type of the entity.
        /// </summary>
        [JsonProperty(PropertyName = "type", Required = Required.Always)]
        public readonly string Type;

        /// <summary>
        /// The file path of an entity if it can be associated with a file.
        /// </summary>
        [JsonProperty(PropertyName = "path", Required = Required.Default)]
        public readonly string Path;

        /// <summary>
        /// The line number of an entity if it can be associated with a file location.
        /// </summary>
        [JsonProperty(PropertyName = "line", Required = Required.Default)]
        public readonly uint? Line;

        public Entity(string id, string name, string type, string path, uint? line)
        {
            this.ID = id;
            this.Name = name;
            this.Type = type;
            this.Path = path;
            this.Line = line;
        }
    }
}