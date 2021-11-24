using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Dissonance.Networking
{
    internal class TrafficCounter
    {
        public uint Packets { get; private set; }
        public uint Bytes { get; private set; }
        public uint BytesPerSecond { get; private set; }

        private uint _runningTotal;
        private readonly Queue<KeyValuePair<DateTime, uint>> _updated = new Queue<KeyValuePair<DateTime, uint>>(64);

        public void Update(int bytes, DateTime? now = null)
        {
            //Check bytes is in valid range
            if (bytes < 0) throw new ArgumentOutOfRangeException("bytes");
            var uBytes = (uint)bytes;

            //If it eventually overflows the total byte/packet count we'll get wrong stats, but at least it won't crash
            unchecked
            {
                Packets++;
                Bytes += uBytes;
            }

            //Store the update in a queue, keyed by time
            var time = now ?? DateTime.UtcNow;
            _updated.Enqueue(new KeyValuePair<DateTime, uint>(time, uBytes));
            _runningTotal += uBytes;

            //Remove the oldest value if it's over 10 seconds old
            if (time - _updated.Peek().Key >= TimeSpan.FromSeconds(10))
            {
                var removed = _updated.Dequeue();
                _runningTotal -= removed.Value;

                //Calculate bytes per second, now that we have a 10 second window
                BytesPerSecond = _runningTotal / 10;
            }
        }

        public override string ToString()
        {
            return Format(Packets, Bytes, BytesPerSecond);
        }

        public static void Combine(out uint packets, out uint bytes, out uint totalBytesPerSecond, [NotNull] params TrafficCounter[] counters)
        {
            packets = 0;
            bytes = 0;
            totalBytesPerSecond = 0;

            foreach (var counter in counters)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse, HeuristicUnreachableCode (Justification: Sanity check)
                if (counter == null) continue;

                packets += counter.Packets;
                bytes += counter.Bytes;
                totalBytesPerSecond += counter.BytesPerSecond;
            }
        }

        [NotNull] public static string Format(ulong packets, ulong bytes, ulong bytesPerSecond)
        {
            return string.Format("{0} in {1:N0}pkts at {2}/s", FormatByteString(bytes), packets, FormatByteString(bytesPerSecond));
        }

        [NotNull] private static string FormatByteString(decimal bytes)
        {
            const decimal kb = 1024;
            const decimal mb = kb * 1024;
            const decimal gb = mb * 1024;

            string suffix;

            if (bytes >= gb)
            {
                bytes /= gb;
                suffix = "GiB";
            }
            else if (bytes >= mb)
            {
                bytes /= mb;
                suffix = "MiB";
            }
            else if (bytes >= kb)
            {
                bytes /= kb;
                suffix = "KiB";
            }
            else
                suffix = "B";

            return string.Format("{0:0.0}{1}", bytes, suffix);
        }
    }
}
