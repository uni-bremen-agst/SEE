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
        private readonly LockedValue<WaveFileWriter> _lock;

        public AudioFileWriter(string filename, [NotNull] WaveFormat format)
        {
            if (filename == null)
                throw new ArgumentNullException("filename");
            if (format == null)
                throw new ArgumentNullException("format");

            if (string.IsNullOrEmpty(Path.GetExtension(filename)))
                filename += ".wav";

            var directory = Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            _lock = new LockedValue<WaveFileWriter>(
                new WaveFileWriter(File.Open(filename, FileMode.CreateNew), format)
            );
        }

        public void Dispose()
        {
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
            using (var writer = _lock.Lock())
            {
                var v = writer.Value;
                if (v != null)
                    v.Flush();
            }
        }

        public void WriteSamples(ArraySegment<float> samples)
        {
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
