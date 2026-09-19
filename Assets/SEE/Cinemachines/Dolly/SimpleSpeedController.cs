using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Pool;
using Unity.Cinemachine;

namespace SEE.Cinemachines.Dolly
{
    /// <summary>
    /// Class for a simple implementation of a speed controller, based on which
    /// section of the spline the object is.
    /// </summary>
    [Serializable]
    internal class SimpleSpeedController : SplineAutoDolly.ISplineAutoDolly
    {
        /// <summary>
        /// Structure for storing sector data, specifically start point on a line [0,1), and its speed on that sector.
        /// </summary>
        [Serializable]
        internal struct SplineSector
        {
            public float SectorStart;
            public float SectorSpeed;
        }

        bool SplineAutoDolly.ISplineAutoDolly.RequiresTrackingTarget => false;

        /// <summary>
        /// List of sections on a spline, with its corresponding speeds, in which that
        /// section needs to be paced with.
        /// </summary>
        /// <remarks>Cannot be made readonly because it needs to be serialized by Unity.</remarks>
        [Tooltip("List of sections on a spline, with its corresponding speeds, in which that section needs to be paced with.")]
        public SplineSector[] SpeedList = {};

        /// <summary>
        /// Calculates the new spline position.
        /// </summary>
        /// <param name="sender">(Unused) Behaviour-Script, that triggered the function.</param>
        /// <param name="target">(Unused) The Transform to apply the changes to.</param>
        /// <param name="spline">(Unused) The spline where the object should be moved on.</param>
        /// <param name="currentPosition">The current position on the <paramref name="spline">.</param>
        /// <param name="positionUnit">(Unused) Units used for the splines.</param>
        /// <param name="deltaTime">Delta time between the current and last frame.</param>
        /// <exception cref="IndexOutOfRangeException">Gets thrown, if the speed list has less than one entry.</exception>
        /// <returns>Either the unmodified <paramref name="currentPosition">, if the Editor is in EditMode and the component is paused, or <paramref name="currentPosition"> + SectorSpeed, when in PlayMode.</returns>
        float SplineAutoDolly.ISplineAutoDolly.GetSplinePosition(MonoBehaviour sender, Transform target, SplineContainer spline, float currentPosition, PathIndexUnit positionUnit, float deltaTime)
        {
            // Don't Progress inside Editor; Credit https://gist.github.com/adammyhre/b81eb6e1d07ebe24a49844fbbddf368b
            if (deltaTime <= 0)
            {
                return currentPosition;
            }

            SplineSector selectedSector;

            if (SpeedList.Length > 0)
            {
                selectedSector = SpeedList[0];
            }
            else
            {
                throw new IndexOutOfRangeException("Speed-List must be longer than one (1) entry");
            }

            for (int i = 1; i < SpeedList.Length; i++)
            {
                SplineSector tmpSector = SpeedList[i];

                if (tmpSector.SectorStart >= selectedSector.SectorStart
                    && tmpSector.SectorStart <= currentPosition)
                {
                    selectedSector = tmpSector;
                }
            }

            // Progress in Preview/Export
            return currentPosition + (selectedSector.SectorSpeed * deltaTime);
        }

        /// <summary>
        /// Resets data that needs to be reset before scene start (dynamic data).
        /// </summary>
        void SplineAutoDolly.ISplineAutoDolly.Reset()
        {
            // Intentionally left blank
        }

        /// <summary>
        /// Validation function to make sure that all values are validly set.
        /// </summary>
        /// <exception cref="NullReferenceException">Thrown, if the speed list is not initialized.</exception>
        /// <exception cref="IndexOutOfRangeException">Thrown, if the speed list has less than one entry.</exception>
        /// <exception cref="ArgumentException">Thrown, if the speed is zero or the sectors are out of range in an entry.</exception>
        void SplineAutoDolly.ISplineAutoDolly.Validate()
        {
            // NullReference and index checks
            if (SpeedList == null)
            {
                throw new NullReferenceException("Spline speed list needs to be initialized.");
            }

            if (SpeedList != null && SpeedList.Length <= 0)
            {
                throw new IndexOutOfRangeException("Spline speed controller needs at least one entry in the speed list.");
            }

            for (int i = 0; i < SpeedList.Length; i++)
            {
                SplineSector currentEntry = SpeedList[i];
                if (currentEntry.SectorStart < 0)
                {
                    throw new ArgumentException(String.Format("Sector can only start at '0'; At Entry {0}", i), "SectorRange");
                }

                if (currentEntry.SectorStart >= 1)
                {
                    throw new ArgumentException(String.Format("Sector can only start before '1'; At Entry {0}", i), "SectorRange");
                }

                if (currentEntry.SectorSpeed <= 0)
                {
                    throw new ArgumentException(String.Format("Speed should not be zero; At Entry {0}", i), "SectorSpeed");
                }
            }
        }
    }
}
