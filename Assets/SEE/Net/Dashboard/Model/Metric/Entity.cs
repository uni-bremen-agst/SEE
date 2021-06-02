using System;
using Valve.Newtonsoft.Json;

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
        [JsonProperty(Required = Required.Always)]
        public readonly string id;
        
        /// <summary>
        /// A non-unique name of the entity.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string name;
        
        /// <summary>
        /// The type of the entity.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string type;
        
        /// <summary>
        /// The file path of an entity if it can be associated with a file.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public readonly string path;
        
        /// <summary>
        /// The line number of an entity if it can be associated with a file location.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public readonly uint? line;

        public Entity(string id, string name, string type, string path, uint? line)
        {
            this.id = id;
            this.name = name;
            this.type = type;
            this.path = path;
            this.line = line;
        }
    }
}