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
    /// <remarks>This class it is selected by hand in the Inspector, from the
    /// "Automatic Dolly" dropdown on a CinemachineSplineDolly or CinemachineSplineCart.
    /// Cinemachine's SplineAutoDollyPropertyDrawer builds that dropdown by reflection
    /// over all non-abstract, non-obsolete ISplineAutoDolly implementations.
    /// It shows up as "Simple Speed Controller" next to Cinemachine's own "Fixed Speed"
    /// and "Nearest Point To Target". Picking it would then store it into the scene as
    /// a SerializeReference object.</remarks>
    [Serializable]
    internal class SimpleSpeedController : SplineAutoDolly.ISplineAutoDolly
    {
        /// <summary>
        /// Structure for storing sector data, specifically start point on a line [0,1), and its speed on that sector.
        /// </summary>
        [Serializable]
        internal struct SplineSector
        {
            /// <summary>
            /// The position on the spline at which this sector begins. Its
            /// <see cref="SectorSpeed"/> applies from here on until the next sector starts.
            /// </summary>
            /// <remarks>The value is compared against the current spline position and must,
            /// therefore, be given in the same unit, that is, in the
            /// <see cref="PathIndexUnit"/> configured on the dolly. Because the validation
            /// restricts the value to [0, 1), only <see cref="PathIndexUnit.Normalized"/> is
            /// actually supported, where 0 is the start and 1 the end of the spline.</remarks>
            public float SectorStart;

            /// <summary>
            /// The speed at which the object travels while it is within this sector,
            /// in spline position units per second.
            /// </summary>
            /// <remarks>Must be greater than zero, which the validation enforces. Because the
            /// speed is added to a normalized position (see <see cref="SectorStart"/>), a
            /// value of 1 traverses the whole spline in one second, independent of its length.</remarks>
            public float SectorSpeed;
        }

        bool SplineAutoDolly.ISplineAutoDolly.RequiresTrackingTarget => false;

        /// <summary>
        /// List of sections on a spline, with its corresponding speeds, in which that
        /// section needs to be paced with.
        /// </summary>
        /// <remarks>Cannot be made readonly because it needs to be serialized by Unity.
        /// The <see cref="SerializeField"/> attribute is required for the same reason: Unity
        /// serializes non-public fields only if they are marked with it, and a field that is
        /// not serialized would neither be shown in the Inspector nor retain its values.</remarks>
        [SerializeField]
        [Tooltip("List of sections on a spline, with its corresponding speeds, in which that section needs to be paced with.")]
        private SplineSector[] speedList = {};

        /// <summary>
        /// Whether the emptiness of <see cref="speedList"/> has been reported already.
        /// </summary>
        /// <remarks>Not serialized: this is about one run, not about the asset. It
        /// keeps the report to the one line rather than one per frame.</remarks>
        [NonSerialized]
        private bool emptyListReported;

        /// <summary>
        /// Compute the desired position on the spline as requested by
        /// <see cref="SplineAutoDolly.ISplineAutoDolly.GetSplinePosition"/>.
        /// </summary>
        /// <param name="sender">(Unused) Behaviour-Script, that triggered the function.</param>
        /// <param name="target">(Unused) The Transform to apply the changes to.</param>
        /// <param name="spline">(Unused) The spline where the object should be moved on.</param>
        /// <param name="currentPosition">The current position on the <paramref name="spline">.</param>
        /// <param name="positionUnit">(Unused) Units used for the splines.</param>
        /// <param name="deltaTime">Delta time between the current and last frame.</param>
        /// <exception cref="IndexOutOfRangeException">Gets thrown, if the speed list has less than one entry.</exception>
        /// <returns>Either the unmodified <paramref name="currentPosition">, if the Editor is in EditMode and the component is paused, or <paramref name="currentPosition"> + SectorSpeed, when in PlayMode.</returns>
        float SplineAutoDolly.ISplineAutoDolly.GetSplinePosition
            (MonoBehaviour sender,
            Transform target,
            SplineContainer spline,
            float currentPosition,
            PathIndexUnit positionUnit,
            float deltaTime)
        {
            // Don't Progress inside Editor; Credit https://gist.github.com/adammyhre/b81eb6e1d07ebe24a49844fbbddf368b
            if (deltaTime <= 0)
            {
                return currentPosition;
            }

            // This runs once a frame from Cinemachine, so an empty list is reported the
            // once and the dolly left where it is. Throwing here would raise the same
            // exception on every frame for as long as play mode lasted.
            if (speedList == null || speedList.Length == 0)
            {
                if (!emptyListReported)
                {
                    emptyListReported = true;
                    Debug.LogError("The speed list of the simple speed controller is empty, so "
                                   + "nothing moves. Give it at least one sector, the first of "
                                   + "them starting at 0.\n");
                }
                return currentPosition;
            }

            SplineSector selectedSector = speedList[0];

            for (int i = 1; i < speedList.Length; i++)
            {
                SplineSector tmpSector = speedList[i];

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
        /// <remarks>Implements <see cref="SplineAutoDolly.ISplineAutoDolly.Reset"/>.
        /// Does not do anything at the moment.</remarks>
        void SplineAutoDolly.ISplineAutoDolly.Reset()
        {
            // Intentionally left blank
        }

        /// <summary>
        /// Validation function to make sure that all values are validly set.
        /// Called from OnValidate() to validate the settings.
        /// </summary>
        /// <exception cref="NullReferenceException">Thrown, if the speed list is not initialized.</exception>
        /// <exception cref="IndexOutOfRangeException">Thrown, if the speed list has less than one entry.</exception>
        /// <exception cref="ArgumentException">Thrown, if the speed is zero or the sectors are out of range in an entry.</exception>
        /// <remarks>Implements <see cref="SplineAutoDolly.ISplineAutoDolly.Validate"/>.</remarks>
        void SplineAutoDolly.ISplineAutoDolly.Validate()
        {
            // NullReference and index checks
            if (speedList == null)
            {
                throw new NullReferenceException("Spline speed list needs to be initialized.");
            }

            if (speedList.Length == 0)
            {
                throw new IndexOutOfRangeException("Spline speed controller needs at least one entry in the speed list.");
            }

            for (int i = 0; i < speedList.Length; i++)
            {
                SplineSector currentEntry = speedList[i];
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
