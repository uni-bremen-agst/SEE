using System;
using System.Collections.Generic;
using System.Linq;

namespace SEE.DataModel
{
    // When you hit the play button in the editor, all the objects in the active scene are 
    // serialized and saved, so that unity can deserialize and return them to their original 
    // state when you stop the execution in the editor. Unity also creates copies of all 
    // objects in the scene, so the changes you do during play mode change the copies, not 
    // the original objects in the scene. During this copy process it deserializes the 
    // objects with the data it saved just before copying, so no visible change is done 
    // on the objects.
    // 
    // All the scripts which inherit from MonoBehaviour are serializable, but custom classes 
    // are not. To inform unity that you want your class to be serialized you have to use 
    // the [System.Serializable] attribute. 
    //
    // Also, unity only serializes the public members in your class, if you want your 
    // private members to be serialized too, you should inform unity with the [SerializeField] 
    // attribute. Note: Unity does not serialize static fields.
    //
    // More on Unity's serialization can be found here: 
    // https://docs.unity3d.com/Manual/script-Serialization.html

    /// <summary>
    /// Specifies and implements attributable objects with named toggle, int, float, and string attributes.
    /// </summary>
    public abstract class Attributable : ICloneable
    {
        //----------------------------------
        // Toggle attributes
        //----------------------------------

        /// <summary>
        /// The set of toggle attributes. A toggle is set if it is contained in this
        /// list, otherwise it is unset. Conceptionally, toggleAttributes is a HashSet,
        /// but HashSets are not serialized by Unity. That is why we use List instead.
        /// </summary>
        private HashSet<string> toggleAttributes = new HashSet<string>();
        public ICollection<string> ToggleAttributes
        {
            get => toggleAttributes;
        }

        public void SetToggle(string attributeName)
        {
            if (!toggleAttributes.Contains(attributeName))
            {
                toggleAttributes.Add(attributeName);
            }
        }

        public bool HasToggle(string attributeName)
        {
            return toggleAttributes.Contains(attributeName);
        }

        //----------------------------------
        // String attributes
        //----------------------------------

        // Unity does not serializes Dictionaries. That is why we need to use StringStringDictionary
        // instead here. Note that we need to declare the attribute here as a SerializeField 
        // nevertheless.
        private Dictionary<string, string> stringAttributes = new Dictionary<string, string>();
        public Dictionary<string, string> StringAttributes
        {
            get => stringAttributes;
        }

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

        //----------------------------------
        // Float attributes
        //----------------------------------

        public Dictionary<string, float> floatAttributes = new Dictionary<string, float>();
        public Dictionary<string, float> FloatAttributes
        {
            get => floatAttributes;
        }

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

        //----------------------------------
        // Integer attributes
        //----------------------------------

        public Dictionary<string, int> intAttributes = new Dictionary<string, int>();
        public Dictionary<string, int> IntAttributes
        {
            get => intAttributes;
        }

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

        //----------------------------------
        // Numeric attributes
        //----------------------------------

        public bool TryGetNumeric(string attributeName, out float value)
        {

            if (intAttributes.TryGetValue(attributeName, out int intValue))
            {
                value = intValue;
                return true;
            }
            else
            {
                // second try if we cannot find attributeName as an integer attribute
                return floatAttributes.TryGetValue(attributeName, out value);
            }
        }

        //----------------------------------
        // General
        //----------------------------------

        protected static void Report(string message)
        {
            UnityEngine.Debug.Log(message + "\n");
        }

        /// <summary>
        /// Returns true if <paramref name="other"/> meets all of the following conditions:
        /// (1) is not null
        /// (2) has exactly the same C# type
        /// (3) has exactly the same attributes with exactly the same values as this attributable.
        /// </summary>
        /// <param name="other">to be compared to</param>
        /// <returns>true if equal</returns>
        public override bool Equals(Object other)
        {
            if (other == null)
            {
                Report("other is null");
                return false;
            }
            else if (this.GetType() != other.GetType())
            {
                Report("other has different C# type");
                return false;
            }
            else
            {
                Attributable otherAttributable = other as Attributable;
                if (!this.toggleAttributes.SetEquals(otherAttributable.toggleAttributes))
                {
                    Report("The toggle attributes are different");
                    return false;
                }
                else if (!AreEqual<string>(this.stringAttributes, otherAttributable.stringAttributes))
                {
                    Report("The string attributes are different");
                    return false;
                }
                else if (!AreEqual<int>(this.intAttributes, otherAttributable.intAttributes))
                {
                    Report("The int attributes are different");
                    return false;
                }
                else if (!AreEqual<float>(this.floatAttributes, otherAttributable.floatAttributes))
                {
                    Report("The float attributes are different");
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Yields true if the two dictionaries are equal, i.e., have the same number of entries,
        /// and for each key in <paramref name="left"/> there is the same key in <paramref name="right"/>
        /// with the same value and vice versa.
        /// </summary>
        /// <typeparam name="V">any kind of type for a dictionary value</typeparam>
        /// <param name="left">left dictionary for the comparison</param>
        /// <param name="right">right dictionary for the comparison</param>
        /// <returns></returns>
        protected static bool AreEqual<V>(Dictionary<string,V> left, Dictionary<string,V> right)
        {
            return left.Count == right.Count && !left.Except(right).Any();
        }

        /// <summary>
        /// Returns a hash code.
        /// </summary>
        /// <returns>hash code</returns>
        public override int GetHashCode()
        {
            // we are using only those two attribute kinds to avoid unnecessary 
            // computation in the hope that they suffice; nodes and edges should
            // have some attributes of this kind sufficiently different to others
            return intAttributes.GetHashCode() ^ stringAttributes.GetHashCode();
        }

        /// <summary>
        /// Returns a string representation for all attributes and their values for this
        /// attributable.
        /// </summary>
        /// <returns>string representation of all attributes</returns>
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

        /// <summary>
        /// Returns a deep clone of this attributable. Deep means that the list
        /// of attributes of this attributable are copied, too.
        /// </summary>
        /// <returns>deep clone</returns>
        public virtual object Clone()
        {
            var clone = (Attributable)this.MemberwiseClone();
            HandleCloned(clone);
            return clone;
        }

        /// <summary>
        /// Creates deep copies of attributes where necessary. Is called by
        /// Clone() once the copy is created. Must be extended by every 
        /// subclass that adds fields that should be cloned, too.
        /// </summary>
        /// <param name="clone">the clone receiving the copied attributes</param>
        protected virtual void HandleCloned(object clone)
        {
            Attributable target = (Attributable)clone;
            // The dictionaries must be newly created and assigned because MemberwiseClone() creates
            // a shallow copy in which those attributes will all refer to the dictionaries of the
            // original attributable.
            // Because the keys and values are primitive types, the following are deep copies of the 
            // attributes.
            target.toggleAttributes = new HashSet<string>(this.toggleAttributes);
            target.stringAttributes = new Dictionary<string, string>(this.stringAttributes);
            target.floatAttributes = new Dictionary<string, float>(this.floatAttributes);
            target.intAttributes = new Dictionary<string, int>(this.intAttributes);
        }
    }
}