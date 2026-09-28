using NUnit.Framework;
using UnityEngine;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// Tests the cloning semantics of <see cref="LineConf"/>.
    /// </summary>
    [TestFixture]
    public class TestLineConfClone
    {
        /// <summary>
        /// Verifies that renderer positions are copied into an independent array.
        /// </summary>
        [Test]
        public void CloneCopiesRendererPositions()
        {
            LineConf original = new()
            {
                RendererPositions = new[]
                {
                    new Vector3(1.0f, 2.0f, 3.0f),
                    new Vector3(4.0f, 5.0f, 6.0f)
                }
            };

            LineConf clone = original.Clone();

            Assert.That(clone.RendererPositions, Is.Not.SameAs(original.RendererPositions));
            Assert.That(clone.RendererPositions, Is.EqualTo(original.RendererPositions));

            clone.RendererPositions[0] = Vector3.zero;

            Assert.That(original.RendererPositions[0], Is.EqualTo(new Vector3(1.0f, 2.0f, 3.0f)));
        }

        /// <summary>
        /// Verifies that start and end line cap configurations are cloned independently.
        /// </summary>
        [Test]
        public void CloneCopiesLineCapConfigurations()
        {
            LineConf original = new()
            {
                LineCapStart = new LineCapConf
                {
                    Thickness = 1.0f
                },
                LineCapEnd = new LineCapConf
                {
                    Thickness = 2.0f
                }
            };

            LineConf clone = original.Clone();

            Assert.That(clone.LineCapStart, Is.Not.SameAs(original.LineCapStart));
            Assert.That(clone.LineCapEnd, Is.Not.SameAs(original.LineCapEnd));

            clone.LineCapStart.Thickness = 3.0f;
            clone.LineCapEnd.Thickness = 4.0f;

            Assert.That(original.LineCapStart.Thickness, Is.EqualTo(1.0f));
            Assert.That(original.LineCapEnd.Thickness, Is.EqualTo(2.0f));
        }

        /// <summary>
        /// Verifies that null mutable members remain null in the cloned configuration.
        /// </summary>
        [Test]
        public void ClonePreservesNullMutableMembers()
        {
            LineConf original = new()
            {
                RendererPositions = null,
                LineCapStart = null,
                LineCapEnd = null
            };

            LineConf clone = original.Clone();

            Assert.That(clone.RendererPositions, Is.Null);
            Assert.That(clone.LineCapStart, Is.Null);
            Assert.That(clone.LineCapEnd, Is.Null);
        }
    }
}
