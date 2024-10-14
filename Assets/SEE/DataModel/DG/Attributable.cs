using SEE.Utils;
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
        /// <summary>
        /// The names of all numeric attributes (int and float) of any <see cref="Attributable"/>.
        /// Note that this set is shared among all instances of <see cref="Attributable"/>.
        /// Note also that it may contain names of attributes that are no longer present in any
        /// <see cref="Attributable"/> instance. This can happen if an attribute is removed from
        /// all instances. In this case, the name will remain in this set.
        /// </summary>
        public static readonly ThreadSafeHashSet<string> NumericAttributeNames = new();

        //----------------------------------
        // Toggle attributes
        //----------------------------------

        /// <summary>
        /// The set of toggle attributes. A toggle is set if it is contained in this
        /// list, otherwise it is unset. Conceptually, toggleAttributes is a HashSet,
        /// but HashSets are not serialized by Unity. That is why we use List instead.
        /// </summary>
        private HashSet<string> toggleAttributes = new();
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

        /// <summary>
        /// If <paramref name="value"/> is true, the toggle with <paramref name="attributeName"/>
        /// will be set, otherwise it will be removed.
        /// </summary>
        /// <param name="attributeName">name of toggle attribute</param>
        /// <param name="value">value to be set</param>
        /// <remarks>All listeners will be notified in case of a change.</remarks>
        public void SetToggle(string attributeName, bool value)
        {
            if (value)
            {
                SetToggle(attributeName);
            }
            else
            {
                UnsetToggle(attributeName);
            }
        }

        /// <summary>
        /// Sets the toggle attribute with <paramref name="attributeName"/> if not
        /// already set. All listeners will be notified of this change.
        /// If the attribute is set already, nothing happens.
        /// </summary>
        /// <param name="attributeName">name of toggle attribute</param>
        /// <remarks>All listeners will be notified in case of a change.</remarks>
        public void SetToggle(string attributeName)
        {
            if (toggleAttributes.Add(attributeName))
            {
                Notify(new AttributeEvent<UnitType>(Version, this, attributeName, UnitType.Unit, Addition));
            }
        }

        /// <summary>
        /// Removes the toggle attribute with <paramref name="attributeName"/> if set.
        /// All listeners will be notified of this change.
        /// If no such attribute exists, nothing happens.
        /// </summary>
        /// <param name="attributeName">name of toggle attribute</param>
        /// <remarks>All listeners will be notified in case of a change.</remarks>
        public void UnsetToggle(string attributeName)
        {
            if (toggleAttributes.Contains(attributeName))
            {
                toggleAttributes.Remove(attributeName);
                Notify(new AttributeEvent<UnitType>(Version, this, attributeName, UnitType.Unit, Removal));
            }
        }

        /// <summary>
        /// True if the toggle attribute with <paramref name="attributeName"/> is set.
        /// </summary>
        /// <param name="attributeName">name of toggle attribute</param>
        /// <returns>true if set</returns>
        public bool HasToggle(string attributeName)
        {
            return toggleAttributes.Contains(attributeName);
        }

        //----------------------------------
        // String attributes
        //----------------------------------

        public Dictionary<string, string> StringAttributes { get; private set; } = new();

        /// <summary>
        /// Sets the string attribute with given <paramref name="attributeName"/> to <paramref name="value"/>
        /// if <paramref name="value"/> is different from <c>null</c>. If <paramref name="value"/> is <c>null</c>,
        /// the attribute will be removed.
        /// </summary>
        /// <param name="attributeName">name of the attribute</param>
        /// <param name="value">new value of the attribute</param>
        /// <remarks>This method will notify all listeners of this attributable</remarks>
        public void SetString(string attributeName, string value)
        {
            if (value == null)
            {
                StringAttributes.Remove(attributeName);
            }
            else
            {
                StringAttributes[attributeName] = value;
            }
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

        public Dictionary<string, float> FloatAttributes { get; private set; } = new();

        /// <summary>
        /// Sets the float attribute with given <paramref name="attributeName"/> to <paramref name="value"/>
        /// if <paramref name="value"/> is different from <c>null</c>. If <paramref name="value"/> is <c>null</c>,
        /// the attribute will be removed.
        /// </summary>
        /// <param name="attributeName">name of the attribute</param>
        /// <param name="value">new value of the attribute</param>
        /// <remarks>This method will notify all listeners of this attributable</remarks>
        public void SetFloat(string attributeName, float? value)
        {
            if (value.HasValue)
            {
                FloatAttributes[attributeName] = value.Value;
                NumericAttributeNames.Add(attributeName);
            }
            else
            {
                FloatAttributes.Remove(attributeName);
            }
            Notify(new AttributeEvent<float?>(Version, this, attributeName, value, Addition));
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

        public Dictionary<string, int> IntAttributes { get; private set; } = new();

        /// <summary>
        /// Sets the integer attribute with given <paramref name="attributeName"/> to <paramref name="value"/>
        /// if <paramref name="value"/> is different from <c>null</c>. If <paramref name="value"/> is <c>null</c>,
        /// the attribute will be removed.
        /// </summary>
        /// <param name="attributeName">name of the attribute</param>
        /// <param name="value">new value of the attribute</param>
        /// <remarks>This method will notify all listeners of this attributable</remarks>
        public void SetInt(string attributeName, int? value)
        {
            if (value.HasValue)
            {
                IntAttributes[attributeName] = value.Value;
                NumericAttributeNames.Add(attributeName);
            }
            else
            {
                IntAttributes.Remove(attributeName);
            }
            Notify(new AttributeEvent<int?>(Version, this, attributeName, value, Addition));
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
        /// <exception cref="UnknownAttribute">thrown in case there is no such <paramref name="attributeName"/></exception>
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
            throw new UnknownAttribute(attributeName);
        }

        // ------------------------------
        // Range attributes
        // ------------------------------

        public const string RangeStartLineSuffix = "_StartLine";
        public const string RangeStartCharacterSuffix = "_StartCharacter";
        public const string RangeEndLineSuffix = "_EndLine";
        public const string RangeEndCharacterSuffix = "_EndCharacter";

        /// <summary>
        /// Sets the range attribute with given <paramref name="attributeName"/> to <paramref name="value"/>
        /// if <paramref name="value"/> is different from <c>null</c>. If <paramref name="value"/> is <c>null</c>,
        /// the attribute will be removed.
        /// </summary>
        /// <param name="attributeName">name of the attribute</param>
        /// <param name="value">new value of the attribute</param>
        /// <remarks>This method will notify all listeners of this attributable</remarks>
        public void SetRange(string attributeName, Range value)
        {
            if (value == null)
            {
                IntAttributes.Remove(attributeName + RangeStartLineSuffix);
                IntAttributes.Remove(attributeName + RangeStartCharacterSuffix);
                IntAttributes.Remove(attributeName + RangeEndLineSuffix);
                IntAttributes.Remove(attributeName + RangeEndCharacterSuffix);
            }
            else
            {
                IntAttributes[attributeName + RangeStartLineSuffix] = value.StartLine;
                IntAttributes[attributeName + RangeEndLineSuffix] = value.EndLine;
                if (value.StartCharacter.HasValue)
                {
                    IntAttributes[attributeName + RangeStartCharacterSuffix] = value.StartCharacter.Value;
                }
                else
                {
                    IntAttributes.Remove(attributeName + RangeStartCharacterSuffix);
                }
                if (value.EndCharacter.HasValue)
                {
                    IntAttributes[attributeName + RangeEndCharacterSuffix] = value.EndCharacter.Value;
                }
                else
                {
                    IntAttributes.Remove(attributeName + RangeEndCharacterSuffix);
                }
            }
            Notify(new AttributeEvent<Range>(Version, this, attributeName, value, Addition));
        }

        public bool TryGetRange(string attributeName, out Range value)
        {
            if (IntAttributes.TryGetValue(attributeName + RangeStartLineSuffix, out int startLine) &&
                IntAttributes.TryGetValue(attributeName + RangeEndLineSuffix, out int endLine))
            {
                int? startCharacter, endCharacter;
                if (IntAttributes.TryGetValue(attributeName + RangeStartCharacterSuffix, out int startCharacterValue))
                {
                    startCharacter = startCharacterValue;
                }
                else
                {
                    startCharacter = null;
                }
                if (IntAttributes.TryGetValue(attributeName + RangeEndCharacterSuffix, out int endCharacterValue))
                {
                    endCharacter = endCharacterValue;
                }
                else
                {
                    endCharacter = null;
                }
                value = new Range(startLine, endLine, startCharacter, endCharacter);
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public Range GetRange(string attributeName)
        {
            if (TryGetRange(attributeName, out Range value))
            {
                return value;
            }
            else
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
        /// <exception cref="UnknownAttribute">if <paramref name="attributeName"/> is not an attribute of this attributable</exception>
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
        /// Returns a shallow clone of this attributable. Shallow means that
        /// only the immediate list of attributes of this attributable
        /// (i.e., <see cref="ToggleAttributes"/>, <see cref="StringAttributes"/>,
        /// <see cref="FloatAttributes"/>, and <see cref="IntAttributes"/>) are copied, too.
        /// </summary>
        /// <returns>shallow clone</returns>
        public virtual object CloneAttributes()
        {
            Attributable clone = (Attributable)Activator.CreateInstance(GetType());
            CopyAttributes(clone);
            return clone;
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
            CopyAttributes(target);
        }

        /// <summary>
        /// Copies <see cref="ToggleAttributes"/>, <see cref="StringAttributes"/>,
        /// <see cref="FloatAttributes"/>, and <see cref="IntAttributes"/>) from this
        /// attributable to <paramref name="target"/>.
        /// </summary>
        /// <param name="target">receiver of the copied attributes</param>
        private void CopyAttributes(Attributable target)
        {
            target.toggleAttributes = new HashSet<string>(toggleAttributes);
            target.StringAttributes = new Dictionary<string, string>(StringAttributes);
            target.FloatAttributes = new Dictionary<string, float>(FloatAttributes);
            target.IntAttributes = new Dictionary<string, int>(IntAttributes);
        }
    }
}
