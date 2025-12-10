using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// A factory for creating instances of <see cref="ParsingConfig"/>.
    /// 
    /// This factory uses reflection to automatically discover all non-abstract subclasses 
    /// of <see cref="ParsingConfig"/> within the executing assembly. It allows creating 
    /// concrete configuration objects based on their unique tool identifier (e.g., "JaCoCo").
    /// </summary>
    public static class ParsingConfigFactory
    {
        /// <summary>
        /// A registry mapping the unique tool identifier (e.g., "JaCoCo") to the corresponding 
        /// C# <see cref="Type"/> of the configuration class.
        /// </summary>
        private static readonly Dictionary<string, Type> _registry = new Dictionary<string, Type>();

        /// <summary>
        /// Static constructor that initializes the factory by registering all available configuration types.
        /// This ensures the registry is populated upon the first access to this class.
        /// </summary>
        static ParsingConfigFactory()
        {
            RegisterAllConfigs();
        }

        /// <summary>
        /// Scans the executing assembly for all valid <see cref="ParsingConfig"/> implementations
        /// and registers them in the <see cref="_registry"/>.
        /// 
        /// A type is considered valid if it inherits from <see cref="ParsingConfig"/> and is not abstract.
        /// To determine the key for the registry, a temporary instance of the type is created 
        /// to access its <see cref="ParsingConfig.ToolId"/>.
        /// </summary>
        private static void RegisterAllConfigs()
        {
            // 1. Get all types in the current assembly.
            Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();

            // 2. Filter: Select only non-abstract subclasses of ParsingConfig.
            IEnumerable<Type> configTypes = allTypes.Where(t => t.IsSubclassOf(typeof(ParsingConfig)) && !t.IsAbstract);

            foreach (Type type in configTypes)
            {
                try
                {
                    // 3. Create a temporary instance to retrieve the identifier.
                    // We need the instance because the ID is an instance property/field, not static.
                    ParsingConfig tempInstance = (ParsingConfig)Activator.CreateInstance(type);

                    // 4. Retrieve the identifier.
                    string kind = tempInstance.ToolId;

                    if (!string.IsNullOrEmpty(kind))
                    {
                        if (!_registry.ContainsKey(kind))
                        {
                            _registry.Add(kind, type);
                        }
                        else
                        {
                            Debug.LogError($"Duplicate ParsingConfig kind found: '{kind}' used by {_registry[kind].Name} and {type.Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Could not register ParsingConfig type {type.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Creates a new instance of a <see cref="ParsingConfig"/> corresponding to the given <paramref name="kind"/>.
        /// </summary>
        /// <param name="kind">The unique tool identifier (e.g., "JaCoCo") for which to create the configuration.</param>
        /// <returns>A new instance of the matching <see cref="ParsingConfig"/> subclass.</returns>
        /// <exception cref="ArgumentException">Thrown if no configuration type is registered for the given <paramref name="kind"/>.</exception>
        public static ParsingConfig Create(string kind)
        {
            if (_registry.TryGetValue(kind, out Type type))
            {
                // Create a new, fresh instance of the registered type.
                return (ParsingConfig)Activator.CreateInstance(type);
            }

            throw new ArgumentException($"Unknown ParsingConfig kind: '{kind}'. Ensure the class is implemented, inherits from ParsingConfig, and initializes its ToolId correctly.");
        }
    }
}