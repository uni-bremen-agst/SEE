using System;
using System.Collections.Generic;
using System.Text;
using Dissonance.Audio.Codecs;
using Dissonance.Datastructures;
using Dissonance.Networking.Client;
using JetBrains.Annotations;

namespace Dissonance.Networking
{
    internal struct PacketReader
    {
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(PacketReader).Name);

        #region fields and properties
        private readonly ArraySegment<byte> _array;
        private int _count;

        public ArraySegment<byte> Read
        {
            // ReSharper disable once AssignNullToNotNullAttribute (Justification: Array segment Array property cannot be null, unless this is a default instance in which case something else is horribly wrong)
            get { return new ArraySegment<byte>(_array.Array, _array.Offset, _count); }
        }

        public ArraySegment<byte> Unread
        {
            // ReSharper disable once AssignNullToNotNullAttribute (Justification: Array segment Array property cannot be null)
            get { return new ArraySegment<byte>(_array.Array, _array.Offset + _count, _array.Count - _count); }
        }

        public ArraySegment<byte> All
        {
            get { return _array; }
        }
        #endregion

        #region constructor
        public PacketReader(ArraySegment<byte> array)
        {
            if (array.Array == null)
                throw new ArgumentNullException("array");

            _array = array;
            _count = 0;
        }

        public PacketReader([NotNull] byte[] array)
            : this(new ArraySegment<byte>(array))
        {
        }
        #endregion

        #region read primitive
        private void Check(int count, string type)
        {
            if (_array.Count - count - _count < 0)
                throw Log.CreatePossibleBugException(string.Format("Insufficient space in packet reader to read {0}", type), "4AFBC61A-77D4-43B8-878F-796F0D921184");
        }

        /// <summary>
        /// Read a byte without performing a check on the size of the array first
        /// </summary>
        /// <returns></returns>
        private byte FastReadByte()
        {
            _count++;
            // ReSharper disable once PossibleNullReferenceException (Justification: Array segment Array property cannot be null)
            return _array.Array[_array.Offset + _count - 1];
        }

        /// <summary>
        /// Read a single byte
        /// </summary>
        /// <returns></returns>
        public byte ReadByte()
        {
            Check(sizeof(byte), "byte");

            return FastReadByte();
        }

        /// <summary>
        /// Read a 16 bit unsigned integer
        /// </summary>
        /// <returns></returns>
        public ushort ReadUInt16()
        {
            Check(sizeof(ushort), "ushort");

            var un = new Union16 {
                MSB = FastReadByte(),
                LSB = FastReadByte()
            };

            return un.UInt16;
        }

        /// <summary>
        /// Read a 32 bit unsigned integer
        /// </summary>
        /// <returns></returns>
        public uint ReadUInt32()
        {
            Check(sizeof(uint), "uint");

            var un = new Union32();

            un.SetBytesFromNetworkOrder(
                FastReadByte(),
                FastReadByte(),
                FastReadByte(),
                FastReadByte()
            );

            return un.UInt32;
        }

        /// <summary>
        /// Read a slice of the internal array (returns a reference to the array, does not perform a copy).
        /// </summary>
        /// <returns></returns>
        public ArraySegment<byte> ReadByteSegment()
        {
            //Read length prefix
            var length = ReadUInt16();

            //Now check that the rest of the data is available
            Check(length, "byte[]");

            //Get the segment from the middle of the buffer
            // ReSharper disable once AssignNullToNotNullAttribute (Justification: Array segment Array property cannot be null)
            var segment = new ArraySegment<byte>(_array.Array, Unread.Offset, length);
            _count += length;

            return segment;
        }

        /// <summary>
        /// Read a string (potentially null)
        /// </summary>
        /// <returns></returns>
        [CanBeNull]public string ReadString()
        {
            //Read the length prefix
            var length = ReadUInt16();

            //Special case for null
            if (length == 0)
                return null;
            else
                length--;

            //Now check that the rest of the string is available
            Check(length, "string");

            //Read the string
            var unread = Unread;
            // ReSharper disable once AssignNullToNotNullAttribute (Justification: Array segment Array property cannot be null)
            var str = Encoding.UTF8.GetString(unread.Array, unread.Offset, length);

            //Apply the offset over the string length
            _count += length;

            return str;
        }

        public CodecSettings ReadCodecSettings()
        {
            var codec = (Codec) ReadByte();
            var frameSize = ReadUInt32();
            var sampleRate = (int)ReadUInt32();
            return new CodecSettings(codec, frameSize, sampleRate);
        }

        public ClientInfo ReadClientInfo()
        {
            var playerName = ReadString();
            var playerId = ReadUInt16();
            var codecSettings = ReadCodecSettings();

            return new ClientInfo(playerName, playerId, codecSettings);
        }
        #endregion

        #region read high level
        public bool ReadPacketHeader(out MessageTypes messageType)
        {
            var magic = ReadUInt16() == PacketWriter.Magic;

            if (magic)
                messageType = (MessageTypes)ReadByte();
            else
                messageType = default(MessageTypes);

            return magic;
        }

        public void ReadHandshakeRequest([CanBeNull] out string name, out CodecSettings codecSettings)
        {
            codecSettings = ReadCodecSettings();
            name = ReadString();
        }

        public void ReadHandshakeResponseHeader(out uint session, out ushort clientId)
        {
            session = ReadUInt32();
            clientId = ReadUInt16();
        }

        /// <summary>
        /// Read the handshake response. Will totally overwrite the contents of outputRoomsToPeerId with a new state
        /// </summary>
        /// <param name="clients"></param>
        /// <param name="outputRoomsToPeerId"></param>
        public void ReadHandshakeResponseBody([NotNull] List<ClientInfo> clients, [NotNull] Dictionary<string, List<ushort>> outputRoomsToPeerId)
        {
            if (clients == null) throw new ArgumentNullException("clients");
            if (outputRoomsToPeerId == null) throw new ArgumentNullException("outputRoomsToPeerId");

            // Read client list
            var clientCount = ReadUInt16();
            for (var i = 0; i < clientCount; i++)
            {
                var client = ReadClientInfo();
                clients.Add(client);
            }

            // Read room name list, later in the packet we'll refer to rooms by their ID and we can look up their name in this list.
            // Only the name is in the packet, we can work out the ID locally.
            var roomNamesById = new Dictionary<ushort, string>();
            var roomNameCount = ReadUInt16();
            for (var i = 0; i < roomNameCount; i++)
            {
                var name = ReadString();
                if (!Log.AssertAndLogWarn(name != null, "Read a null room name in handshake packet (potentially corrupt packet)"))
                    roomNamesById[name.ToRoomId()] = name;
            }

            //Clear all the lists
            using (var enumerator = outputRoomsToPeerId.GetEnumerator())
                while (enumerator.MoveNext())
                    enumerator.Current.Value.Clear();

            //Read out lists per channel
            var channelCount = ReadUInt16();
            for (var i = 0; i < channelCount; i++)
            {
                var channel = ReadUInt16();
                var peerCount = ReadByte();

                //Find room name
                string room;
                Log.AssertAndThrowPossibleBug(roomNamesById.TryGetValue(channel, out room), "C8E9EBED-2A46-4207-A050-0ABFE00BA9E8", "Could not find room name in handshake for ID:{0}", channel);
                
                //Get or create a list for this channel
                List<ushort> peers;
                if (!outputRoomsToPeerId.TryGetValue(room, out peers))
                {
                    peers = new List<ushort>();
                    outputRoomsToPeerId[room] = peers;
                }

                //Read out all the peers for this chanel
                for (var j = 0; j < peerCount; j++)
                    peers.Add(ReadUInt16());
            }
        }

        public void ReadhandshakeP2P(out ushort peerId)
        {
            //assigned peer ID
            peerId = ReadUInt16();
        }

        /// <summary>
        /// Read the state of a client, will get/create a ClientInfo object from the given dictionary
        /// </summary>
        public ClientInfo ReadClientStateHeader()
        {
            // ReSharper disable once AssignNullToNotNullAttribute (Justification: we're sanity checking this immediately below)
            var client = ReadClientInfo();
            if (client.PlayerName == null)
                throw Log.CreatePossibleBugException("Deserialized a ClientState packet with a null client Name", "5F77FC4F-4110-4A6F-8F96-97B393AD7324");

            return client;
        }

        public void ReadClientStateRooms([NotNull] List<string> rooms)
        {
            if (rooms == null)
                throw new ArgumentNullException("rooms");

            rooms.Clear();

            var count = ReadUInt16();
            for (var i = 0; i < count; i++)
            {
                //Sanity check that string is not null (shouldn't be possible if encoding is correct)
                var str = ReadString();
                if (str == null) Log.Debug("Read a null string in client state rooms list. Ignoring.");

                rooms.Add(str);

            }
        }

        public void ReadRemoveClient(out ushort clientId)
        {
            clientId = ReadUInt16();
        }

        public void ReadVoicePacketHeader1(out ushort senderId)
        {
            senderId = ReadUInt16();
        }

        public void ReadVoicePacketHeader2(out VoicePacketOptions options, out ushort sequenceNumber, out ushort numChannels)
        {
            options = VoicePacketOptions.Unpack(ReadByte());
            sequenceNumber = ReadUInt16();
            numChannels = ReadUInt16();
        }

        public void ReadVoicePacketChannel(out ChannelBitField bitfield, out ushort recipient)
        {
            bitfield = new ChannelBitField(ReadUInt16());
            recipient = ReadUInt16();
        }

        public TextPacket ReadTextPacket()
        {
            var options = ReadByte();
            var senderId = ReadUInt16();
            var target = ReadUInt16();
            var txt = ReadString();

            return new TextPacket(senderId, (ChannelType)options, target, txt);
        }

        public uint ReadErrorWrongSession()
        {
            return ReadUInt32();
        }

        public void ReadRelay(List<ushort> destinations, out ArraySegment<byte> data)
        {
            //Read out destinations
            var count = ReadByte();
            for (var i = 0; i < count; i++)
                destinations.Add(ReadUInt16());

            //Read out data (not allocating a new array, it's just a slice of this packet)
            data = ReadByteSegment();
        }

        public void ReadDeltaChannelState(out bool joined, out ushort peer, [NotNull] out string name)
        {
            joined = ReadByte() != 0;
            peer = ReadUInt16();

            // ReSharper disable once AssignNullToNotNullAttribute (Justification: we're asserting it's not null)
            name = ReadString();
            Log.AssertAndThrowPossibleBug(name != null, "51C30A8D-C665-4AFD-A88F-BC9060A4DDB9", "Read a null string from a DeltaChannelState packet");
        }
        #endregion
    }
}
