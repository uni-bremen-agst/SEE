using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.Utilities;
using Mediapipe.Tasks.Components.Containers;


namespace SEE.Game.Avatars
{
    /// <summary>
    /// Includes functionality for using the so-calles One Euro Filter,
    /// which is used to improve (smooth) animations based on the MediaPipe landmarks.
    /// </summary>
    /// <remarks>The animation itself and use of the filter occurs using the functions of the class <see cref="HandsAnimator"/>.</remarks>
    public class OneEuroFilter
    {
        /// <summary>
        /// Adaptive smoothing factor of the filter.
        /// </summary>
        private float smoothingFactor = 1f;

        /// <summary>
        /// Sampling period of a signal to be filtered.
        /// </summary>
        private float samplingPeriod = 0f;

        /// <summary>
        /// The cutoff frequency of the filter.
        /// Its goal is to stabilize the signal by reducing jitter.
        /// </summary>
        private Vector3 cutoffFrequency = Vector3.zero;

        /// <summary>
        /// The minimum possible value for the <see cref="cutoffFrequency"/>.
        /// </summary>
        private static Vector3 minimumCutOffFrequency = new Vector3(0.03f, 0.03f, 0.03f);

        /// <summary>
        /// Filter speed coefficient used to compute the new value of the
        /// <see cref="cutoffFrequency"/>.
        /// </summary>
        private static float beta = 0.9f;

        /// <summary>
        /// Stores the previous output of the filter.
        /// </summary>
        private Vector3 prevFilteredValue;

        /// <summary>
        /// Stores the new output of the filter.
        /// </summary>
        private Vector3 latestFilteredValue;

        /// <summary>
        /// Stores the previous derivative of the signal.
        /// </summary>
        private Vector3 prevFilteredDerivative;

        /// <summary>
        /// Indicates whether the filter is being applied for the first time,
        /// meaning there are no previous values.
        /// </summary>
        private bool isFirstApplicationOfTheFilter = true;

        /// <summary>
        /// Initializes a new instance of the One Euro filter and sets internal
        /// state values to their initial defaults.
        /// </summary>
        public OneEuroFilter()
        {
            prevFilteredDerivative = Vector3.zero;
            prevFilteredValue = Vector3.zero;
        }

        /// <summary>
        /// Sets the sampling period used by the filter based on the provided sampling timestamps.
        /// </summary>
        /// <param name="samplingTimes">A list of recent sampling timestamps used to estimate the sampling period.</param>
        private void SetSamplingPeriod(List<float> samplingTimes)
        {
            if (samplingTimes.Count == 1)
            {
                samplingPeriod = samplingTimes[0] + 0.001f;
            }
            else
            {
                samplingPeriod = samplingTimes.Last() - samplingTimes[^2];
            }
        }

        /// <summary>
        /// Sets the smoothing factor used by the filter.
        /// </summary>
        private void SetSmoothingFactor()
        {
            var r = 2 * Mathf.PI * cutoffFrequency.x * samplingPeriod;
            smoothingFactor = r / (r + 1);
        }

        /// <summary>
        /// Applies exponential smoothing to a value using the smoothing factor and the previous output of the filter.
        /// </summary>
        /// <param name="smoothingFactor">The current smoothing factor of the filter.</param>
        /// <param name="newSignaValue">The current raw input value to be smoothed.</param>
        /// <param name="filteredPreviousValue">The previously smoothed value used as the recursive reference.</param>
        /// <returns></returns>
        private Vector3 ExponentialSmoothing(float smoothingFactor, Vector3 newSignaValue, Vector3 filteredPreviousValue)
        {
            return (smoothingFactor * newSignaValue + (1 - smoothingFactor) * filteredPreviousValue);
        }

        /// <summary>
        /// Applies the One Euro Filter to a new incoming MediaPipe landmark value.
        /// </summary>
        /// <param name="samplingTimes">A list of recent sampling timestamps used to estimate the sampling period.</param>
        /// <param name="newSignalValue">The current raw input value to be smoothed.</param>
        /// <returns></returns>
        public Vector3 ApplyFilterToHandLandmark(List<float> samplingTimes, Landmark newSignalValue)
        {
            // Convert MediaPipe landmark coordinates into a Vector3 representation.
            Vector3 latestValue = new Vector3(newSignalValue.x, newSignalValue.y, 0);

            SetSamplingPeriod(samplingTimes);

            float smoothingFactorOfTheDerivative = 2 * Mathf.PI * samplingPeriod;
            smoothingFactorOfTheDerivative = smoothingFactorOfTheDerivative / (smoothingFactorOfTheDerivative + 1);

            if (samplingPeriod == 0)
            {
                samplingPeriod = 0.001f;
            }

            Vector3 signalDerivative = (latestValue - prevFilteredValue) / samplingPeriod;

            if (isFirstApplicationOfTheFilter)
            {
                isFirstApplicationOfTheFilter = false;
                signalDerivative = Vector3.zero;
            }

            if (prevFilteredDerivative == Vector3.zero)
            {
                prevFilteredDerivative = signalDerivative;
                prevFilteredValue = latestValue;
            }

            var filteredDerivative = ExponentialSmoothing(smoothingFactorOfTheDerivative, signalDerivative, prevFilteredDerivative);
            prevFilteredDerivative = filteredDerivative;

            cutoffFrequency = minimumCutOffFrequency + beta * filteredDerivative.Abs();

            SetSmoothingFactor();

            var newFilteredValue = ExponentialSmoothing(smoothingFactor, latestValue, prevFilteredValue);
            latestFilteredValue = newFilteredValue;
            prevFilteredValue = latestFilteredValue;

            return newFilteredValue;
        }

        /// <summary>
        /// Applies the One Euro Filter to a new incoming MediaPipe hand position value.
        /// </summary>
        /// <param name="samplingTimes">A list of recent sampling timestamps used to estimate the sampling period.</param>
        /// <param name="newHandPosition">The new hand position from MediaPipe to be smoothed.</param>
        /// <returns></returns>
        public Vector3 ApplyFilterToHandPosition(List<float> samplingTimes, Vector3 newHandPosition)
        {
            SetSamplingPeriod(samplingTimes);

            float smoothingFactorOfTheDerivative = 2 * Mathf.PI * samplingPeriod;
            smoothingFactorOfTheDerivative = smoothingFactorOfTheDerivative / (smoothingFactorOfTheDerivative + 1);

            if (samplingPeriod == 0)
            {
                samplingPeriod = 0.001f;
            }

            Vector3 signalDerivative = (newHandPosition - prevFilteredValue) / samplingPeriod;

            if (isFirstApplicationOfTheFilter)
            {
                isFirstApplicationOfTheFilter = false;
                signalDerivative = Vector3.zero;
            }

            if (prevFilteredDerivative == Vector3.zero)
            {
                prevFilteredDerivative = signalDerivative;
                prevFilteredValue = newHandPosition;
            }

            var filteredDerivative = ExponentialSmoothing(smoothingFactorOfTheDerivative, signalDerivative, prevFilteredDerivative);
            prevFilteredDerivative = filteredDerivative;

            cutoffFrequency = minimumCutOffFrequency + beta * filteredDerivative.Abs();

            SetSmoothingFactor();

            var newFilteredValue = ExponentialSmoothing(smoothingFactor, newHandPosition, prevFilteredValue);
            latestFilteredValue = newFilteredValue;
            prevFilteredValue = latestFilteredValue;

            return newFilteredValue;
        }
    }
}
