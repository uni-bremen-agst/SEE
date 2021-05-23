using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Memory cache stream.</summary>
   public class MemoryCacheStream : System.IO.Stream
   {
      #region Variables

      /// <summary>The cache as byte[]</summary>
      private byte[] cache;

      /// <summary>The write position within the stream.</summary>
      private long writePosition;

      /// <summary>The read position within the stream.</summary>
      private long readPosition;

      /// <summary>Stream length. Indicates where the end of the stream is.</summary>
      private long length;

      /// <summary>Cache size in bytes</summary>
      private int size;

      /// <summary>Maximum cache size in bytes</summary>
      private readonly int maxSize;

      #endregion


      #region Constructors

      /// <summary>Constructor with a specified cache size.</summary>
      /// <param name="cacheSize">Cache size of the stream in bytes.</param>
      /// <param name="maxCacheSize">Maximum cache size of the stream in bytes.</param>
      public MemoryCacheStream(int cacheSize = 64 * BaseConstants.FACTOR_KB, int maxCacheSize = 64 * BaseConstants.FACTOR_MB)
      {
         length = writePosition = readPosition = 0;
         size = cacheSize;
         maxSize = maxCacheSize;

         createCache();

         //Debug.Log("MemoryCacheStream: " + cacheSize + "-" + maxCacheSize);
      }

      #endregion


      #region Stream Overrides [Properties]

      /// <summary>Gets a flag flag that indicates if the stream is readable (always true).</summary>
      public override bool CanRead => true;

      /// <summary>Gets a flag flag that indicates if the stream is seekable (always true).</summary>
      public override bool CanSeek => true;

      /// <summary>Gets a flag flag that indicates if the stream is seekable (always true).</summary>
      public override bool CanWrite => true;

      /// <summary>Gets or sets the current stream position.</summary>
      public override long Position
      {
         get => readPosition;

         set
         {
            if (value < 0L)
            {
               throw new System.ArgumentOutOfRangeException(nameof(value), "Non-negative number required.");
            }

            //writePosition = readPosition = value; //TODO only readPosition?
            readPosition = value;
         }
      }

      /// <summary>Gets the current stream length.</summary>
      public override long Length => length;

      #endregion


      #region Stream Overrides [Methods]

      public override void Flush()
      {
         // Memory based stream with nothing to flush; Do nothing.
      }

      public override long Seek(long offset, System.IO.SeekOrigin origin)
      {
         switch (origin)
         {
            case System.IO.SeekOrigin.Begin:
            {
               Position = (int)offset;
               break;
            }
            case System.IO.SeekOrigin.Current:
            {
               long newPos = unchecked(Position + offset);
               if (newPos < 0L)
                  throw new System.IO.IOException("An attempt was made to move the position before the beginning of the stream.");
               Position = newPos;
               break;
            }
            case System.IO.SeekOrigin.End:
            {
               long newPos = unchecked(length + offset);
               if (newPos < 0L)
                  throw new System.IO.IOException("An attempt was made to move the position before the beginning of the stream.");
               Position = newPos;
               break;
            }
            default:
            {
               throw new System.ArgumentException("Invalid seek origin.");
            }
         }

         return Position;
      }

      public override void SetLength(long value)
      {
         int _size = (int)value;

         if (size != _size)
         {
            size = _size;
            length = Position = 0;

            createCache();
         }
      }

      public override int Read(byte[] buffer, int offset, int count)
      {
         if (offset < 0)
            throw new System.ArgumentOutOfRangeException(nameof(offset), "Non-negative number required.");
         if (count < 0)
            throw new System.ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
         if (buffer.Length - offset < count)
         {
            throw new System.ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
         }

         // Test for end of stream (or beyond end)
         return readPosition >= length ? 0 : read(buffer, offset, count);
      }

      public override void Write(byte[] buffer, int offset, int count)
      {
         if (offset < 0)
            throw new System.ArgumentOutOfRangeException(nameof(offset), "Non-negative number required.");
         if (count < 0)
            throw new System.ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
         if (count > size)
            throw new System.ArgumentOutOfRangeException(nameof(count), "Value is larger than the cache size.");
         if (buffer.Length - offset < count)
         {
            throw new System.ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
         }

         if (0 == count)
         {
            // Nothing to do.
            return;
         }

         write(buffer, offset, count);
      }

      #endregion


      #region Private methods

      /// <summary>Read bytes from the memory stream into the provided buffer.</summary>
      private int read(byte[] buff, int offset, int count)
      {
         int arrayPosition = (int)(readPosition % size);

         if (arrayPosition + count > size)
         {
            int countEnd = size - arrayPosition;
            int countStart = count - countEnd;

            System.Array.Copy(cache, arrayPosition, buff, offset, countEnd);

            System.Array.Copy(cache, 0, buff, offset + countEnd, countStart);
         }
         else
         {
            System.Array.Copy(cache, arrayPosition, buff, offset, count);
         }

         readPosition += count;
         return count;
      }

      /// <summary>Write bytes into the memory stream.</summary>
      private void write(byte[] buff, int offset, int count)
      {
         int arrayPosition = (int)(writePosition % size);

         if (arrayPosition + count > size)
         {
            int countEnd = size - arrayPosition;
            int countStart = count - countEnd;

            System.Array.Copy(buff, offset, cache, arrayPosition, countEnd);

            System.Array.Copy(buff, offset + countEnd, cache, 0, countStart);
         }
         else
         {
            System.Array.Copy(buff, offset, cache, arrayPosition, count);
         }

         writePosition += count;

         length = writePosition;
      }

      /// <summary>Create the cache</summary>
      private void createCache()
      {
         if (size > maxSize)
         {
            cache = new byte[maxSize];
            Debug.LogWarning("'size' is larger than 'maxSize'! Using 'maxSize' as cache!");
         }
         else
         {
            cache = new byte[size];
         }
      }

      #endregion
   }
}
// © 2016-2021 crosstales LLC (https://www.crosstales.com)