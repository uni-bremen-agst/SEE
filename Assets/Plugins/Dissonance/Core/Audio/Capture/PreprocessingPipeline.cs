//using System;
//using Dissonance.VAD;
//using NAudio.Wave;

//namespace Dissonance.Audio.Capture
//{
//    internal class PreprocessingPipeline
//        : BasePreprocessingPipeline
//    {
//        #region fields and properties
//        private static readonly Log Log = Logs.Create(LogCategory.Recording, typeof(PreprocessingPipeline).Name);
        
//        private bool _subActive;
//        private bool _vadActive;
//        private bool _vadDirty;

//        private readonly Preprocessor _speex;

//        private readonly short[] _int16Frame;

//        public override bool RequiresInput
//        {
//            get { return _vadActive || _subActive; }
//        }

//        private readonly IVoiceDetector _vad;
//        protected override bool VadIsSpeechDetected
//        {
//            get { return _vad.IsSpeaking; }
//        }
//        #endregion

//        #region constructor
//        public PreprocessingPipeline(WaveFormat inputFormat)
//            : base(inputFormat, CalculateInputFrameSize(inputFormat), 960, 48000, 960, 48000)
//        {
//            //Pipeline runs at 48kHz
//            _speex = new Preprocessor(OutputFrameSize, 48000);
//            _vad = new WebRtcVoiceDetector((uint)OutputFrameSize, 48000);
//            _int16Frame = new short[OutputFrameSize];
//        }

//        private static int CalculateInputFrameSize(WaveFormat inputFormat)
//        {
//            //Process input data in 20ms frames
//            return (int)(inputFormat.SampleRate * 0.02f);
//        }
//        #endregion

//        #region disposal
//        public override void Dispose()
//        {
//            base.Dispose();

//            _speex.Dispose();
//        }
//        #endregion

//        protected override void ApplyReset()
//        {
//            _vad.Reset();
//            _speex.Reset();

//            base.ApplyReset();
//        }

//        protected override void PreprocessAudioFrame(float[] buffer)
//        {
//            //Convert the audio to int16 format for VAD and preprocessing
//            FormatConverter.ConvertFloat32ToInt16(new ArraySegment<float>(buffer), new ArraySegment<short>(_int16Frame));

//            //Process the buffer with VAD, sets the IsSpeaking flag on the VAD
//            ProcessVAD(_int16Frame);

//            //Preprocess the audio (AGC, noise removal)
//            _speex.Process(new ArraySegment<short>(_int16Frame));

//            //Convert back to float format
//            FormatConverter.ConvertInt16ToFloat32(new ArraySegment<short>(_int16Frame), new ArraySegment<float>(buffer));

//            //Send the processed audio to subscribers
//            SendSamplesToSubscribers(buffer);
//        }

//        /// <summary>
//        /// Preprocess a buffer of audio. Updates the VAD (if necessary).
//        /// </summary>
//        /// <param name="buffer"></param>
//        /// <returns>true, if the audio should be delayed by a frame. Otherwise false</returns>
//        private void ProcessVAD(short[] buffer)
//        {
//            if (VadSubscriptionCount > 0)
//            {
//                if (_vadDirty)
//                {
//                    _vadDirty = false;
//                    _vad.Reset();

//                    Log.Trace("Reset VAD");
//                }

//                _vad.Handle(new ArraySegment<short>(buffer));
//            }
//        }

//        #region subscriptions
//        public override void Subscribe(IMicrophoneHandler listener)
//        {
//            base.Subscribe(listener);

//            _subActive = true;
//        }

//        public override bool Unsubscribe(IMicrophoneHandler listener)
//        {
//            var removed = base.Unsubscribe(listener);

//            _subActive = MicSubscriptionCount > 0;

//            return removed;
//        }

//        public override void Subscribe(IVoiceActivationListener listener)
//        {
//            _vadActive = true;
//            _vadDirty = true;

//            base.Subscribe(listener);
//        }

//        public override bool Unsubscribe(IVoiceActivationListener listener)
//        {
//            _vadDirty = true;
//            _vadActive = VadSubscriptionCount > 0;

//            return base.Unsubscribe(listener);
//        }
//        #endregion
//    }
//}
