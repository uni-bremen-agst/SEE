using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Controls
{
    /// <summary>
    /// Parent class for mappings. Inherits ScriptableObject class to make it a serializable data container.
    /// </summary>
    public abstract class ActionMapping : ScriptableObject
    {
        public string MappingName = "";

        public abstract void CheckInput();
        public abstract string GetTypeAsString();
        public string GetName()
        {
            return MappingName;
        }
    }
}
