using System;
using System.Collections.Generic;
using System.Linq;

namespace SEE.Net
{
    /// <summary>
    /// Represents a class for fragments of a packet to be sent.
    /// </summary>
    [Serializable]
    public class Fragment
    {
        /// <summary>
        /// The packet id.
        /// </summary>
        public readonly string PacketID;

        /// <summary>
        /// The fragments size of the packet.
        /// </summary>
        public readonly int PacketSize;

        /// <summary>
        /// The number of the current fragment.
        /// </summary>
        public readonly int CurrentFragment;

        /// <summary>
        /// The data of the fragment.
        /// </summary>
        public readonly string Data;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="packageID">The packet id.</param>
        /// <param name="packageSize">The amount how many fragments the packet have.</param>
        /// <param name="currentFragment">The current fragment.</param>
        /// <param name="data">The data of the fragment.</param>
        public Fragment(string packageID, int packageSize, int currentFragment, string data)
        {
            PacketID = packageID;
            PacketSize = packageSize;
            CurrentFragment = currentFragment;
            Data = data;
        }

        /// <summary>
        /// Combines the fragments to recover the packet data.
        /// </summary>
        /// <param name="fragments">The fragments</param>
        /// <returns>The combined string, is empty if a fragment is missing or a wrong fragment
        /// is in the list.</returns>
        public static string CombineFragments(List<Fragment> fragments)
        {
            string combined = "";
            fragments.OrderBy(f => f.CurrentFragment);
            string id = fragments[0].PacketID;
            int packageSize = fragments[0].PacketSize;
            if (fragments.All(f => f.PacketID == id)
                && fragments.All(f => f.PacketSize == packageSize)
                && CheckNumbering(fragments))
            {
                for (int i = 0; i < packageSize; i++)
                {
                    combined += fragments[i].Data;
                }
            }
            return combined;
        }

        /// <summary>
        /// Checks whether all fragments have been received.
        /// </summary>
        /// <param name="fragments">The list of fragments.</param>
        /// <returns>True if every fragment is in the list, false if some fragment is missing.</returns>
        private static bool CheckNumbering(List<Fragment> fragments)
        {
            List<int> numbers = fragments.Select(f => f.CurrentFragment).ToList();
            int packageSize = fragments[0].PacketSize;
            for (int i = 0; i < packageSize; i++)
            {
                if (!numbers.Contains(i))
                {
                    return false;
                }
            }
            return true;
        }
    }
}