using Newtonsoft.Json;
using SEE.Game.SceneManipulation;
using System.Collections.Generic;

namespace SEE.Utils
{
    /// <summary>
    ///  A serializer for lists of <see cref="RestoreGraphElement"/>.
    /// </summary>
    public class RestoreGraphElementListSerializer
    {
        /// <summary>
        /// Serializes the given <paramref name="elementList"/> as a string.
        /// Do not make any assumption about the kind of serialization.
        /// Instead always use <see cref="Unserialize(string)"/> to retrieve
        /// the original string list.
        ///
        /// Precondition: Neither <paramref name="elementList"/> nor any of its
        /// elements is null.
        ///
        /// Note: <paramref name="elementList"/> may be the empty list and
        /// elements in <paramref name="elementList"/> may be the empty string.
        /// </summary>
        /// <param name="elementList">list to be serialized</param>
        /// <returns>serialization of <paramref name="elementList"/></returns>
        /// <exception cref="ArgumentNullException">thrown if <paramref name="elementList"/>
        /// or any of its elements is null</exception>
        public static string Serialize(List<RestoreGraphElement> elementList)
        {
            if (elementList == null || elementList.Contains(null))
            {
                throw new System.ArgumentNullException();
            }
            return JsonConvert.SerializeObject(elementList, Formatting.None,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
        }

        /// <summary>
        /// Unserializes the given <paramref name="serializedList"/> back to
        /// a list of <see cref="RestoreGraphElement"/>.
        ///
        /// Assumption: <paramref name="serializedList"/> is the result of
        /// <see cref="Serialize(List{RestoreGraphElement})"/>.
        ///
        /// Postcondition: Unserialize(Serialize(X)) is equal to X for every X
        /// where X is not null and none of its elements is null.
        /// </summary>
        /// <param name="serializedList">list of <see cref="RestoreGraphElement"/> to be unserialized</param>
        /// <returns>original list of <see cref="RestoreGraphElement"/>s that was serialized by <see cref="Serialize(List{RestoreGraphElement})"/></returns>
        public static List<RestoreGraphElement> Unserialize(string serializedList)
        {
            return JsonConvert.DeserializeObject<List<RestoreGraphElement>>(serializedList,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
        }
    }
}
