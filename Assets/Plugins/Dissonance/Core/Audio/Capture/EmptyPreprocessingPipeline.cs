using JetBrains.Annotations;
using NAudio.Wave;

namespace Dissonance.Audio.Capture
{
    internal class EmptyPreprocessingPipeline
        : BasePreprocessingPipeline
    {
        public EmptyPreprocessingPipeline([NotNull] WaveFormat inputFormat)
            : base(inputFormat, 480, 48000, 480, 48000)
        {
        }

        public override bool IsOutputMuted
        {
            set { }
        }

        protected override bool VadIsSpeechDetected
        {
            get { return true; }
        }

        protected override void PreprocessAudioFrame(float[] frame)
        {
            SendSamplesToSubscribers(frame);
        }
    }
}
