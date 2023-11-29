using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace Assets.SEE.Game.Drawable
{
    public static class GameShapesCalculator
    {
        public enum Shapes
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
            Polygon
        }

        public static List<Shapes> GetShapes()
        {
            return Enum.GetValues(typeof(Shapes)).Cast<Shapes>().ToList();
        }

        public static Vector3[] Square(Vector3 point, float a)
        {
            Vector3 A = new Vector3(point.x - a / 2, point.y - a / 2, 0) - ValueHolder.distanceToDrawable;
            Vector3 B = new Vector3(A.x + a, A.y, 0) - ValueHolder.distanceToDrawable;
            Vector3 C = new Vector3(B.x, B.y + a, 0) - ValueHolder.distanceToDrawable;
            Vector3 D = new Vector3(A.x, A.y + a, 0) - ValueHolder.distanceToDrawable;
            return new Vector3[] { A, B, C, D };
        }

        public static Vector3[] Rectanlge(Vector3 point, float a, float b)
        {
            Vector3 A = new Vector3(point.x - a / 2, point.y - b / 2, 0) - ValueHolder.distanceToDrawable;
            Vector3 B = new Vector3(A.x + a, A.y, 0) - ValueHolder.distanceToDrawable;
            Vector3 C = new Vector3(B.x, B.y + b, 0) - ValueHolder.distanceToDrawable;
            Vector3 D = new Vector3(A.x, A.y + b, 0) - ValueHolder.distanceToDrawable;
            return new Vector3[] { A, B, C, D };
        }

        public static Vector3[] Rhombus(Vector3 point, float f, float e)
        {
            Vector3 A = new Vector3(point.x - e / 2, point.y, 0) - ValueHolder.distanceToDrawable;
            Vector3 B = new Vector3(point.x, point.y - f / 2, 0) - ValueHolder.distanceToDrawable;
            Vector3 C = new Vector3(point.x + e / 2, point.y, 0) - ValueHolder.distanceToDrawable;
            Vector3 D = new Vector3(point.x, point.y + f / 2, 0) - ValueHolder.distanceToDrawable;
            return new Vector3[] { A, B, C, D };
        }

        public static Vector3[] Kite(Vector3 point, float f1, float f2, float e)
        {
            Vector3 A = new Vector3(point.x - e / 2, point.y, 0) - ValueHolder.distanceToDrawable;
            Vector3 B = new Vector3(point.x, point.y - f2, 0) - ValueHolder.distanceToDrawable;
            Vector3 C = new Vector3(point.x + e / 2, point.y, 0) - ValueHolder.distanceToDrawable;
            Vector3 D = new Vector3(point.x, point.y + f1, 0) - ValueHolder.distanceToDrawable;
            return new Vector3[] { A, B, C, D };
        }
        /// <summary>
        /// Isosceles triangle
        /// </summary>
        /// <param name="point"></param>
        /// <param name="c"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static Vector3[] Triangle(Vector3 point, float c, float h)
        {
            Vector3 A = new Vector3(point.x - c / 2, point.y - h / 2, 0) - ValueHolder.distanceToDrawable;
            Vector3 B = new Vector3(point.x + c / 2, point.y - h / 2, 0) - ValueHolder.distanceToDrawable;
            Vector3 C = new Vector3(point.x, point.y + h / 2, 0) - ValueHolder.distanceToDrawable;
            return new Vector3[] { A, B, C };
        }

        public static Vector3[] Circle(Vector3 point, float radius)
        {
            return Ellipse(point, radius, radius);
        }

        public static Vector3[] Ellipse(Vector3 point, float xScale, float yScale)
        {
            int vertices = 50;
            return Polygon(point, xScale, yScale, vertices);
        }


        public static Vector3[] Parallelogram(Vector3 point, float a, float h, float offset)
        {
            Vector3 A = new Vector3(point.x - a / 2, point.y - h / 2, 0) - ValueHolder.distanceToDrawable;
            Vector3 B = new Vector3(point.x + a / 2, point.y - h / 2, 0) - ValueHolder.distanceToDrawable;
            Vector3 C = new Vector3(B.x + offset, B.y + h, 0) - ValueHolder.distanceToDrawable;
            Vector3 D = new Vector3(A.x + offset, A.y + h, 0) - ValueHolder.distanceToDrawable;
            return new Vector3[] { A, B, C, D };
        }

        /// <summary>
        /// Calculates the points for a isosceles trapezoid
        /// </summary>
        /// <param name="point">the middle point. get it from the raycast hit point.</param>
        /// <param name="a">the longest side.</param>
        /// <param name="c">the short side.</param>
        /// <param name="h">the height.</param>
        /// <returns>the calculated points of the trapezoid.</returns>
        public static Vector3[] Trapezoid(Vector3 point, float a, float c, float h)
        {
            Vector3 A = new Vector3(point.x - a / 2, point.y - h / 2, 0) - ValueHolder.distanceToDrawable;
            Vector3 B = new Vector3(point.x + a / 2, point.y - h / 2, 0) - ValueHolder.distanceToDrawable;
            Vector3 C = new Vector3(point.x + c / 2, point.y + h / 2, 0) - ValueHolder.distanceToDrawable;
            Vector3 D = new Vector3(point.x - c / 2, point.y + h / 2, 0) - ValueHolder.distanceToDrawable;

            return new Vector3[] { A, B, C, D };
        }

        public static Vector3[] Polygon(Vector3 point, float length, int vertices)
        {
            return Polygon(point, length, length, vertices);
        }

        //inspired by EricBalcon from comment of https://www.youtube.com/watch?v=DdAfwHYNFOE
        private static Vector3[] Polygon(Vector3 point, float xScale, float yScale, int vertices)
        {
            Vector3[] positions = new Vector3[vertices];
            float angle = 0f;
            for (int i = 0; i < vertices; i++)
            {
                float x = xScale * Mathf.Sin(angle);
                float y = yScale * Mathf.Cos(angle);
                positions[i] = new Vector3(point.x + x, point.y + y, point.z);
                angle += 2f * Mathf.PI / vertices;
            }
            return positions;
        }

        public static Vector3[] MindMapRectangle(Vector3 point, float a, float b)
        {
            float splittedA = a / 12;
            float splittedB = b / 12;

            Vector3 A = new Vector3(point.x - a / 2, point.y - b / 2, 0) - ValueHolder.distanceToDrawable;
            Vector3[] AB = new Vector3[14];
            AB[0] = A;
            for(int i = 1; i < 13; i++)
            {
                AB[i] = new Vector3(AB[i-1].x + splittedA, AB[i-1].y, 0) - ValueHolder.distanceToDrawable;
            }
            Vector3 B = new Vector3(A.x + a, A.y, 0) - ValueHolder.distanceToDrawable;
            AB[13] = B;
            Vector3[] BC = new Vector3[14];
            BC[0] = B;
            for (int i = 1; i < 13; i++)
            {
                BC[i] = new Vector3(BC[i - 1].x, BC[i - 1].y + splittedB, 0) - ValueHolder.distanceToDrawable;
            }
            
            Vector3 C = new Vector3(B.x, B.y + b, 0) - ValueHolder.distanceToDrawable;
            BC[13] = C;
            Vector3[] CD = new Vector3[14];
            CD[0] = C;
            for (int i = 1; i < 13; i++)
            {
                CD[i] = new Vector3(CD[i - 1].x - splittedA, CD[i - 1].y, 0) - ValueHolder.distanceToDrawable;
            }
            
            Vector3 D = new Vector3(A.x, A.y + b, 0) - ValueHolder.distanceToDrawable;
            CD[13] = D;
            Vector3[] DA = new Vector3[14];
            DA[0] = D;
            for (int i = 1; i < 13; i++)
            {
                DA[i] = new Vector3(DA[i - 1].x, DA[i - 1].y - splittedB, 0) - ValueHolder.distanceToDrawable;
            }
            DA[13] = A;
            
            Vector3[] all = new Vector3[AB.Length + BC.Length + CD.Length + DA.Length];
            AB.CopyTo(all, 0);
            BC.CopyTo(all, AB.Length);
            CD.CopyTo(all, AB.Length + BC.Length);
            DA.CopyTo(all, AB.Length + BC.Length + CD.Length);
            return all;
        }
    }
}