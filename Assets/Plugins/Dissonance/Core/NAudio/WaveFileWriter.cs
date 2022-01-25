using System;
using System.IO;

// Justification: copied from NAudio, and we want to make the minimal changes possible
// ReSharper disable All

namespace NAudio.Wave
{
    /// <summary>
    /// This class writes WAV data to a .wav file on disk
    /// </summary>
    internal class WaveFileWriter : Stream
    {
        private Stream _outStream;
        private BinaryWriter _writer;
        private long _dataSizePos;
        private long _factSampleCountPos;
        private int _dataChunkSize;

        /// <summary>
        /// WaveFileWriter that actually writes to a stream
        /// </summary>
        /// <param name="outStream">Stream to be written to</param>
        /// <param name="format">Wave format to use</param>
        public WaveFileWriter(Stream outStream, WaveFormat format)
        {
            _outStream = outStream;
            WaveFormat = format;
            _writer = new BinaryWriter(outStream, System.Text.Encoding.ASCII);
            _writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            _writer.Write((int)0); // placeholder
            _writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            _writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            //format.Serialize(this.writer);
            //Replaced above with following code (copied in directly from WaveFormat class in NAudio)
            _writer.Write((int)(18)); // wave format length
            _writer.Write((short)0x3);   //encoding:IeeeFloat
            _writer.Write((short)format.Channels);     //channels
            _writer.Write((int)format.SampleRate);   //sample rate
            _writer.Write((int)format.SampleRate * 4);   //average bytes per second
            _writer.Write((short)4); //block align
            _writer.Write((short)32);//bits per sample
            _writer.Write((short)0);

            CreateFactChunk();
            WriteDataChunkHeader();
        }

        /// <summary>
        /// Creates a new WaveFileWriter
        /// </summary>
        /// <param name="filename">The filename to write to</param>
        /// <param name="format">The Wave Format of the output data</param>
        public WaveFileWriter(string filename, WaveFormat format)
            : this(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read), format)
        {
            Filename = filename;
        }

        private void WriteDataChunkHeader()
        {
            _writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            _dataSizePos = _outStream.Position;
            _writer.Write((int)0); // placeholder
        }

        private void CreateFactChunk()
        {
            if (HasFactChunk())
            {
                _writer.Write(System.Text.Encoding.ASCII.GetBytes("fact"));
                _writer.Write((int)4);
                _factSampleCountPos = _outStream.Position;
                _writer.Write((int)0); // number of samples
            }
        }

        private bool HasFactChunk()
        {
            //return this.format.Encoding != WaveFormatEncoding.Pcm && this.format.BitsPerSample != 0;
            return true;    //assuming encoding == ieeefloat and bits == 32
        }

        /// <summary>
        /// The wave file name or null if not applicable
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Number of bytes of audio in the data chunk
        /// </summary>
        public override long Length
        {
            get { return _dataChunkSize; }
        }

        /// <summary>
        /// WaveFormat of this wave file
        /// </summary>
        public WaveFormat WaveFormat { get; private set; }

        /// <summary>
        /// Returns false: Cannot read from a WaveFileWriter
        /// </summary>
        public override bool CanRead
        {
            get { return false; }
        }

        /// <summary>
        /// Returns true: Can write to a WaveFileWriter
        /// </summary>
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <summary>
        /// Returns false: Cannot seek within a WaveFileWriter
        /// </summary>
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <summary>
        /// Read is not supported for a WaveFileWriter
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Cannot read from a WaveFileWriter");
        }

        /// <summary>
        /// Seek is not supported for a WaveFileWriter
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException("Cannot seek within a WaveFileWriter");
        }

        /// <summary>
        /// SetLength is not supported for WaveFileWriter
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            throw new InvalidOperationException("Cannot set length of a WaveFileWriter");
        }

        /// <summary>
        /// Gets the Position in the WaveFile (i.e. number of bytes written so far)
        /// </summary>
        public override long Position
        {
            get { return _dataChunkSize; }
            set { throw new InvalidOperationException("Repositioning a WaveFileWriter is not supported"); }
        }

        ///// <summary>
        ///// Appends bytes to the WaveFile (assumes they are already in the correct format)
        ///// </summary>
        ///// <param name="data">the buffer containing the wave data</param>
        ///// <param name="offset">the offset from which to start writing</param>
        ///// <param name="count">the number of bytes to write</param>
        //[Obsolete("Use Write instead")]
        //public void WriteData(byte[] data, int offset, int count)
        //{
        //    Write(data, offset, count);
        //}

        /// <summary>
        /// Appends bytes to the WaveFile (assumes they are already in the correct format)
        /// </summary>
        /// <param name="data">the buffer containing the wave data</param>
        /// <param name="offset">the offset from which to start writing</param>
        /// <param name="count">the number of bytes to write</param>
        public override void Write(byte[] data, int offset, int count)
        {
            _outStream.Write(data, offset, count);
            _dataChunkSize += count;
        }

        //private byte[] value24 = new byte[3]; // keep this around to save us creating it every time

        /// <summary>
        /// Writes a single sample to the Wave file
        /// </summary>
        /// <param name="sample">the sample to write (assumed floating point with 1.0f as max value)</param>
        public void WriteSample(float sample)
        {
            //if (WaveFormat.BitsPerSample == 16)
            //{
            //    writer.Write((Int16)(Int16.MaxValue * sample));
            //    dataChunkSize += 2;
            //}
            //else if (WaveFormat.BitsPerSample == 24)
            //{
            //    var value = BitConverter.GetBytes((Int32)(Int32.MaxValue * sample));
            //    value24[0] = value[1];
            //    value24[1] = value[2];
            //    value24[2] = value[3];
            //    writer.Write(value24);
            //    dataChunkSize += 3;
            //}
            //else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Extensible)
            //{
            //    writer.Write(UInt16.MaxValue * (Int32)sample);
            //    dataChunkSize += 4;
            //}
            //else if (WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            //{
                _writer.Write(sample);
                _dataChunkSize += 4;
            //}
            //else
            //{
            //    throw new ApplicationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            //}
        }

        /// <summary>
        /// Writes 32 bit floating point samples to the Wave file
        /// They will be converted to the appropriate bit depth depending on the WaveFormat of the WAV file
        /// </summary>
        /// <param name="samples">The buffer containing the floating point samples</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of floating point samples to write</param>
        public void WriteSamples(float[] samples, int offset, int count)
        {
            for (int n = 0; n < count; n++)
            {
                WriteSample(samples[offset + n]);
            }
        }

        ///// <summary>
        ///// Writes 16 bit samples to the Wave file
        ///// </summary>
        ///// <param name="samples">The buffer containing the 16 bit samples</param>
        ///// <param name="offset">The offset from which to start writing</param>
        ///// <param name="count">The number of 16 bit samples to write</param>
        //[Obsolete("Use WriteSamples instead")]
        //public void WriteData(short[] samples, int offset, int count)
        //{
        //    WriteSamples(samples, offset, count);
        //}


        ///// <summary>
        ///// Writes 16 bit samples to the Wave file
        ///// </summary>
        ///// <param name="samples">The buffer containing the 16 bit samples</param>
        ///// <param name="offset">The offset from which to start writing</param>
        ///// <param name="count">The number of 16 bit samples to write</param>
        //public void WriteSamples(short[] samples, int offset, int count)
        //{
        //    // 16 bit PCM data
        //    if (WaveFormat.BitsPerSample == 16)
        //    {
        //        for (int sample = 0; sample < count; sample++)
        //        {
        //            writer.Write(samples[sample + offset]);
        //        }
        //        dataChunkSize += (count * 2);
        //    }
        //    // 24 bit PCM data
        //    else if (WaveFormat.BitsPerSample == 24)
        //    {
        //        byte[] value;
        //        for (int sample = 0; sample < count; sample++)
        //        {
        //            value = BitConverter.GetBytes(UInt16.MaxValue * (Int32)samples[sample + offset]);
        //            value24[0] = value[1];
        //            value24[1] = value[2];
        //            value24[2] = value[3];
        //            writer.Write(value24);
        //        }
        //        dataChunkSize += (count * 3);
        //    }
        //    // 32 bit PCM data
        //    else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Extensible)
        //    {
        //        for (int sample = 0; sample < count; sample++)
        //        {
        //            writer.Write(UInt16.MaxValue * (Int32)samples[sample + offset]);
        //        }
        //        dataChunkSize += (count * 4);
        //    }
        //    // IEEE float data
        //    else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
        //    {
        //        for (int sample = 0; sample < count; sample++)
        //        {
        //            writer.Write((float)samples[sample + offset] / (float)(Int16.MaxValue + 1));
        //        }
        //        dataChunkSize += (count * 4);
        //    }
        //    else
        //    {
        //        throw new ApplicationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
        //    }
        //}

        /// <summary>
        /// Ensures data is written to disk
        /// </summary>
        public override void Flush()
        {
            _writer.Flush();
        }

        #region IDisposable Members

        /// <summary>
        /// Actually performs the close,making sure the header contains the correct data
        /// </summary>
        /// <param name="disposing">True if called from <see>Dispose</see></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_outStream != null)
                {
                    try
                    {
                        UpdateHeader(_writer);
                    }
                    finally
                    {
#if NETFX_CORE
                        _outStream.Dispose();
#else
                        // in a finally block as we don't want the FileStream to run its disposer in
                        // the GC thread if the code above caused an IOException (e.g. due to disk full)
                        _outStream.Close(); // will close the underlying base stream
#endif
                        _outStream = null;

                    }
                }
            }
        }

        /// <summary>
        /// Updates the header with file size information
        /// </summary>
        protected virtual void UpdateHeader(BinaryWriter writer)
        {
            Flush();
            UpdateRiffChunk(writer);
            UpdateFactChunk(writer);
            UpdateDataChunk(writer);
        }

        private void UpdateDataChunk(BinaryWriter writer)
        {
            writer.Seek((int)_dataSizePos, SeekOrigin.Begin);
            writer.Write((int)(_dataChunkSize));
        }

        private void UpdateRiffChunk(BinaryWriter writer)
        {
            writer.Seek(4, SeekOrigin.Begin);
            writer.Write((int)(_outStream.Length - 8));
        }

        private void UpdateFactChunk(BinaryWriter writer)
        {
            if (HasFactChunk())
            {
                int bitsPerSample = 32 * WaveFormat.Channels;//(format.BitsPerSample * format.Channels);
                if (bitsPerSample != 0)
                {
                    writer.Seek((int)_factSampleCountPos, SeekOrigin.Begin);
                    writer.Write((int)((_dataChunkSize * 8) / bitsPerSample));
                }
            }
        }

        /// <summary>
        /// Finaliser - should only be called if the user forgot to close this WaveFileWriter
        /// </summary>
        ~WaveFileWriter()
        {
            Dispose(false);
        }

#endregion
    }
}