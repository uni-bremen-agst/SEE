using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// This class provides the position calculation for the shapes.
    /// </summary>
    public static class ShapePointsCalculator
    {
        /// <summary>
        /// The different kinds of a shape.
        /// </summary>
        public enum Shape
        {
            Line,
            Square,
            Rectangle,
            Rhombus,
            Kite,
            Triangle,
            Circle,
            Ellipse,
            Parallelogram,
            Trapezoid,
            Polygon,
        }

        /// <summary>
        /// Gets a list with all shape kinds.
        /// </summary>
        /// <returns>A list that holds all shape kinds.</returns>
        public static List<Shape> GetShapes()
        {
            return Enum.GetValues(typeof(Shape)).Cast<Shape>().ToList();
        }

        /// <summary>
        /// Calculates the positions for a square.
        /// The last point is again the starting point.
        /// Thus, the square consists of five points.
        /// </summary>
        /// <param name="point">The middle point of the square. It's the hit point on the drawable.</param>
        /// <param name="a">The length for the edge</param>
        /// <returns>The positions for the square</returns>
        public static Vector3[] Square(Vector3 point, float a)
        {
            Vector3 A = new Vector3(point.x - a / 2, point.y - a / 2, 0) - ValueHolder.DistanceToDrawable;
            Vector3 B = new Vector3(A.x + a, A.y, 0) - ValueHolder.DistanceToDrawable;
            Vector3 C = new Vector3(B.x, B.y + a, 0) - ValueHolder.DistanceToDrawable;
            Vector3 D = new Vector3(A.x, A.y + a, 0) - ValueHolder.DistanceToDrawable;
            return new Vector3[] { A, B, C, D, A };
        }

        /// <summary>
        /// Calculates the positions for a rectangle.
        /// The last point is again the starting point.
        /// Thus, the rectangle consists of five points.
        /// </summary>
        /// <param name="point">The middle point of the rectangle. It's the hit point on the drawable.</param>
        /// <param name="a">Specifies the edge length of the a sides of the rectangle</param>
        /// <param name="b">Specifies the edge length of the b sides of the rectangle</param>
        /// <returns>The positions for the rectangle</returns>
        public static Vector3[] Rectangle(Vector3 point, float a, float b)
        {
            Vector3 A = new Vector3(point.x - a / 2, point.y - b / 2, 0) - ValueHolder.DistanceToDrawable;
            Vector3 B = new Vector3(A.x + a, A.y, 0) - ValueHolder.DistanceToDrawable;
            Vector3 C = new Vector3(B.x, B.y + b, 0) - ValueHolder.DistanceToDrawable;
            Vector3 D = new Vector3(A.x, A.y + b, 0) - ValueHolder.DistanceToDrawable;
            return new Vector3[] { A, B, C, D, A };
        }

        /// <summary>
        /// Calculates the positions for a rhombus.
        /// The last point is again the starting point.
        /// Thus, the rhombus consists of five points.
        /// </summary>
        /// <param name="point">The middle point of the rhombus. It's the hit point on the drawable.</param>
        /// <param name="f">Specifies the edge length of the f sides of the rhombus</param>
        /// <param name="e">Specifies the edge length of the e sides of the rhombus</param>
        /// <returns>The positions for the rhombus</returns>
        public static Vector3[] Rhombus(Vector3 point, float f, float e)
        {
            Vector3 A = new Vector3(point.x - e / 2, point.y, 0) - ValueHolder.DistanceToDrawable;
            Vector3 B = new Vector3(point.x, point.y - f / 2, 0) - ValueHolder.DistanceToDrawable;
            Vector3 C = new Vector3(point.x + e / 2, point.y, 0) - ValueHolder.DistanceToDrawable;
            Vector3 D = new Vector3(point.x, point.y + f / 2, 0) - ValueHolder.DistanceToDrawable;
            return new Vector3[] { A, B, C, D, A };
        }

        /// <summary>
        /// Calculates the positions for a kite.
        /// The last point is again the starting point.
        /// Thus, the kite consists of five points.
        /// </summary>
        /// <param name="point">The middle point of the kite. It's the hit point on the drawable.</param>
        /// <param name="f1">Specifies the edge length of the f1 sides of the kite</param>
        /// <param name="f2">Specifies the edge length of the f1 sides of the kite</param>
        /// <param name="e">Specifies the edge length of the e sides of the kite</param>
        /// <returns>The positions for the kite</returns>
        public static Vector3[] Kite(Vector3 point, float f1, float f2, float e)
        {
            Vector3 A = new Vector3(point.x - e / 2, point.y, 0) - ValueHolder.DistanceToDrawable;
            Vector3 B = new Vector3(point.x, point.y - f2, 0) - ValueHolder.DistanceToDrawable;
            Vector3 C = new Vector3(point.x + e / 2, point.y, 0) - ValueHolder.DistanceToDrawable;
            Vector3 D = new Vector3(point.x, point.y + f1, 0) - ValueHolder.DistanceToDrawable;
            return new Vector3[] { A, B, C, D, A };
        }

        /// <summary>
        /// Calculates the positions for an isosceles triangle.
        /// The last point is again the starting point.
        /// Thus, the triangle consists of four points.
        /// </summary>
        /// <param name="point">The middle point of the triangle. It's the hit point on the drawable.</param>
        /// <param name="c">Specifies the edge length of the c side of the triangle</param>
        /// <param name="h">Specifies the height of the triangle</param>
        /// <returns>The positions for the triangle</returns>
        public static Vector3[] Triangle(Vector3 point, float c, float h)
        {
            Vector3 A = new Vector3(point.x - c / 2, point.y - h / 2, 0) - ValueHolder.DistanceToDrawable;
            Vector3 B = new Vector3(point.x + c / 2, point.y - h / 2, 0) - ValueHolder.DistanceToDrawable;
            Vector3 C = new Vector3(point.x, point.y + h / 2, 0) - ValueHolder.DistanceToDrawable;
            return new Vector3[] { A, B, C, A };
        }

        /// <summary>
        /// Calculates the positions for a circle.
        /// It used the <see cref="Ellipse(Vector3, float, float)"/> method for calculating.
        /// Where the x length and the y length correspond to the radius of the circle.
        /// </summary>
        /// <param name="point">The middle point of the circle. It's the hit point on the drawable.</param>
        /// <param name="radius">The length for the radius of the circle.</param>
        /// <returns>The positions for the circle.</returns>
        public static Vector3[] Circle(Vector3 point, float radius)
        {
            return Ellipse(point, radius, radius);
        }

        /// <summary>
        /// Calculates the positions for a ellipse.
        /// It used the <see cref="Polygon(Vector3, float, float, int)"/> method for calculating.
        /// For the vertices will be used a default by 50.
        /// </summary>
        /// <param name="point">The middle point of the ellipse. It's the hit point on the drawable.</param>
        /// <param name="xScale">Specifies the radius of the x length of the ellipse</param>
        /// <param name="yScale">Specifies the radius of the y length of the ellipse</param>
        /// <returns>The positions of the ellipse</returns>
        public static Vector3[] Ellipse(Vector3 point, float xScale, float yScale)
        {
            int vertices = 50;
            return Polygon(point, xScale, yScale, vertices);
        }

        /// <summary>
        /// Calculates the positions for a parallelogram.
        /// The last point is again the starting point.
        /// Thus, the parallelogram consists of five points.
        /// </summary>
        /// <param name="point">The middle point of the parallelogram. It's the hit point on the drawable.</param>
        /// <param name="a">Specifies the edge length of the a side of the parallelogram</param>
        /// <param name="h">Specifies the height parallelogram</param>
        /// <param name="offset">Specifies by how much points C and D of the parallelogram should be moved.
        /// If the offset is in the negative range, the points will be shifted to the left.
        /// In the positive range, to the right.</param>
        /// <returns>The positions of the parallelogram.</returns>
        public static Vector3[] Parallelogram(Vector3 point, float a, float h, float offset)
        {
            Vector3 A = new Vector3(point.x - a / 2, point.y - h / 2, 0) - ValueHolder.DistanceToDrawable;
            Vector3 B = new Vector3(point.x + a / 2, point.y - h / 2, 0) - ValueHolder.DistanceToDrawable;
            Vector3 C = new Vector3(B.x + offset, B.y + h, 0) - ValueHolder.DistanceToDrawable;
            Vector3 D = new Vector3(A.x + offset, A.y + h, 0) - ValueHolder.DistanceToDrawable;
            return new Vector3[] { A, B, C, D, A };
        }

        /// <summary>
        /// Calculates the positions for a isosceles trapezoid
        /// The last point is again the starting point.
        /// Thus, the trapezoid consists of five points.
        /// </summary>
        /// <param name="point">The middle point of the trapezoid. It's the hit point on the drawable.</param>
        /// <param name="a">Specifies the edge length of the a side (bottom side) of the trapezoid</param>
        /// <param name="c">Specifies the edge length of the c side (upper side) of the trapezoid</param>
        /// <param name="h">Specifies the height of the trapezoid</param>
        /// <returns>the calculated positions of the trapezoid.</returns>
        public static Vector3[] Trapezoid(Vector3 point, float a, float c, float h)
        {
            Vector3 A = new Vector3(point.x - a / 2, point.y - h / 2, 0) - ValueHolder.DistanceToDrawable;
            Vector3 B = new Vector3(point.x + a / 2, point.y - h / 2, 0) - ValueHolder.DistanceToDrawable;
            Vector3 C = new Vector3(point.x + c / 2, point.y + h / 2, 0) - ValueHolder.DistanceToDrawable;
            Vector3 D = new Vector3(point.x - c / 2, point.y + h / 2, 0) - ValueHolder.DistanceToDrawable;

            return new Vector3[] { A, B, C, D, A };
        }

        /// <summary>
        /// Calculates the positions for a polygon.
        /// The last point is again the starting point.
        /// Thus, the polygon consists of vertices + 1 points.
        /// </summary>
        /// <param name="point">The middle point of the polygon. It's the hit point on the drawable.</param>
        /// <param name="length">Specifies the edge length of the polygon</param>
        /// <param name="vertices">Specifies how much vertices the polygon should have</param>
        /// <returns>The calculated positions of the polygon.</returns>
        public static Vector3[] Polygon(Vector3 point, float length, int vertices)
        {
            return Polygon(point, length, length, vertices);
        }

        /// <summary>
        /// Calculates the positions of a polygon with X and Y radius lengths.
        /// The last point is again the starting point.
        /// Thus, the polygon consits of vertices + 1 points.
        /// The method was inspired by EricBalcon from comment of https://www.youtube.com/watch?v=DdAfwHYNFOE (last visite 12.12.2023)
        /// </summary>
        /// <param name="point">The middle point of the polygon. It's the hit point on the drawable.</param>
        /// <param name="xScale">Specifies the radius of the x length of the polygon</param>
        /// <param name="yScale">Specifies the radius of the y length of the polygon</param>
        /// <param name="vertices">Specifies how much vertices the polygon should have</param>
        /// <returns>The positions of the polygon</returns>
        private static Vector3[] Polygon(Vector3 point, float xScale, float yScale, int vertices)
        {
            Vector3[] positions = new Vector3[vertices + 1];
            float angle = 0f;
            for (int i = 0; i < vertices; i++)
            {
                float x = xScale * Mathf.Sin(angle);
                float y = yScale * Mathf.Cos(angle);
                positions[i] = new Vector3(point.x + x, point.y + y, point.z);
                angle += 2f * Mathf.PI / vertices;
            }
            positions[vertices] = positions[0];
            return positions;
        }

        /// <summary>
        /// Calculates the position for a mind map rectangle.
        /// It consists of 56 points.
        /// The distances between the edges have been reduced so that the node connections
        /// between MindMap nodes can be presented more attractively.
        /// A branch line chooses the nearest point from the borderline.
        /// With the conventional five points, there are not as many options,
        /// and the connecting lines would always be on the corner edges of the rectangle.
        /// </summary>
        /// <param name="point">The middle point of the rectangle. It's the hit point on the drawable.</param>
        /// <param name="a">Specifies the edge length of the a sides of the rectangle</param>
        /// <param name="b">Specifies the edge length of the b sides of the rectangle</param>
        /// <returns>The positions for the rectangle</returns>
        public static Vector3[] MindMapRectangle(Vector3 point, float a, float b)
        {
            float splitA = a / 12;
            float splitB = b / 12;

            Vector3 A = new Vector3(point.x - a / 2, point.y - b / 2, 0) - ValueHolder.DistanceToDrawable;

            /// Calculates the points between A and B.
            Vector3[] AB = new Vector3[14];
            AB[0] = A;
            for (int i = 1; i < 13; i++)
            {
                AB[i] = new Vector3(AB[i - 1].x + splitA, AB[i - 1].y, 0) - ValueHolder.DistanceToDrawable;
            }

            Vector3 B = new Vector3(A.x + a, A.y, 0) - ValueHolder.DistanceToDrawable;
            AB[13] = B;

            /// Calculates the points between B and C.
            Vector3[] BC = new Vector3[14];
            BC[0] = B;
            for (int i = 1; i < 13; i++)
            {
                BC[i] = new Vector3(BC[i - 1].x, BC[i - 1].y + splitB, 0) - ValueHolder.DistanceToDrawable;
            }

            Vector3 C = new Vector3(B.x, B.y + b, 0) - ValueHolder.DistanceToDrawable;
            BC[13] = C;

            /// Calculates the points between C and D.
            Vector3[] CD = new Vector3[14];
            CD[0] = C;
            for (int i = 1; i < 13; i++)
            {
                CD[i] = new Vector3(CD[i - 1].x - splitA, CD[i - 1].y, 0) - ValueHolder.DistanceToDrawable;
            }

            Vector3 D = new Vector3(A.x, A.y + b, 0) - ValueHolder.DistanceToDrawable;
            CD[13] = D;

            /// Calculates the points between D and A.
            Vector3[] DA = new Vector3[14];
            DA[0] = D;
            for (int i = 1; i < 13; i++)
            {
                DA[i] = new Vector3(DA[i - 1].x, DA[i - 1].y - splitB, 0) - ValueHolder.DistanceToDrawable;
            }
            DA[13] = A;

            /// Concatenates all arrays.
            Vector3[] all = new Vector3[AB.Length + BC.Length + CD.Length + DA.Length];
            AB.CopyTo(all, 0);
            BC.CopyTo(all, AB.Length);
            CD.CopyTo(all, AB.Length + BC.Length);
            DA.CopyTo(all, AB.Length + BC.Length + CD.Length);
            return all;
        }
    }
}
