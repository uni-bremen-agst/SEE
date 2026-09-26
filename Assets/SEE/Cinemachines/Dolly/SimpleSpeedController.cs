using System;
using UnityEngine;
using UnityEngine.Splines;
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
            /// The point of the spline at which this sector begins, as a fraction of the
            /// whole: 0 is the start of the spline and 1 its end. Its
            /// <see cref="SectorSpeed"/> applies from here on until the next sector starts.
            /// </summary>
            /// <remarks>A fraction rather than a position in the
            /// <see cref="PathIndexUnit"/> of the dolly, so that the sectors keep their
            /// meaning when the units are changed or the spline is redrawn: "the first
            /// third of the route" stays the first third. It is converted into the unit
            /// in force before being compared against the current position. The
            /// validation clamps it to [0, 1].</remarks>
            public float SectorStart;

            /// <summary>
            /// The speed at which the object travels while it is within this sector, in
            /// the <see cref="PathIndexUnit"/> of the dolly per second.
            /// </summary>
            /// <remarks>The same unit as Cinemachine's own <c>Fixed Speed</c>, so the
            /// number means there what it means here: world units per second under
            /// <see cref="PathIndexUnit.Distance"/>, whole splines per second under
            /// <see cref="PathIndexUnit.Normalized"/>, knots per second under
            /// <see cref="PathIndexUnit.Knot"/>. A value of zero or less leaves the
            /// object where it is, which is reported once.</remarks>
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
        /// keeps the report to one line rather than one per frame.</remarks>
        [NonSerialized]
        private bool emptyListReported;

        /// <summary>
        /// Whether it has been reported already that no sector of
        /// <see cref="speedList"/> had begun.
        /// </summary>
        /// <remarks>Not serialized, for the reason given at
        /// <see cref="emptyListReported"/>.</remarks>
        [NonSerialized]
        private bool noSectorReported;

        /// <summary>
        /// Whether it has been reported already that the spline cannot be used.
        /// </summary>
        /// <remarks>Not serialized, for the reason given at
        /// <see cref="emptyListReported"/>.</remarks>
        [NonSerialized]
        private bool noSplineReported;

        /// <summary>
        /// Whether it has been reported already that the sector in force travels at a
        /// speed of zero or less.
        /// </summary>
        /// <remarks>Not serialized, for the reason given at
        /// <see cref="emptyListReported"/>.</remarks>
        [NonSerialized]
        private bool noSpeedReported;

        /// <summary>
        /// Compute the desired position on the spline as requested by
        /// <see cref="SplineAutoDolly.ISplineAutoDolly.GetSplinePosition"/>.
        /// </summary>
        /// <param name="sender">(Unused) Behaviour-Script, that triggered the function.</param>
        /// <param name="target">(Unused) The Transform to apply the changes to.</param>
        /// <param name="spline">The spline the object is moving on, needed to express the
        /// sector boundaries in <paramref name="positionUnit"/>.</param>
        /// <param name="currentPosition">The current position on the <paramref name="spline"/>,
        /// in <paramref name="positionUnit"/>.</param>
        /// <param name="positionUnit">The unit in which positions on the
        /// <paramref name="spline"/> are expressed.</param>
        /// <param name="deltaTime">Delta time between the current and last frame.</param>
        /// <returns><paramref name="currentPosition"/> advanced by the speed of the sector
        /// in force, or <paramref name="currentPosition"/> unchanged where nothing can be
        /// advanced: when <paramref name="deltaTime"/> is not positive, when the speed list
        /// is empty, when the <paramref name="spline"/> holds no spline, when no sector
        /// begins at or before the current position, or when the speed of the sector in
        /// force is not positive. Each of the last four is reported once.</returns>
        /// <remarks>Movement is not confined to play mode, so that a shot can be watched by
        /// dragging the playhead of a timeline and comes out of the recorder as it looks
        /// there.</remarks>
        float SplineAutoDolly.ISplineAutoDolly.GetSplinePosition
            (MonoBehaviour sender,
            Transform target,
            SplineContainer spline,
            float currentPosition,
            PathIndexUnit positionUnit,
            float deltaTime)
        {
            // Time is not advancing — the game is paused, or this is the first frame of a
            // preview — so neither is the dolly.
            // Credit https://gist.github.com/adammyhre/b81eb6e1d07ebe24a49844fbbddf368b
            if (deltaTime <= 0)
            {
                return currentPosition;
            }

            // This runs once per frame from Cinemachine, so an empty list is reported
            // once and the dolly left where it is. Throwing here would raise the same
            // exception on every frame for as long as play mode lasts.
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

            // The boundaries are fractions of the spline and the position is in the unit
            // of the dolly, so the spline is needed to bring the two together.
            if (spline == null || spline.Splines == null || spline.Splines.Count == 0)
            {
                if (!noSplineReported)
                {
                    noSplineReported = true;
                    Debug.LogError("The simple speed controller has no spline to work on, so "
                                   + "nothing moves. Name one in the Spline property of the "
                                   + "dolly.\n");
                }
                return currentPosition;
            }

            // The sector in force is the last one to have begun, that is, the one whose
            // start is the greatest of those not past the current position. Seeking it
            // out this way asks nothing of the order the sectors are given in.
            bool found = false;
            float selectedStart = 0;
            SplineSector selectedSector = default;

            foreach (SplineSector sector in speedList)
            {
                float sectorStart = InPositionUnit(spline, sector.SectorStart, positionUnit);
                if (sectorStart <= currentPosition
                    && (!found || sectorStart > selectedStart))
                {
                    selectedSector = sector;
                    selectedStart = sectorStart;
                    found = true;
                }
            }

            // No sector has begun, so there is no speed to travel at. This is what a list
            // whose first sector starts past the beginning of the spline comes to.
            if (!found)
            {
                if (!noSectorReported)
                {
                    noSectorReported = true;
                    Debug.LogError($"No sector of the simple speed controller starts at or before "
                                   + $"{currentPosition}, so nothing moves. Give it a sector "
                                   + "starting at 0.\n");
                }
                return currentPosition;
            }

            // A sector that does not move is almost always a row left half filled in,
            // the speed of a new one being zero until it is given one.
            if (selectedSector.SectorSpeed <= 0)
            {
                if (!noSpeedReported)
                {
                    noSpeedReported = true;
                    Debug.LogError($"The sector of the simple speed controller beginning at "
                                   + $"{selectedSector.SectorStart} travels at a speed of "
                                   + $"{selectedSector.SectorSpeed}, so nothing moves. Give it "
                                   + "a speed greater than zero.\n");
                }
                return currentPosition;
            }

            // Progress in Preview/Export
            return currentPosition + (selectedSector.SectorSpeed * deltaTime);
        }

        /// <summary>
        /// Returns the point <paramref name="fraction"/> of the way along
        /// <paramref name="spline"/>, expressed in <paramref name="unit"/>.
        /// </summary>
        /// <param name="spline">The spline the fraction refers to.</param>
        /// <param name="fraction">A fraction of the spline, 0 being its start and 1 its end.</param>
        /// <param name="unit">The unit the result is to be given in.</param>
        /// <returns>The corresponding position in <paramref name="unit"/>.</returns>
        private static float InPositionUnit(SplineContainer spline, float fraction, PathIndexUnit unit)
        {
            return unit == PathIndexUnit.Normalized
                ? fraction
                : spline.Spline.ConvertIndexUnit(fraction, PathIndexUnit.Normalized, unit);
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
        /// Brings the settings into a state that can be worked with: the speed list is
        /// given an array if it has none, and every sector is brought within the spline.
        /// </summary>
        /// <remarks>Implements <see cref="SplineAutoDolly.ISplineAutoDolly.Validate"/>,
        /// which Cinemachine calls unguarded from the <c>OnValidate</c> of the dolly. It
        /// must therefore not throw: filling a list in means passing through states that
        /// are not yet valid — a row with no speed, a boundary being typed — and an
        /// exception on each of them would fill the console with editor errors while the
        /// user is halfway through a keystroke. What can be repaired is repaired here in
        /// silence; what cannot is reported once by
        /// <see cref="SplineAutoDolly.ISplineAutoDolly.GetSplinePosition"/>, when it comes
        /// to actually preventing movement.</remarks>
        void SplineAutoDolly.ISplineAutoDolly.Validate()
        {
            speedList ??= Array.Empty<SplineSector>();

            for (int i = 0; i < speedList.Length; i++)
            {
                // A boundary outside the spline describes no part of it.
                speedList[i].SectorStart = Mathf.Clamp01(speedList[i].SectorStart);
            }
        }
    }
}
