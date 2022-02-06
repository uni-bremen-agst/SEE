using System;
using Dissonance.Config;
using Random = System.Random;

namespace Dissonance.Networking.Client
{
    internal class PacketDelaySimulator
    {
        #region fields and properties
        private readonly Random _rnd = new Random();
        #endregion

        private static bool IsOrderedReliable(MessageTypes header)
        {
            return header != MessageTypes.VoiceData;
        }

        public bool ShouldLose(ArraySegment<byte> packet)
        {
            #if DEBUG
            if (DebugSettings.Instance.EnableNetworkSimulation)
            {
                var reader = new PacketReader(packet);

                //Read the header, if we don't know what this packet is then play it safe and don't lose it
                MessageTypes header;
                if (!reader.ReadPacketHeader(out header))
                    return false;

                var loss = DebugSettings.Instance.PacketLoss;
                if (!IsOrderedReliable(header) && loss > 0 && _rnd.NextDouble() < loss)
                    return true;
            }
            #endif

            return false;
        }
    }
}
