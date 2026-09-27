using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.TestTools;

namespace SEE.Cinemachines.Dolly
{
    /// <summary>
    /// Tests for <see cref="SpeedProfile"/>.
    /// </summary>
    /// <remarks>The speed at a position is what these tests are really about, but the
    /// controller reports a position rather than a speed. They are one and the same here:
    /// the controller advances the position by the speed times the elapsed time, so asking
    /// it for a step of one second and subtracting where it started gives the speed in
    /// force. That is what <see cref="SpeedAt"/> does.</remarks>
    internal class TestSpeedProfile
    {
        /// <summary>
        /// The distance from the first knot of the test spline to its last, in world units.
        /// </summary>
        private const float splineExtent = 40;

        /// <summary>
        /// Tolerance for comparing speeds, which are arrived at by interpolation and by
        /// subtracting two positions.
        /// </summary>
        private const float tolerance = 1e-4f;

        /// <summary>
        /// The game object carrying <see cref="container"/>.
        /// </summary>
        private GameObject gameObject;

        /// <summary>
        /// A container holding a single straight spline of length <see cref="splineExtent"/>
        /// along the X axis. Straight, so that a fraction of the spline is the same fraction
        /// of its length and the expectations of the tests in <see cref="PathIndexUnit.Distance"/>
        /// can be stated in world units.
        /// </summary>
        private SplineContainer container;

        /// <summary>
        /// Creates <see cref="container"/> with the spline described there.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("Spline");
            container = gameObject.AddComponent<SplineContainer>();
            container.Spline.Add(new float3(0, 0, 0));
            container.Spline.Add(new float3(splineExtent, 0, 0));
        }

        /// <summary>
        /// Destroys the game object created in <see cref="SetUp"/>.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
            gameObject = null;
            container = null;
        }

        #region Sectors

        /// <summary>
        /// A position takes the speed of the sector it falls in.
        /// </summary>
        [Test]
        public void TestSectorInForceGivesTheSpeed()
        {
            SpeedProfile controller = Controller(0, (0, 1), (0.5f, 3));

            Assert.That(SpeedAt(controller, 0.25f), Is.EqualTo(1).Within(tolerance));
            Assert.That(SpeedAt(controller, 0.75f), Is.EqualTo(3).Within(tolerance));
        }

        /// <summary>
        /// A sector takes effect from its own start, so the speed changes there and not
        /// before.
        /// </summary>
        [Test]
        public void TestSpeedChangesAtTheBoundary()
        {
            SpeedProfile controller = Controller(0, (0, 1), (0.5f, 3));

            Assert.That(SpeedAt(controller, 0.4999f), Is.EqualTo(1).Within(tolerance));
            Assert.That(SpeedAt(controller, 0.5f), Is.EqualTo(3).Within(tolerance));
        }

        /// <summary>
        /// The sector in force is the last one to have begun, whatever order the sectors
        /// are given in.
        /// </summary>
        [Test]
        public void TestSectorsNeedNotBeOrdered()
        {
            SpeedProfile controller = Controller(0, (0.5f, 3), (0.8f, 5), (0, 1));

            Assert.That(SpeedAt(controller, 0.1f), Is.EqualTo(1).Within(tolerance));
            Assert.That(SpeedAt(controller, 0.6f), Is.EqualTo(3).Within(tolerance));
            Assert.That(SpeedAt(controller, 0.9f), Is.EqualTo(5).Within(tolerance));
        }

        #endregion Sectors

        #region Position units

        /// <summary>
        /// Sector boundaries are fractions of the spline and are converted into the unit
        /// the dolly works in, so the same configuration describes the same stretches of
        /// spline whichever unit that is.
        /// </summary>
        /// <remarks>This is the case the controller used to get wrong. Comparing the
        /// boundary 0.5 against a position measured in world units put every boundary
        /// within the first unit of the spline, so the last sector was in force almost
        /// everywhere.</remarks>
        [Test]
        public void TestBoundariesFollowTheDistanceUnit()
        {
            SpeedProfile controller = Controller(0, (0, 1), (0.5f, 3));
            float length = container.Spline.GetLength();

            Assert.That(SpeedAt(controller, 0.25f * length, PathIndexUnit.Distance),
                        Is.EqualTo(1).Within(tolerance), "A quarter along lies in the first sector.");
            Assert.That(SpeedAt(controller, 0.75f * length, PathIndexUnit.Distance),
                        Is.EqualTo(3).Within(tolerance), "Three quarters along lies in the second.");
        }

        /// <summary>
        /// The same, in <see cref="PathIndexUnit.Knot"/>, where a position is a knot index.
        /// </summary>
        [Test]
        public void TestBoundariesFollowTheKnotUnit()
        {
            SpeedProfile controller = Controller(0, (0, 1), (0.5f, 3));

            // The spline has two knots, so its one curve runs from index 0 to index 1 and
            // the boundary at half the spline falls at index 0.5.
            Assert.That(SpeedAt(controller, 0.25f, PathIndexUnit.Knot), Is.EqualTo(1).Within(tolerance));
            Assert.That(SpeedAt(controller, 0.75f, PathIndexUnit.Knot), Is.EqualTo(3).Within(tolerance));
        }

        #endregion Position units

        #region Easing

        /// <summary>
        /// Without easing the speed changes from one value to the other at the boundary,
        /// with nothing in between.
        /// </summary>
        [Test]
        public void TestWithoutEasingTheSpeedJumps()
        {
            SpeedProfile controller = Controller(0, (0, 1), (0.5f, 3));

            Assert.That(SpeedAt(controller, 0.5f), Is.EqualTo(3).Within(tolerance));
            Assert.That(SpeedAt(controller, 0.6f), Is.EqualTo(3).Within(tolerance));
        }

        /// <summary>
        /// With easing the sector works up to its speed over the run-up given, starting
        /// from the speed of the sector before it.
        /// </summary>
        [Test]
        public void TestEasingGivesTheSectorARunUp()
        {
            SpeedProfile controller = Controller(0.2f, (0, 1), (0.5f, 3));

            Assert.That(SpeedAt(controller, 0.4999f), Is.EqualTo(1).Within(tolerance),
                        "Before the boundary nothing has changed.");
            Assert.That(SpeedAt(controller, 0.5f), Is.EqualTo(1).Within(tolerance),
                        "At the boundary the run-up begins at the old speed.");
            Assert.That(SpeedAt(controller, 0.6f), Is.EqualTo(2).Within(tolerance),
                        "Halfway up, the speed is halfway between the two.");
            Assert.That(SpeedAt(controller, 0.7f), Is.EqualTo(3).Within(tolerance),
                        "At the end of the run-up the sector's own speed is reached.");
            Assert.That(SpeedAt(controller, 0.8f), Is.EqualTo(3).Within(tolerance),
                        "Beyond the run-up it stays there.");
        }

        /// <summary>
        /// The speed rises without turning back on itself anywhere along the run-up.
        /// </summary>
        [Test]
        public void TestTheRunUpIsMonotone()
        {
            SpeedProfile controller = Controller(0.2f, (0, 1), (0.5f, 3));
            float previous = SpeedAt(controller, 0.5f);

            for (int step = 1; step <= 40; step++)
            {
                float speed = SpeedAt(controller, 0.5f + (0.2f * step / 40));
                Assert.That(speed, Is.GreaterThanOrEqualTo(previous - tolerance),
                            $"The speed fell back between the steps ending at {step}.");
                previous = speed;
            }
        }

        /// <summary>
        /// The first sector has no sector before it to ease away from, so it applies from
        /// the start of the spline at its own speed.
        /// </summary>
        [Test]
        public void TestTheFirstSectorHasNoRunUp()
        {
            SpeedProfile controller = Controller(0.2f, (0, 1), (0.5f, 3));

            Assert.That(SpeedAt(controller, 0), Is.EqualTo(1).Within(tolerance));
            Assert.That(SpeedAt(controller, 0.1f), Is.EqualTo(1).Within(tolerance));
        }

        /// <summary>
        /// A run-up reaching past the next sector is cut short at it, so that a sector
        /// shorter than the easing still arrives at its speed by its own end and the next
        /// one carries on from there.
        /// </summary>
        [Test]
        public void TestTheRunUpStopsAtTheNextSector()
        {
            SpeedProfile controller = Controller(0.2f, (0, 1), (0.5f, 3), (0.55f, 9));

            Assert.That(SpeedAt(controller, 0.525f), Is.EqualTo(2).Within(tolerance),
                        "The run-up is cut to [0.5, 0.55], so its midpoint is at 0.525.");
            Assert.That(SpeedAt(controller, 0.55f - 1e-5f), Is.EqualTo(3).Within(1e-2f),
                        "The sector's own speed is reached by the time the next one begins.");
        }

        /// <summary>
        /// The speed does not jump where one run-up ends and the next begins.
        /// </summary>
        [Test]
        public void TestTheChainOfRunUpsIsContinuous()
        {
            SpeedProfile controller = Controller(0.2f, (0, 1), (0.5f, 3), (0.55f, 9));
            float before = SpeedAt(controller, 0.55f - 1e-4f);
            float after = SpeedAt(controller, 0.55f + 1e-4f);

            Assert.That(after - before, Is.LessThan(0.1f),
                        $"The speed jumped from {before} to {after} at the boundary.");
            Assert.That(SpeedAt(controller, 0.75f), Is.EqualTo(9).Within(tolerance),
                        "The third sector reaches its own speed at the end of its run-up.");
        }

        #endregion Easing

        #region Faults

        /// <summary>
        /// Time that is not advancing moves nothing, and says nothing about it either.
        /// </summary>
        [Test]
        public void TestTimeNotAdvancingMovesNothing()
        {
            SpeedProfile controller = Controller(0, (0, 1));

            Assert.That(Position(controller, 0.25f, PathIndexUnit.Normalized, 0),
                        Is.EqualTo(0.25f).Within(tolerance));
            Assert.That(Position(controller, 0.25f, PathIndexUnit.Normalized, -1),
                        Is.EqualTo(0.25f).Within(tolerance));
        }

        /// <summary>
        /// An empty speed list moves nothing and is reported, once however often the
        /// controller is asked.
        /// </summary>
        [Test]
        public void TestAnEmptyListIsReportedOnce()
        {
            SpeedProfile controller = Controller(0);
            LogAssert.Expect(LogType.Error, new Regex("The speed list of the speed profile is empty"));

            Assert.That(Position(controller, 0.25f), Is.EqualTo(0.25f).Within(tolerance));
            Assert.That(Position(controller, 0.25f), Is.EqualTo(0.25f).Within(tolerance));
        }

        /// <summary>
        /// A dolly with no spline to travel moves nothing and is reported once.
        /// </summary>
        [Test]
        public void TestAMissingSplineIsReportedOnce()
        {
            SpeedProfile controller = Controller(0, (0, 1));
            LogAssert.Expect(LogType.Error, new Regex("has no spline to work on"));
            SplineAutoDolly.ISplineAutoDolly dolly = controller;

            Assert.That(dolly.GetSplinePosition(null, null, null, 0.25f, PathIndexUnit.Normalized, 1),
                        Is.EqualTo(0.25f).Within(tolerance));
            Assert.That(dolly.GetSplinePosition(null, null, null, 0.25f, PathIndexUnit.Normalized, 1),
                        Is.EqualTo(0.25f).Within(tolerance));
        }

        /// <summary>
        /// A position before the first sector belongs to no sector, so there is no speed
        /// to travel at. It moves nothing and is reported once.
        /// </summary>
        [Test]
        public void TestAPositionBeforeEverySectorIsReportedOnce()
        {
            SpeedProfile controller = Controller(0, (0.3f, 2));
            LogAssert.Expect(LogType.Error,
                             new Regex("No sector of the speed profile starts at or before"));

            Assert.That(Position(controller, 0.1f), Is.EqualTo(0.1f).Within(tolerance));
            Assert.That(Position(controller, 0.1f), Is.EqualTo(0.1f).Within(tolerance));
        }

        /// <summary>
        /// A sector that travels at no speed moves nothing and is reported once.
        /// </summary>
        [Test]
        public void TestASectorWithoutSpeedIsReportedOnce()
        {
            SpeedProfile controller = Controller(0, (0, 0));
            LogAssert.Expect(LogType.Error, new Regex("travels at a speed of"));

            Assert.That(Position(controller, 0.25f), Is.EqualTo(0.25f).Within(tolerance));
            Assert.That(Position(controller, 0.25f), Is.EqualTo(0.25f).Within(tolerance));
        }

        #endregion Faults

        #region Validation

        /// <summary>
        /// Validation does not throw on settings that are not usable yet, which is the
        /// state every list passes through while it is being filled in. Cinemachine calls
        /// it from OnValidate without guarding, so a throw would surface as an editor error
        /// on every keystroke.
        /// </summary>
        [Test]
        public void TestValidationDoesNotThrow()
        {
            SplineAutoDolly.ISplineAutoDolly empty = Controller(0);
            Assert.That(() => empty.Validate(), Throws.Nothing, "An empty list must be tolerated.");

            SpeedProfile uninitialized = new();
            SetField(uninitialized, "speedList", null);
            Assert.That(() => ((SplineAutoDolly.ISplineAutoDolly)uninitialized).Validate(), Throws.Nothing,
                        "A list that was never given an array must be tolerated.");

            SplineAutoDolly.ISplineAutoDolly speedless = Controller(0, (0, 0));
            Assert.That(() => speedless.Validate(), Throws.Nothing, "A sector without a speed must be tolerated.");

            SplineAutoDolly.ISplineAutoDolly gap = Controller(0, (0.3f, 2));
            Assert.That(() => gap.Validate(), Throws.Nothing, "A list starting past the spline must be tolerated.");
        }

        /// <summary>
        /// Validation brings a sector that begins outside the spline back within it, and
        /// gives a run-up of negative length a length of zero.
        /// </summary>
        [Test]
        public void TestValidationClampsWhatItCan()
        {
            SpeedProfile controller = Controller(-3, (-1, 2), (5, 4));

            ((SplineAutoDolly.ISplineAutoDolly)controller).Validate();

            Assert.That(GetField<float>(controller, "easing"), Is.EqualTo(0).Within(tolerance),
                        "A run-up cannot be shorter than nothing.");
            SpeedProfile.SplineSector[] sectors =
                GetField<SpeedProfile.SplineSector[]>(controller, "speedList");
            Assert.That(sectors[0].SectorStart, Is.EqualTo(0).Within(tolerance));
            Assert.That(sectors[1].SectorStart, Is.EqualTo(1).Within(tolerance));
        }

        /// <summary>
        /// Validation leaves settings that are already usable exactly as they are.
        /// </summary>
        [Test]
        public void TestValidationLeavesGoodSettingsAlone()
        {
            SpeedProfile controller = Controller(0.25f, (0, 1), (0.5f, 3));

            ((SplineAutoDolly.ISplineAutoDolly)controller).Validate();

            Assert.That(GetField<float>(controller, "easing"), Is.EqualTo(0.25f).Within(tolerance));
            Assert.That(SpeedAt(controller, 0.25f), Is.EqualTo(1).Within(tolerance));
        }

        #endregion Validation

        #region Helpers

        /// <summary>
        /// A controller whose speed list holds the given sectors and whose run-up is
        /// <paramref name="easing"/>.
        /// </summary>
        /// <param name="easing">The value for the controller's easing.</param>
        /// <param name="sectors">The sectors of the speed list, as start and speed.</param>
        /// <returns>The controller.</returns>
        private static SpeedProfile Controller(float easing, params (float Start, float Speed)[] sectors)
        {
            SpeedProfile.SplineSector[] list = new SpeedProfile.SplineSector[sectors.Length];
            for (int i = 0; i < sectors.Length; i++)
            {
                list[i].SectorStart = sectors[i].Start;
                list[i].SectorSpeed = sectors[i].Speed;
            }

            SpeedProfile controller = new();
            SetField(controller, "speedList", list);
            SetField(controller, "easing", easing);
            return controller;
        }

        /// <summary>
        /// The speed <paramref name="controller"/> travels at <paramref name="position"/>,
        /// obtained as the ground covered in one second.
        /// </summary>
        /// <param name="controller">The controller to ask.</param>
        /// <param name="position">The position on the spline, in <paramref name="unit"/>.</param>
        /// <param name="unit">The unit positions are expressed in.</param>
        /// <returns>The speed in force at <paramref name="position"/>.</returns>
        private float SpeedAt(SpeedProfile controller, float position,
                              PathIndexUnit unit = PathIndexUnit.Normalized)
        {
            return Position(controller, position, unit) - position;
        }

        /// <summary>
        /// The position <paramref name="controller"/> moves to from <paramref name="position"/>.
        /// </summary>
        /// <param name="controller">The controller to ask.</param>
        /// <param name="position">The position on the spline, in <paramref name="unit"/>.</param>
        /// <param name="unit">The unit positions are expressed in.</param>
        /// <param name="deltaTime">The time said to have passed.</param>
        /// <returns>The position the controller asks for.</returns>
        private float Position(SpeedProfile controller, float position,
                               PathIndexUnit unit = PathIndexUnit.Normalized, float deltaTime = 1)
        {
            SplineAutoDolly.ISplineAutoDolly dolly = controller;
            return dolly.GetSplinePosition(null, null, container, position, unit, deltaTime);
        }

        /// <summary>
        /// Assigns <paramref name="value"/> to the private field <paramref name="name"/> of
        /// <paramref name="controller"/>.
        /// </summary>
        /// <remarks>The fields are serialized by Unity and set in the inspector, so they
        /// are private and have no setters. Reflection stands in for the inspector here.
        /// A field that cannot be found fails the test rather than passing silently, so
        /// that renaming one is noticed.</remarks>
        /// <param name="controller">The controller whose field is to be set.</param>
        /// <param name="name">The name of the field.</param>
        /// <param name="value">The value to assign.</param>
        private static void SetField(SpeedProfile controller, string name, object value)
        {
            Field(name).SetValue(controller, value);
        }

        /// <summary>
        /// The value of the private field <paramref name="name"/> of <paramref name="controller"/>.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="controller">The controller whose field is to be read.</param>
        /// <param name="name">The name of the field.</param>
        /// <returns>The value of the field.</returns>
        private static T GetField<T>(SpeedProfile controller, string name)
        {
            return (T)Field(name).GetValue(controller);
        }

        /// <summary>
        /// The private instance field <paramref name="name"/> of <see cref="SpeedProfile"/>.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <returns>The field.</returns>
        private static FieldInfo Field(string name)
        {
            FieldInfo field = typeof(SpeedProfile).GetField(name,
                                                                    BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null,
                        $"{nameof(SpeedProfile)} has no field {name}; has it been renamed?");
            return field;
        }

        #endregion Helpers
    }
}
