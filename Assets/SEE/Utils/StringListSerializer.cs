using System.Collections.Generic;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    ///  A serializer for lists of strings.
    /// </summary>
    public class StringListSerializer
    {
        /// <summary>
        /// We are wrapping the serialized list of strings into this
        /// class so that it can be serialized.
        /// </summary>
        [System.Serializable]
        private class Wrapper
        {
            public List<string> stringList;
        }

        /// <summary>
        /// Serializes the given <paramref name="stringList"/> as a string.
        /// Do not make any assumption about the kind of serialization.
        /// Instead always use <see cref="Unserialize(string)"/> to retrieve
        /// the original string list.
        ///
        /// Precondition: Neither <paramref name="stringList"/> nor any of its
        /// elements is null.
        ///
        /// Note: <paramref name="stringList"/> may be the empty list and
        /// elements in <paramref name="stringList"/> may be the empty string.
        /// </summary>
        /// <param name="stringList">list to be serialized</param>
        /// <returns>serialization of <paramref name="stringList"/></returns>
        /// <exception cref="ArgumentNullException">thrown if <paramref name="stringList"/>
        /// or any of its elements is null</exception>
        public static string Serialize(List<string> stringList)
        {
            if (stringList == null || stringList.Contains(null))
            {
                throw new System.ArgumentNullException();
            }
            return JsonUtility.ToJson(new Wrapper
            {
                stringList = stringList
            });
        }

        /// <summary>
        /// Unserializes the given <paramref name="serializedList"/> back to
        /// a list of strings.
        ///
        /// Assumption: <paramref name="serializedList"/> is the result of
        /// <see cref="Serialize(List{string})"/>.
        ///
        /// Postcondition: Unserialize(Serialize(X)) is equal to X for every X
        /// where X is not null and none of its elements is null.
        /// </summary>
        /// <param name="serializedList">list of strings to be unserialized</param>
        /// <returns>original list of strings that was serialized by <see cref="Serialize(List{string})"/></returns>
        public static List<string> Unserialize(string serializedList)
        {
            return JsonUtility.FromJson<Wrapper>(serializedList).stringList;
        }
    }
}
