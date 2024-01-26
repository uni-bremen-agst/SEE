using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static SEE.DataModel.ChangeType;

namespace SEE.DataModel.DG
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
    public abstract class Attributable : Observable<ChangeEvent>, ICloneable
    {
        public static readonly HashSet<string> NumericAttributeNames = new();

        //----------------------------------
        // Toggle attributes
        //----------------------------------

        /// <summary>
        /// The set of toggle attributes. A toggle is set if it is contained in this
        /// list, otherwise it is unset. Conceptually, toggleAttributes is a HashSet,
        /// but HashSets are not serialized by Unity. That is why we use List instead.
        /// </summary>
        private HashSet<string> toggleAttributes = new HashSet<string>();
        public ISet<string> ToggleAttributes => toggleAttributes;

        /// <summary>
        /// The version of this <see cref="Attributable"/>.
        /// </summary>
        public Guid Version { get; private set; } = Guid.Empty;

        /// <summary>
        /// Unit type consisting of a single value.
        /// </summary>
        public enum UnitType {Unit}

        /// <summary>
        /// Creates a new version for sent-out events on this <see cref="Attributable"/>.
        /// The newly created version ID is returned.
        /// </summary>
        /// <returns>the newly created version ID</returns>
        public Guid NewVersion()
        {
            Guid newVersion = Guid.NewGuid();
            Notify(new VersionChangeEvent(newVersion, Version));
            return Version = newVersion;
        }

        public void SetToggle(string attributeName)
        {
            if (!toggleAttributes.Contains(attributeName))
            {
                toggleAttributes.Add(attributeName);
                Notify(new AttributeEvent<UnitType>(Version, this, attributeName, UnitType.Unit, Addition));
            }
        }

        public void UnsetToggle(string attributeName)
        {
            if (toggleAttributes.Contains(attributeName))
            {
                toggleAttributes.Remove(attributeName);
                Notify(new AttributeEvent<UnitType>(Version, this, attributeName, UnitType.Unit, Removal));
            }
        }

        public bool HasToggle(string attributeName)
        {
            return toggleAttributes.Contains(attributeName);
        }

        //----------------------------------
        // String attributes
        //----------------------------------

        public Dictionary<string, string> StringAttributes { get; private set; } = new Dictionary<string, string>();

        public void SetString(string attributeName, string value)
        {
            StringAttributes[attributeName] = value;
            Notify(new AttributeEvent<string>(Version, this, attributeName, value, Addition));
        }

        public bool TryGetString(string attributeName, out string value)
        {
            return StringAttributes.TryGetValue(attributeName, out value);
        }

        public string GetString(string attributeName)
        {
            if (StringAttributes.TryGetValue(attributeName, out string value))
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

        public Dictionary<string, float> FloatAttributes { get; private set; } = new Dictionary<string, float>();

        public void SetFloat(string attributeName, float value)
        {
            FloatAttributes[attributeName] = value;
            NumericAttributeNames.Add(attributeName);
            Notify(new AttributeEvent<float>(Version, this, attributeName, value, Addition));
        }

        public float GetFloat(string attributeName)
        {
            if (FloatAttributes.TryGetValue(attributeName, out float value))
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
            return FloatAttributes.TryGetValue(attributeName, out value);
        }

        //----------------------------------
        // Integer attributes
        //----------------------------------

        public Dictionary<string, int> IntAttributes { get; private set; } = new Dictionary<string, int>();

        public void SetInt(string attributeName, int value)
        {
            IntAttributes[attributeName] = value;
            NumericAttributeNames.Add(attributeName);
            Notify(new AttributeEvent<int>(Version, this, attributeName, value, Addition));
        }

        public int GetInt(string attributeName)
        {
            if (IntAttributes.TryGetValue(attributeName, out int value))
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
            return IntAttributes.TryGetValue(attributeName, out value);
        }

        //----------------------------------
        // Numeric attributes
        //----------------------------------

        public bool TryGetNumeric(string attributeName, out float value)
        {
            if (IntAttributes.TryGetValue(attributeName, out int intValue))
            {
                value = intValue;
                return true;
            }
            else
            {
                // second try if we cannot find attributeName as an integer attribute
                return FloatAttributes.TryGetValue(attributeName, out value);
            }
        }

        /// <summary>
        /// Returns the value of a numeric (integer or float) attribute for the
        /// attribute named <paramref name="attributeName"/> if it exists.
        /// Otherwise an exception is thrown.
        ///
        /// Note: It could happen that the same name is given to a float and
        /// integer attribute, in which case the float attribute will be
        /// preferred.
        /// </summary>
        /// <param name="attributeName">name of an integer or float attribute</param>
        /// <returns>value of numeric attribute <paramref name="attributeName"/></returns>
        public float GetNumeric(string attributeName)
        {
            if (FloatAttributes.TryGetValue(attributeName, out float floatValue))
            {
                return floatValue;
            }
            else if (IntAttributes.TryGetValue(attributeName, out int intValue))
            {
                return intValue;
            }
            {
                throw new UnknownAttribute(attributeName);
            }
        }

        /// <summary>
        /// Returns the value of an attribute of any type (integer, float, string, or toggle)
        /// for the attribute named <paramref name="attributeName"/> if it exists.
        /// Otherwise an exception is thrown.
        ///
        /// In case of a toggle attribute, the value is always <see cref="UnitType.Unit"/>
        /// if the attribute exists.
        ///
        /// Note: It could happen that the same name is given to different attribute types,
        /// in which case the preference is as follows: float, integer, string, toggle.
        /// </summary>
        /// <param name="attributeName">name of an attribute</param>
        /// <returns>value of attribute <paramref name="attributeName"/></returns>
        /// <exception cref="UnknownAttribute">if <paramref name="attributeName"/> is not an attribute of this node</exception>
        public object GetAny(string attributeName)
        {
            if (FloatAttributes.TryGetValue(attributeName, out float floatValue))
            {
                return floatValue;
            }
            else if (IntAttributes.TryGetValue(attributeName, out int intValue))
            {
                return intValue;
            }
            else if (StringAttributes.TryGetValue(attributeName, out string stringValue))
            {
                return stringValue;
            }
            else if (toggleAttributes.Contains(attributeName))
            {
                return UnitType.Unit;
            }
            else
            {
                throw new UnknownAttribute(attributeName);
            }
        }

        /// <summary>
        /// Returns true if <paramref name="attributeName"/> is the name of an attribute
        /// of any type (integer, float, string, or toggle) of this node, and if so,
        /// sets <paramref name="value"/> to the value of that attribute.
        /// Otherwise <paramref name="value"/> is set to null and false is returned.
        ///
        /// Note: It could happen that the same name is given to different attribute types,
        /// in which case the preference is as follows: float, integer, string, toggle.
        /// </summary>
        /// <param name="attributeName">name of an attribute</param>
        /// <param name="value">value of attribute <paramref name="attributeName"/></param>
        /// <returns>whether <paramref name="attributeName"/> is the name of an attribute of this node</returns>
        public bool TryGetAny(string attributeName, out object value)
        {
            if (FloatAttributes.TryGetValue(attributeName, out float floatValue))
            {
                value = floatValue;
                return true;
            }
            else if (IntAttributes.TryGetValue(attributeName, out int intValue))
            {
                value = intValue;
                return true;
            }
            else if (StringAttributes.TryGetValue(attributeName, out string stringValue))
            {
                value = stringValue;
                return true;
            }
            else if (toggleAttributes.Contains(attributeName))
            {
                value = UnitType.Unit;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Returns the values of all numeric (int and float) attributes of this node.
        /// </summary>
        /// <returns>all numeric attribute values</returns>
        public float[] AllNumerics()
        {
            float[] floats = FloatAttributes.Values.ToArray();
            int[] ints = IntAttributes.Values.ToArray();
            float[] result = new float[floats.Length + ints.Length];
            floats.CopyTo(result, 0);
            int i = floats.Length;
            foreach (int value in ints)
            {
                result[i] = value;
                ++i;
            }
            return result;
        }

        /// <summary>
        /// Returns the names of all numeric attributes (metrics).
        /// </summary>
        /// <returns>names of all numeric attributes</returns>
        public ISet<string> AllMetrics()
        {
            HashSet<string> result = new(FloatAttributes.Keys);
            result.UnionWith(IntAttributes.Keys);
            return result;
        }

        /// <summary>
        /// Yields all string attribute names of this <see cref="Attributable"/>.
        /// </summary>
        /// <returns>all string attribute names</returns>
        public ICollection<string> AllStringAttributeNames()
        {
            return StringAttributes.Keys;
        }

        /// <summary>
        /// Yields all toggle attribute names of this <see cref="Attributable"/>.
        /// </summary>
        /// <returns>all toggle attribute names</returns>
        public ICollection<string> AllToggleAttributeNames()
        {
            return ToggleAttributes;
        }

        /// <summary>
        /// Yields all float attribute names of this <see cref="Attributable"/>.
        /// </summary>
        /// <returns>all float attribute names</returns>
        public ICollection<string> AllFloatAttributeNames()
        {
            return FloatAttributes.Keys;
        }

        /// <summary>
        /// Yields all integer attribute names of this <see cref="Attributable"/>.
        /// </summary>
        /// <returns>all integer attribute names</returns>
        public ICollection<string> AllIntAttributeNames()
        {
            return IntAttributes.Keys;
        }

        //----------------------------------
        // General
        //----------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Report(string message)
        {
            // UnityEngine.Debug.Log(message + "\n");
        }

        /// <summary>
        /// Returns true if <paramref name="other"/> meets all of the following conditions:
        /// (1) is not null
        /// (2) has exactly the same C# type
        /// (3) has exactly the same attributes with exactly the same values as this attributable.
        /// </summary>
        /// <param name="other">to be compared to</param>
        /// <returns>true if equal</returns>
        public override bool Equals(object other)
        {
            if (other == null)
            {
                Report("other is null");
                return false;
            }
            else if (ReferenceEquals(this, other))
            {
                return true;
            }
            else if (GetType() != other.GetType())
            {
                Report("other has different C# type");
                return false;
            }
            else
            {
                return HasSameAttributes(other as Attributable);
            }
        }

        /// <summary>
        /// Yields true if this <see cref="Attributable"/> has exactly the same attributes
        /// as <paramref name="other"/>.
        /// </summary>
        /// <param name="other">other <see cref="Attributable"/> to be compared to</param>
        /// <returns></returns>
        public bool HasSameAttributes(Attributable other)
        {
            if (other == null)
            {
                return false;
            }
            else if (!toggleAttributes.SetEquals(other!.toggleAttributes))
            {
                Report("The toggle attributes are different");
                return false;
            }
            else if (!AreEqual(StringAttributes, other.StringAttributes))
            {
                Report("The string attributes are different");
                return false;
            }
            else if (!AreEqual(IntAttributes, other.IntAttributes))
            {
                Report("The int attributes are different");
                return false;
            }
            else if (!AreEqual(FloatAttributes, other.FloatAttributes))
            {
                Report("The float attributes are different");
                return false;
            }
            else
            {
                return true;
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
        /// <returns>true if <paramref name="left"/> and <paramref name="right"/> are equal</returns>
        protected static bool AreEqual<V>(IDictionary<string, V> left, IDictionary<string, V> right)
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
            return IntAttributes.GetHashCode() ^ StringAttributes.GetHashCode();
        }

        /// <summary>
        /// Returns a string representation for all attributes and their values for this
        /// attributable.
        /// </summary>
        /// <returns>string representation of all attributes</returns>
        public override string ToString()
        {
            string result = "";

            foreach (string attr in toggleAttributes)
            {
                result += $" \"{attr}\": true,\n";
            }

            foreach (KeyValuePair<string, string> attr in StringAttributes)
            {
                result += $" \"{attr.Key}\": \"{attr.Value}\",\n";
            }

            foreach (KeyValuePair<string, int> attr in IntAttributes)
            {
                result += $" \"{attr.Key}\": {attr.Value},\n";
            }

            foreach (KeyValuePair<string, float> attr in FloatAttributes)
            {
                result += $" \"{attr.Key}\": {attr.Value},\n";
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
            Attributable clone = (Attributable)MemberwiseClone();
            base.HandleCloned(clone);
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
            target.toggleAttributes = new HashSet<string>(toggleAttributes);
            target.StringAttributes = new Dictionary<string, string>(StringAttributes);
            target.FloatAttributes = new Dictionary<string, float>(FloatAttributes);
            target.IntAttributes = new Dictionary<string, int>(IntAttributes);
        }
    }
}
