using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.EmptySpace
{
    /// <summary>
    /// Tests for <see cref="EmptySpaceFinder"/>.
    /// </summary>
    public class TestEmptySpaceFinder
    {
        /// <summary>
        /// outerRectangle is null. Expected to throw ArgumentNullException.
        /// </summary>
        [Test]
        public void Find_NullOuterRectangle_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => EmptySpaceFinder.Find(null!, new List<Rectangle>()));
        }

        /// <summary>
        /// outerRectangle is null. Expected to throw ArgumentNullException.
        /// </summary>
        [Test]
        public void Find_NullInnerRectangles_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => EmptySpaceFinder.Find(new Rectangle(0, 0, 10, 10), null!));
        }

        /// <summary>
        /// The inner rectangles contain one that is outside the outer rectangle. Expected to throw ArgumentException.
        /// </summary>
        [Test]
        public void Find_InnerRectangleOutsideOuter_ThrowsArgumentException()
        {
            Rectangle outer = new Rectangle(0, 0, 10, 10);
            List<Rectangle> inner = new List<Rectangle> {
                  new Rectangle(1, 1, 2, 2),
                  new Rectangle(20, 20, 2, 2),
                  new Rectangle(3, 3, 1, 1)};
            Assert.Throws<ArgumentException>(() => EmptySpaceFinder.Find(outer, inner));
        }

        /// <summary>
        /// Inner rectangles is empty. The outer rectangle should be returned as the only maximal empty rectangle.
        /// </summary>
        [Test]
        public void Find_NoInnerRectangles_ReturnsSingleRectEqualOuter()
        {
            Rectangle outer = new Rectangle(0, 0, 10, 10);
            IList<Rectangle> result = EmptySpaceFinder.Find(outer, new List<Rectangle>());
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(outer.Left, Is.EqualTo(result[0].Left));
            Assert.That(outer.Top, Is.EqualTo(result[0].Top));
            Assert.That(outer.Width, Is.EqualTo(result[0].Width));
            Assert.That(outer.Height, Is.EqualTo(result[0].Height));
        }

        /// <summary>
        /// Single inner rectangle that splits the outer rectangle into four maximal empty rectangles.
        /// </summary>
        [Test]
        public void Find_OneInnerRectangleSplitsSpace_ReturnsCorrectMaximals()
        {
            Rectangle outer = new(0, 0, 10, 10);
            List<Rectangle> inner = new() { new(3, 3, 4, 4) };
            HashSet<Rectangle> result = new(EmptySpaceFinder.Find(outer, inner));
            HashSet<Rectangle> expected = new()
            {
                new Rectangle(0, 0, 10, 3),    // Top
                new Rectangle(0, 3, 3, 4),     // Left
                new Rectangle(7, 3, 3, 4),     // Right
                new Rectangle(0, 7, 10, 3)     // Bottom
            };
            Assert.That(result.SetEquals(expected), Is.True);

            // Expect several maximal rectangles; ensure none overlap the inner and they fit inside outer
            foreach (Rectangle r in result)
            {
                Assert.That(outer.Contains(r), Is.True);
            }

            Assert.That(result.All(r => r.Width > 0 && r.Height > 0), Is.True);
        }

        [Test]
        public void NoVerticalPlaceLeft()
        {
            Rectangle outer = new(0, 0, 4, 4);
            List<Rectangle> inner = new() { new(0, 0, 2, 4), new(2, 0, 2, 4) };
            HashSet<Rectangle> result = new(EmptySpaceFinder.Find(outer, inner));
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void NoHorizontalPlaceLeft()
        {
            Rectangle outer = new(0, 0, 4, 4);
            List<Rectangle> inner = new() { new(0, 0, 4, 2), new(0, 2, 4, 2) };
            HashSet<Rectangle> result = new(EmptySpaceFinder.Find(outer, inner));
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void NoPlaceleft()
        {
            Rectangle outer = new(0, 0, 2, 2);
            List<Rectangle> inner = new() {
                new(0, 0, 1, 1), // TopLeft
                new(0, 1, 1, 1), // TopRight
                new(1, 0, 1, 1), // BottomLeft
                new(1, 1, 1, 1)  // BottomRight
            };
            HashSet<Rectangle> result = new(EmptySpaceFinder.Find(outer, inner));
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void OneTopLeft()
        {
            Rectangle outer = new(0, 0, 2, 2);
            Rectangle topLeft = new(0, 0, 1, 1);
            Rectangle topRight = new(0, 1, 1, 1);
            Rectangle bottomLeft = new(1, 0, 1, 1);
            Rectangle bottomRight = new(1, 1, 1, 1);
            List<Rectangle> inner = new() {
                topRight,
                bottomLeft,
                bottomRight
            };
            HashSet<Rectangle> result = new(EmptySpaceFinder.Find(outer, inner));
            HashSet<Rectangle> expected = new() { topLeft };
            Assert.That(result.SetEquals(expected), Is.True);
        }

        [Test]
        public void OneTopRight()
        {
            Rectangle outer = new(0, 0, 2, 2);
            Rectangle topLeft = new(0, 0, 1, 1);
            Rectangle topRight = new(0, 1, 1, 1);
            Rectangle bottomLeft = new(1, 0, 1, 1);
            Rectangle bottomRight = new(1, 1, 1, 1);
            List<Rectangle> inner = new() {
                topLeft,
                bottomLeft,
                bottomRight
            };
            HashSet<Rectangle> result = new(EmptySpaceFinder.Find(outer, inner));
            HashSet<Rectangle> expected = new() { topRight };
            Assert.That(result.SetEquals(expected), Is.True);
        }

        [Test]
        public void OneBottomLeft()
        {
            Rectangle outer = new(0, 0, 2, 2);
            Rectangle topLeft = new(0, 0, 1, 1);
            Rectangle topRight = new(0, 1, 1, 1);
            Rectangle bottomLeft = new(1, 0, 1, 1);
            Rectangle bottomRight = new(1, 1, 1, 1);
            List<Rectangle> inner = new() {
                topLeft,
                topRight,
                bottomRight
            };
            HashSet<Rectangle> result = new(EmptySpaceFinder.Find(outer, inner));
            HashSet<Rectangle> expected = new() { bottomLeft };
            Assert.That(result.SetEquals(expected), Is.True);
        }

        [Test]
        public void OneBottomRight()
        {
            Rectangle outer = new(0, 0, 2, 2);
            Rectangle topLeft = new(0, 0, 1, 1);
            Rectangle topRight = new(0, 1, 1, 1);
            Rectangle bottomLeft = new(1, 0, 1, 1);
            Rectangle bottomRight = new(1, 1, 1, 1);
            List<Rectangle> inner = new() {
                topLeft,
                topRight,
                bottomLeft,
            };
            HashSet<Rectangle> result = new(EmptySpaceFinder.Find(outer, inner));
            HashSet<Rectangle> expected = new() { bottomRight };
            Assert.That(result.SetEquals(expected), Is.True);
        }

        [Test]
        public void Multiple()
        {
            Rectangle outer = new(0, 0, 10, 9);
            List<Rectangle> inner = new() {
                new Rectangle(5, 0, 5 ,1),
                new Rectangle(1, 1, 3, 2),
                new Rectangle(5, 2, 1, 7),
                new Rectangle(6, 2, 3, 3),
                new Rectangle(0, 4, 2, 2),
                new Rectangle(3, 5, 3, 1),
                new Rectangle(9, 7, 1, 1),
                new Rectangle(8, 8, 1, 1),
            };
            HashSet<Rectangle> result = new(EmptySpaceFinder.Find(outer, inner));
            HashSet<Rectangle> expected = new() { };

            foreach (Rectangle r in result)
            {
                Debug.Log(r + "\n");
            }
            Assert.That(result.SetEquals(expected), Is.True);
        }

        [Test]
        public void SubtractInterval_RemovesAndSplitsGapsCorrectly()
        {
            List<Gap> gaps = new List<Gap> { new Gap { Begin = 0, End = 10 } };
            MethodInfo mi = typeof(EmptySpaceFinder).GetMethod("SubtractInterval", BindingFlags.NonPublic | BindingFlags.Static)!;
            List<Gap>? newGaps = mi.Invoke(null, new object[] { gaps, 3, 7 }) as List<Gap>;

            Assert.That(newGaps, Is.Not.Null);
            Assert.That(newGaps!.Count, Is.EqualTo(2));
            Assert.That(newGaps.Exists(g => g.Begin == 0 && g.End == 3), Is.True);
            Assert.That(newGaps.Exists(g => g.Begin == 7 && g.End == 10), Is.True);
        }

        [Test]
        public void AreAllNested_DetectsNonNestedCorrectly()
        {
            Rectangle outer = new Rectangle(0, 0, 10, 10);
            List<Rectangle> innerList = new List<Rectangle> { new Rectangle(1, 1, 2, 2), new Rectangle(20, 20, 2, 2) };
            MethodInfo method = typeof(EmptySpaceFinder).GetMethod("AreAllNested", BindingFlags.NonPublic | BindingFlags.Static)!;

            bool result = (bool)method.Invoke(null, new object[] { outer, innerList })!;
            Assert.That(result, Is.False);
        }

        [Test]
        public void PostProcessMaximalRectangles_FiltersContainedRectangles()
        {
            List<Rectangle> rects = new List<Rectangle>
            {
                new Rectangle(0,0,10,10),
                new Rectangle(1,1,2,2),
                new Rectangle(5,5,2,2)
            };

            MethodInfo method = typeof(EmptySpaceFinder).GetMethod("PostProcessMaximalRectangles", BindingFlags.NonPublic | BindingFlags.Static)!;

            List<Rectangle>? result = method.Invoke(null, new object[] { rects }) as List<Rectangle>;
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Exists(r => r.Left == 0 && r.Top == 0 && r.Width == 10 && r.Height == 10));
            Assert.That(result.Exists(r => r.Left == 1 && r.Top == 1 && r.Width == 2 && r.Height == 2), Is.False);
        }
    }
}
