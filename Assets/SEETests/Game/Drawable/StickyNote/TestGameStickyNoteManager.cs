using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace SEE.Game.Drawable.StickyNote
{
    /// <summary>
    /// Tests the orientation behavior of <see cref="GameStickyNoteManager"/>.
    /// </summary>
    [TestFixture]
    public class TestGameStickyNoteManager
    {
        /// <summary>
        /// The maximum allowed angular difference in degrees when comparing directions.
        /// </summary>
        private const float angleTolerance = 0.01f;

        /// <summary>
        /// The sticky note used by the current test.
        /// </summary>
        private GameObject stickyNote;

        /// <summary>
        /// Creates the sticky note required by each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            stickyNote = new GameObject("StickyNote");
        }

        /// <summary>
        /// Destroys the sticky note created for the current test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (stickyNote != null)
            {
                Object.DestroyImmediate(stickyNote);
            }
        }

        /// <summary>
        /// Verifies that the front of a sticky note is aligned opposite to the
        /// normal of the surface on which it is placed.
        /// </summary>
        /// <param name="normalX">The x component of the surface normal.</param>
        /// <param name="normalY">The y component of the surface normal.</param>
        /// <param name="normalZ">The z component of the surface normal.</param>
        /// <param name="forwardX">The expected x component of the sticky note forward direction.</param>
        /// <param name="forwardY">The expected y component of the sticky note forward direction.</param>
        /// <param name="forwardZ">The expected z component of the sticky note forward direction.</param>
        [TestCase(0.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f)]
        [TestCase(0.0f, 0.0f, 1.0f, 0.0f, 0.0f, -1.0f)]
        [TestCase(1.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f)]
        [TestCase(-1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f)]
        public void TestFrontFacesAwayFromSurface(float normalX, float normalY, float normalZ,
            float forwardX, float forwardY, float forwardZ)
        {
            Vector3 surfaceNormal = new Vector3(normalX, normalY, normalZ);
            Vector3 expectedForward = new Vector3(forwardX, forwardY, forwardZ);

            EnsureFrontFacesAwayFromSurface(stickyNote, surfaceNormal);

            Assert.That(Vector3.Angle(stickyNote.transform.forward, expectedForward),
                Is.LessThan(angleTolerance));
        }

        /// <summary>
        /// Verifies that an already correctly aligned sticky note retains its
        /// existing rotation around the surface normal.
        /// </summary>
        [Test]
        public void TestAlreadyAlignedRotationIsPreserved()
        {
            stickyNote.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 37.0f);
            Quaternion originalRotation = stickyNote.transform.rotation;

            EnsureFrontFacesAwayFromSurface(stickyNote, Vector3.back);

            Assert.That(Quaternion.Angle(stickyNote.transform.rotation, originalRotation),
                Is.LessThan(angleTolerance));
        }

        /// <summary>
        /// Verifies that alignment succeeds when the current up direction is
        /// parallel to the required forward direction.
        /// </summary>
        [Test]
        public void TestAlignmentHandlesParallelUpDirection()
        {
            stickyNote.transform.rotation = Quaternion.identity;
            Vector3 originalRight = stickyNote.transform.right;

            EnsureFrontFacesAwayFromSurface(stickyNote, Vector3.down);

            Assert.That(Vector3.Angle(stickyNote.transform.forward, Vector3.up),
                Is.LessThan(angleTolerance));
            Assert.That(Vector3.Angle(stickyNote.transform.right, originalRight),
                Is.LessThan(angleTolerance));
            Assert.That(Mathf.Abs(Vector3.Dot(stickyNote.transform.forward, stickyNote.transform.up)),
                Is.LessThan(0.0001f));
        }

        /// <summary>
        /// Invokes the private orientation helper of <see cref="GameStickyNoteManager"/>.
        /// </summary>
        /// <param name="target">The sticky note whose orientation should be adjusted.</param>
        /// <param name="surfaceNormal">The normal of the surface on which the sticky note is placed.</param>
        private static void EnsureFrontFacesAwayFromSurface(GameObject target, Vector3 surfaceNormal)
        {
            MethodInfo method = typeof(GameStickyNoteManager).GetMethod(
                "EnsureFrontFacesAwayFromSurface", BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null,
                "Could not find GameStickyNoteManager.EnsureFrontFacesAwayFromSurface().");

            method.Invoke(null, new object[] { target, surfaceNormal });
        }
    }
}
