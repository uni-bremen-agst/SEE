using System;
using System.Collections.Generic;
using Dissonance.Datastructures;
using JetBrains.Annotations;

namespace Dissonance.Audio.Playback
{
    internal static class DecoderPipelinePool
    {
        private static readonly Dictionary<FrameFormat, ConcurrentPool<DecoderPipeline>> Pools = new Dictionary<FrameFormat, ConcurrentPool<DecoderPipeline>>();

        private static int _nextPipelineId;

        [NotNull] private static ConcurrentPool<DecoderPipeline> GetPool(FrameFormat format)
        {
            ConcurrentPool<DecoderPipeline> pool;
            if (!Pools.TryGetValue(format, out pool))
            {
                pool = new ConcurrentPool<DecoderPipeline>(3, () => {
                    var decoder = DecoderFactory.Create(format);

                    var uuid = _nextPipelineId.ToString();
                    return new DecoderPipeline(decoder, format.FrameSize, p => {
                        p.Reset();
                        Recycle(format, p);
                    }, uuid);
                });
                Pools[format] = pool;

                _nextPipelineId++;
            }

            return pool;
        }

        [NotNull] internal static DecoderPipeline GetDecoderPipeline(FrameFormat format, [NotNull] IVolumeProvider volume)
        {
            if (volume == null)
                throw new ArgumentNullException("volume");

            var pool = GetPool(format);
            var pipeline = pool.Get();
            pipeline.Reset();

            pipeline.VolumeProvider = volume;

            return pipeline;
        }

        private static void Recycle(FrameFormat format, [CanBeNull] DecoderPipeline pipeline)
        {
            if (pipeline == null)
                return;

            GetPool(format).Put(pipeline);
        }
    }
}
