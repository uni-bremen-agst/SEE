using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Pool;
using Unity.Cinemachine;

namespace SEE.Cinemachines.Dolly
{
    /// <summary>
    /// Class for a simple implementation of a Speed Controller, based on which section of the Spline the Object is.
    /// </summary>
    [Serializable]
    internal class SimpleSpeedController : SplineAutoDolly.ISplineAutoDolly
    {
        /// <summary>
        /// Structure for storing Sector-Data, specificly Start-Point on a Line [0,1), and its Speed on that sector.
        /// </summary>
        [Serializable]
        internal struct SplineSector
        {
            public float SectorStart;
            public float SectorSpeed;
        }

        bool SplineAutoDolly.ISplineAutoDolly.RequiresTrackingTarget => false;

        [Tooltip("List of Sections on a Spline, with its corresponding Speeds, inwhich that section needs to be paced with.")]
        private SplineSector[] SpeedList = {};

        /// <summary>
        /// Calculation Function to get new Spline Position.
        /// </summary>
        /// <param name="_sender">(Unused) Behaviour-Script, that triggered the function.</param>
        /// <param name="_target">(Unused) The Transform to apply the changes to.</param>
        /// <param name="_spline">(Unused) The Spline, where the Object should be moved on.</param>
        /// <param name="currentPosition">The Current Position on the <paramref name="_spline">.</param>
        /// <param name="_positionUnit">(Unused) Units used for the Splines.</param>
        /// <param name="deltaTime">Delta-Time between the current and last Frame.</param>
        /// <exception cref="IndexOutOfRangeException">Gets thrown, if the Speed-List has less than one Entries.</exception>
        /// <returns>Either the unmodified <paramref name="currentPosition">, if the Editor is in EditMode and the component is paused, or <paramref name="currentPosition"> + SectorSpeed, when in PlayMode.</returns>
        float SplineAutoDolly.ISplineAutoDolly.GetSplinePosition(MonoBehaviour _sender, Transform _target, SplineContainer _spline, float currentPosition, PathIndexUnit _positionUnit, float deltaTime)
        {
            // Dont Progress inside Editor; Credit https://gist.github.com/adammyhre/b81eb6e1d07ebe24a49844fbbddf368b
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

                if (tmpSector.SectorStart >= selectedSector.SectorStart)
                {
                    if (tmpSector.SectorStart <= currentPosition)
                    {
                        selectedSector = tmpSector;
                    }
                }
            }

            // Progress in Preview/Export

            return currentPosition + (selectedSector.SectorSpeed * deltaTime);
        }

        /// <summary>
        /// Reset Data that needs to be reset before Scene Start (Dynamic Data).
        /// </summary>
        void SplineAutoDolly.ISplineAutoDolly.Reset()
        {
            // Intentionally left blank
        }

        /// <summary>
        /// Validation Function, to make sure that all values are validly set.
        /// </summary>
        /// <exception cref="NullReferenceException">Thrown, if the Speed-List is not initialized.</exception>
        /// <exception cref="IndexOutOfRangeException">Thrown, if the Speed-List has less than one Entry.</exception>
        /// <exception cref="ArgumentException">Thrown, if the Speed is zero or the Sectors are out of range in an Entry.</exception>
        void SplineAutoDolly.ISplineAutoDolly.Validate()
        {
            // NullReference and Index Checks
            if (SpeedList == null)
            {
                // Debug.LogError("Spline Speed-List needs to be initialized");
                throw new NullReferenceException("Spline Speed-List needs to be initialized");
            }

            if (SpeedList != null && SpeedList.Length <= 0)
            {
                // Debug.LogError("Spline SpeedController needs at least one Entry in the Speed-List");
                throw new IndexOutOfRangeException("Spline SpeedController needs at least one Entry in the Speed-List");
            }

            for (int i = 0; i < SpeedList.Length; i++)
            {
                SplineSector currentEntry = SpeedList[i];
                if (currentEntry.SectorStart < 0)
                {
                    // Debug.LogError(String.Format("Sector can only start at '0'; At Entry {0}", i));
                    throw new ArgumentException(String.Format("Sector can only start at '0'; At Entry {0}", i), "SectorRange");
                }

                if (currentEntry.SectorStart >= 1)
                {
                    // Debug.LogError(String.Format("Sector can only start before '1'; At Entry {0}", i));
                    throw new ArgumentException(String.Format("Sector can only start before '1'; At Entry {0}", i), "SectorRange");
                }

                if (currentEntry.SectorSpeed <= 0)
                {
                    // Debug.LogErrorFormat(String.Format("Speed should not be zero; At Entry {0}", i));
                    throw new ArgumentException(String.Format("Speed should not be zero; At Entry {0}", i), "SectorSpeed");
                }
            }

            return;
        }
    }
}
