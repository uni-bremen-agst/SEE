using System;
using System.Collections.Generic;

namespace Dissonance.Networking.Client
{
    internal interface IClient<TPeer>
        where TPeer : struct
    {
        void SendReliable(ArraySegment<byte> arraySegment);

        void SendUnreliable(ArraySegment<byte> arraySegment);

        void SendReliableP2P(List<ClientInfo<TPeer?>> destinations, ArraySegment<byte> packet);

        void SendUnreliableP2P(List<ClientInfo<TPeer?>> destinations, ArraySegment<byte> packet);
    }
}
