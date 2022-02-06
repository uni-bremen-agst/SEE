using System;
using Dissonance.Networking;
using JetBrains.Annotations;

namespace Dissonance
{
    public sealed class TextChat
    {
        private readonly Func<ICommsNetwork> _getNetwork;

        internal TextChat([NotNull] Func<ICommsNetwork> getNetwork)
        {
            if (getNetwork == null)
                throw new ArgumentNullException("getNetwork");

            _getNetwork = getNetwork;
        }

        /// <summary>
        /// Send a text chat message to a specific room
        /// </summary>
        /// <param name="roomName">The room to send a message to</param>
        /// <param name="message">The message to send</param>
        public void Send([NotNull] string roomName, [NotNull] string message)
        {
            if (roomName == null)
                throw new ArgumentNullException("roomName", "Cannot send a text message to a null room");
            if (message == null)
                throw new ArgumentNullException("message", "Cannot send null text message");

            var net = _getNetwork();
            if (net != null)
                net.SendText(message, ChannelType.Room, roomName);
        }

        /// <summary>
        /// Send a text chat message to a specific player
        /// </summary>
        /// <param name="playerName">The player to send a message to</param>
        /// <param name="message">The message to send</param>
        public void Whisper([NotNull] string playerName, [NotNull] string message)
        {
            if (playerName == null)
                throw new ArgumentNullException("playerName", "Cannot send a text message to a null playerName");
            if (message == null)
                throw new ArgumentNullException("message", "Cannot send null text message");

            var net = _getNetwork();
            if (net != null)
                net.SendText(message, ChannelType.Player, playerName);
        }

        /// <summary>
        /// Event invoked whenever a message is received
        /// </summary>
        /// <remarks>To receive messages from a room join the room using DissonanceComms.Rooms.Join(room)</remarks>
        public event Action<TextMessage> MessageReceived;

        internal void OnMessageReceived(TextMessage obj)
        {
            var handler = MessageReceived;
            if (handler != null) handler(obj);
        }
    }
}
