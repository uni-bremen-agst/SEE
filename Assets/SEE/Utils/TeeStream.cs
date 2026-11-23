using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

        /// <summary>
        /// Whether the <see cref="primaryStream"/> supports reading.
        /// </summary>
        public override bool CanRead => primaryStream.CanRead;

        /// <summary>
        /// Whether the <see cref="primaryStream"/> supports seeking.
        /// </summary>
        public override bool CanSeek => primaryStream.CanSeek;

        /// <summary>
        /// Whether the <see cref="primaryStream"/> supports writing.
        /// </summary>
        public override bool CanWrite => primaryStream.CanWrite;

        /// <summary>
        /// The length in bytes of the <see cref="primaryStream"/>.
        /// </summary>
        public override long Length => primaryStream.Length;

        /// <summary>
        /// The position within the <see cref="primaryStream"/>.
        /// The position of the <see cref="secondaryStream"/> is not affected.
        /// </summary>
        public override long Position
        {
            get => primaryStream.Position;
            set => primaryStream.Position = value;
        }

        /// <summary>
        /// Flushes both the <see cref="primaryStream"/> and the <see cref="secondaryStream"/>.
        /// </summary>
        public override void Flush()
        {
            primaryStream.Flush();
            secondaryStream.Flush();
        }

        /// <summary>
        /// Asynchronously flushes the <see cref="primaryStream"/> and then the <see cref="secondaryStream"/>.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous flush operation.</returns>
        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await primaryStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            await secondaryStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously reads up to <paramref name="count"/> bytes from the <see cref="primaryStream"/>
        /// and writes the bytes actually read to the <see cref="secondaryStream"/>, which is flushed
        /// immediately afterwards.
        /// </summary>
        /// <param name="buffer">The buffer to write the data read into.</param>
        /// <param name="offset">The offset in <paramref name="buffer"/> at which to start writing the data read.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The number of bytes read, which is 0 if the end of the <see cref="primaryStream"/> has been reached.</returns>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
                                                 CancellationToken cancellationToken)
        {
            int bytesRead = await primaryStream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            await secondaryStream.WriteAsync(buffer, offset, bytesRead, cancellationToken).ConfigureAwait(false);
            await secondaryStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            return bytesRead;
        }

        /// <summary>
        /// Asynchronously writes <paramref name="count"/> bytes to the <see cref="primaryStream"/> and then to
        /// the <see cref="secondaryStream"/>, which is flushed immediately afterwards.
        /// </summary>
        /// <param name="buffer">The buffer containing the data to write.</param>
        /// <param name="offset">The offset in <paramref name="buffer"/> at which the data to write starts.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous write operation.</returns>
        public override async Task WriteAsync(byte[] buffer, int offset, int count,
                                              CancellationToken cancellationToken)
        {
            await primaryStream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            await secondaryStream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            await secondaryStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads up to <paramref name="count"/> bytes from the <see cref="primaryStream"/> and writes the
        /// bytes actually read to the <see cref="secondaryStream"/>, which is flushed immediately afterwards.
        /// </summary>
        /// <param name="buffer">The buffer to write the data read into.</param>
        /// <param name="offset">The offset in <paramref name="buffer"/> at which to start writing the data read.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The number of bytes read, which is 0 if the end of the <see cref="primaryStream"/> has been reached.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = primaryStream.Read(buffer, offset, count);
            secondaryStream.Write(buffer, offset, bytesRead);
            secondaryStream.Flush(); // Make sure everything is immediately logged
            return bytesRead;
        }

        /// <summary>
        /// Sets the position within the <see cref="primaryStream"/>.
        /// The position of the <see cref="secondaryStream"/> is not affected.
        /// </summary>
        /// <param name="offset">The byte offset relative to <paramref name="origin"/>.</param>
        /// <param name="origin">The reference point for <paramref name="offset"/>.</param>
        /// <returns>The new position within the <see cref="primaryStream"/>.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return primaryStream.Seek(offset, origin);
        }

        /// <summary>
        /// Sets the length of the <see cref="primaryStream"/>.
        /// The length of the <see cref="secondaryStream"/> is not affected.
        /// </summary>
        /// <param name="value">The new length in bytes.</param>
        public override void SetLength(long value)
        {
            primaryStream.SetLength(value);
        }

        /// <summary>
        /// Writes <paramref name="count"/> bytes to the <see cref="primaryStream"/> and then to the
        /// <see cref="secondaryStream"/>.
        /// </summary>
        /// <param name="buffer">The buffer containing the data to write.</param>
        /// <param name="offset">The offset in <paramref name="buffer"/> at which the data to write starts.</param>
        /// <param name="count">The number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            primaryStream.Write(buffer, offset, count);
            secondaryStream.Write(buffer, offset, count);
        }
    }

}
