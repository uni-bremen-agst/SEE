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
        private readonly Stream primaryStream;

        /// <summary>
        /// The secondary stream to write the <see cref="primaryStream"/> to.
        /// </summary>
        private readonly Stream secondaryStream;

        /// <summary>
        /// Creates a new instance of the <see cref="TeeStream"/> class.
        /// </summary>
        /// <param name="primaryStream">The primary stream to read from.</param>
        /// <param name="secondaryStream">The secondary stream to write to.</param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="primaryStream"/> or <paramref name="secondaryStream"/> is null.
        /// </exception>
        public TeeStream(Stream primaryStream, Stream secondaryStream)
        {
            this.primaryStream = primaryStream ?? throw new ArgumentNullException(nameof(primaryStream));
            this.secondaryStream = secondaryStream ?? throw new ArgumentNullException(nameof(secondaryStream));
        }

        public override bool CanRead => primaryStream.CanRead;
        public override bool CanSeek => primaryStream.CanSeek;
        public override bool CanWrite => primaryStream.CanWrite;
        public override long Length => primaryStream.Length;

        public override long Position
        {
            get => primaryStream.Position;
            set => primaryStream.Position = value;
        }

        public override void Flush()
        {
            primaryStream.Flush();
            secondaryStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = primaryStream.Read(buffer, offset, count);
            secondaryStream.Write(buffer, offset, bytesRead);
            secondaryStream.Flush(); // Make sure everything is immediately logged
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return primaryStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            primaryStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            primaryStream.Write(buffer, offset, count);
            secondaryStream.Write(buffer, offset, count);
        }
    }

}
