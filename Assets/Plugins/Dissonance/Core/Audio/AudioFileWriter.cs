using System;
using System.IO;
using Dissonance.Threading;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Dissonance.Audio
{
    internal class AudioFileWriter
        : IDisposable
    {
        private static readonly Log Log = Logs.Create(LogCategory.Core, typeof(AudioFileWriter).Name);

        private readonly LockedValue<WaveFileWriter> _lock;
        private readonly bool _error;

        public AudioFileWriter(string filename, [NotNull] WaveFormat format)
        {
            if (filename == null)
                throw new ArgumentNullException(nameof(filename));
            if (format == null)
                throw new ArgumentNullException(nameof(format));

            if (string.IsNullOrEmpty(Path.GetExtension(filename)))
                filename += ".wav";

            try
            {
                var directory = Path.GetDirectoryName(filename);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                _lock = new LockedValue<WaveFileWriter>(
                    new WaveFileWriter(File.Open(filename, FileMode.CreateNew), format)
                );
            }
            catch (Exception ex)
            {
                Log.Error($"Attempting to create `AudioFileWriter` failed (audio logging will be disabled). This is often caused by a lack of permission to write to the specified directory.\nException: {ex}");
                _error = true;
            }
        }

        public void Dispose()
        {
            if (_error)
                return;

            using (var writer = _lock.Lock())
            {
                var v = writer.Value;
                if (v != null)
                    v.Dispose();

                writer.Value = null;
            }
        }

        public void Flush()
        {
            if (_error)
                return;

            using (var writer = _lock.Lock())
            {
                var v = writer.Value;
                if (v != null)
                    v.Flush();
            }
        }

        public void WriteSamples(ArraySegment<float> samples)
        {
            if (_error)
                return;

            using (var writer = _lock.Lock())
            {
                var v = writer.Value;
                if (v == null)
                    return;

                v.WriteSamples(samples.Array, samples.Offset, samples.Count);
            }
        }
    }
}
