using System;
using System.Collections.Generic;
using System.Threading;
using Dissonance.Datastructures;
using Dissonance.Networking;
using HandyCollections.Heap;
using JetBrains.Annotations;

namespace Dissonance.Audio.Playback
{
    /// <summary>
    ///     Buffers encoded audio packets as they arrive, and delivers them in order when requested.
    /// </summary>
    internal class EncodedAudioBuffer
    {
        private static readonly Log Log = Logs.Create(LogCategory.Playback, typeof (EncodedAudioBuffer).Name);
        
        private readonly MinHeap<VoicePacket> _heap;
        private readonly Action<VoicePacket> _droppedFrameHandler;

        private volatile bool _complete;
        private int _count;

        public int Count
        {
            get { return _count; }
        }

        public uint SequenceNumber { get; private set; }

        private readonly PacketLossCalculator _loss = new PacketLossCalculator(128);
        public float PacketLoss { get { return _loss.PacketLoss; } }

        public EncodedAudioBuffer([NotNull] Action<VoicePacket> droppedFrameHandler)
        {
            if (droppedFrameHandler == null) throw new ArgumentNullException("droppedFrameHandler");

            _droppedFrameHandler = droppedFrameHandler;
            _heap = new MinHeap<VoicePacket>(32, new VoicePacketComparer()) { AllowHeapResize = true };
            SequenceNumber = 0;
            _complete = false;
        }

        public void Push(VoicePacket frame)
        {
            Log.Trace("Buffering encoded audio frame {0}", frame.SequenceNumber);

            _heap.Add(frame);
            Interlocked.Increment(ref _count);

            if (_count > 39 && _count % 10 == 0)
                Log.Warn("Encoded audio heap is getting very large ({0} items)", _count);
        }

        public void Stop()
        {
            Log.Trace("Stopping");
            _complete = true;
        }

        /// <summary>
        ///     Reads the next frame from the buffer.
        /// </summary>
        /// <param name="frame">The next frame to play. May return <c>null</c> if the frame has not been received.</param>
        /// <param name="lostPacket">Indicates if the next packet to decode is unavailable. If so the `frame` will either be null, or the next but one packet in sequenced</param>
        /// <returns><c>true</c> if there are more frames available; else <c>false</c></returns>
        public bool Read(out VoicePacket? frame, out bool lostPacket)
        {
            var expected = SequenceNumber;

            // remove frames which we have already decoded past (they arrived too late to be useful)
            while (_heap.Count > 0 && _heap.Minimum.SequenceNumber < expected)
            {
                var dropped = _heap.RemoveMin();
                Interlocked.Decrement(ref _count);

                var difference = expected - dropped.SequenceNumber;
                Log.Trace("Discarding late encoded audio frame from buffer ({0} packets late)", difference);

                if (difference > 30)
                    Log.Warn("Received a very late packet ({0} packets late). This may indicate severe network congestion or a very poor frame rate. (30EF1B03-7BBC-49D3-A23E-6E84781FF29F)", difference);

                _droppedFrameHandler(dropped);
            }

            if (_heap.Count > 0 && _heap.Minimum.SequenceNumber == expected)
            {
                // the next frame is the one we are looking for
                frame = _heap.RemoveMin();
                Interlocked.Decrement(ref _count);
                lostPacket = false;
                Log.Trace("Retrieved frame {0} from buffer ({1} items remain in buffer)", frame.Value.SequenceNumber, _heap.Count);
            }
            else
            {
                //We don't have the _next_ frame yet.
                // - If we have the next next frame, return that with the packet lost flag set
                // - Otherwise return null frame, with packet lost flag set
                lostPacket = true;
                if (_heap.Count > 0 && _heap.Minimum.SequenceNumber == expected + 1)
                    frame = _heap.Minimum;
                else
                    frame = null;
            }

            _loss.Update(!lostPacket);
            SequenceNumber++;
            return IsComplete();
        }

        public void Reset()
        {
            Log.Trace("Resetting");

            while (_heap.Count > 0)
            {
                _droppedFrameHandler(_heap.RemoveMin());
                Interlocked.Decrement(ref _count);
            }

            _loss.Clear();
            _complete = false;
            SequenceNumber = 0;
        }

        private bool IsComplete()
        {
            return _complete && _heap.Count == 0;
        }

        public class VoicePacketComparer
            : IComparer<VoicePacket>
        {
            public int Compare(VoicePacket x, VoicePacket y)
            {
                return x.SequenceNumber.CompareTo(y.SequenceNumber);
            }
        }
    }
}