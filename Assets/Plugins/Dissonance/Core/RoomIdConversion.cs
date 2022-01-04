using System;
using System.Collections.Generic;
using Dissonance.Extensions;
using JetBrains.Annotations;

namespace Dissonance
{
    public static class RoomIdConversion
    {
#if DEBUG
        private static readonly Log Log = Logs.Create(LogCategory.Core, "Rooms");
        private static readonly Dictionary<ushort, string> RoomIdMappings = new Dictionary<ushort, string>();
#endif

        public static ushort ToRoomId([NotNull] this string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            var id = Hash16(name);

#if DEBUG
            string existing;
            if (RoomIdMappings.TryGetValue(id, out existing))
            {
                Log.AssertAndLogError(
                    existing == name,
                    "b3ccbf8e-6a6c-4533-8684-5a299c413937",
                    "Hash collision between room names '{0}' and '{1}'. Please choose a different room name.",
                    existing,
                    name
                );
            }
            else
                RoomIdMappings[id] = name;
#endif

            return id;
        }

        private static ushort Hash16([NotNull] string str)
        {
            var hash = str.GetFnvHashCode();

            unchecked
            {
                //We now have a good 32 bit hash, but we want to mix this down into a 16 bit hash
                var upper = (ushort)(hash >> 16);
                var lower = (ushort)hash;
                return (ushort)(upper * 5791 + lower * 7639);
            }
        }
    }
}
