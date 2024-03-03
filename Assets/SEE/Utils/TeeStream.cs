using System;
using System.IO;

namespace SEE.Utils
{
    /// <summary>
    /// A stream that writes to another stream in parallel.
    /// This is analogous to the Unix `tee` command.
    /// </summary>
    /// <remarks>
    /// The code for this class has been created with the help of GPT-4.
    /// </remarks>
    public class TeeStream : Stream
    {
        /// <summary>
        /// The primary stream, which is the stream to read from.
        /// </summary>
        private readonly Stream _primaryStream;

        /// <summary>
        /// The secondary stream to write the <see cref="_primaryStream"/> to.
        /// </summary>
        private readonly Stream _secondaryStream;

        /// <summary>
        /// Creates a new instance of the <see cref="TeeStream"/> class.
        /// </summary>
        /// <param name="primaryStream">The primary stream to read from.</param>
        /// <param name="secondaryStream">The secondary stream to write to.</param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="primaryStream"/> or <paramref name="secondaryStream"/> is <c>null</c>.
        /// </exception>
        public TeeStream(Stream primaryStream, Stream secondaryStream)
        {
            _primaryStream = primaryStream ?? throw new ArgumentNullException(nameof(primaryStream));
            _secondaryStream = secondaryStream ?? throw new ArgumentNullException(nameof(secondaryStream));
        }

        public override bool CanRead => _primaryStream.CanRead;
        public override bool CanSeek => _primaryStream.CanSeek;
        public override bool CanWrite => _primaryStream.CanWrite;
        public override long Length => _primaryStream.Length;

        public override long Position
        {
            get => _primaryStream.Position;
            set => _primaryStream.Position = value;
        }

        public override void Flush()
        {
            _primaryStream.Flush();
            _secondaryStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = _primaryStream.Read(buffer, offset, count);
            _secondaryStream.Write(buffer, offset, bytesRead);
            _secondaryStream.Flush(); // Make sure everything is immediately logged
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _primaryStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _primaryStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _primaryStream.Write(buffer, offset, count);
            _secondaryStream.Write(buffer, offset, count);
        }
    }

}
