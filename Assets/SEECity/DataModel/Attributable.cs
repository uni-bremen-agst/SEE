using System.Collections.Generic;
using UnityEngine;

namespace SEE.DataModel
{
    /// <summary>
    /// Implements IAttributable providing named toggle, int, float, and string attributes.
    /// </summary>
    public abstract class Attributable : MonoBehaviour, IAttributable
    {
        private HashSet<string> toggleAttributes = new HashSet<string>();

        public void SetToggle(string attributeName)
        {
            toggleAttributes.Add(attributeName);
        }

        public bool HasToggle(string attributeName)
        {
            return toggleAttributes.Contains(attributeName);
        }

        private Dictionary<string, string> stringAttributes = new Dictionary<string, string>();

        public void SetString(string attributeName, string value)
        {
            stringAttributes[attributeName] = value;
        }

        public bool TryGetString(string attributeName, out string value)
        {
            return stringAttributes.TryGetValue(attributeName, out value);
        }

        public string GetString(string attributeName)
        {
            if (stringAttributes.TryGetValue(attributeName, out string value))
            {
                return value;
            }
            else
            {
                throw new UnknownAttribute(attributeName);
            }
        }

        private Dictionary<string, float> floatAttributes = new Dictionary<string, float>();

        public void SetFloat(string attributeName, float value)
        {
            floatAttributes[attributeName] = value;
        }

        public float GetFloat(string attributeName)
        {
            if (floatAttributes.TryGetValue(attributeName, out float value))
            {
                return value;
            }
            else
            {
                throw new UnknownAttribute(attributeName);
            }
        }

        public bool TryGetFloat(string attributeName, out float value)
        {
            return floatAttributes.TryGetValue(attributeName, out value);
        }

        private Dictionary<string, int> intAttributes = new Dictionary<string, int>();

        public void SetInt(string attributeName, int value)
        {
            intAttributes[attributeName] = value;
        }

        public int GetInt(string attributeName)
        {
            if (intAttributes.TryGetValue(attributeName, out int value))
            {
                return value;
            }
            else
            {
                throw new UnknownAttribute(attributeName);
            }
        }

        public bool TryGetInt(string attributeName, out int value)
        {
            return intAttributes.TryGetValue(attributeName, out value);
        }

        public bool TryGetNumeric(string attributeName, out float value)
        {

            if (intAttributes.TryGetValue(attributeName, out int intValue))
            {
                value = intValue;
                return true;
            }
            else
            {
                return floatAttributes.TryGetValue(attributeName, out value);
            }
        }

        public override string ToString()
        {
            string result = "";

            foreach (var attr in toggleAttributes)
            {
                result += " \"" + attr + "\": true,\n";
            }

            foreach (var attr in stringAttributes)
            {
                result += " \"" + attr.Key + "\": \"" + attr.Value + "\",\n";
            }

            foreach (var attr in intAttributes)
            {
                result += " \"" + attr.Key + "\": " + attr.Value + ",\n";
            }

            foreach (var attr in floatAttributes)
            {
                result += " \"" + attr.Key + "\": " + attr.Value + ",\n";
            }
            return result;
        }
    }
}