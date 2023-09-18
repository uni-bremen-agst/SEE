using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.Game;

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
            Polygon//,
            //Pentagon
            //Hexagon
            //Octagon
        }

        public static List<Shapes> GetShapes()
        {
            return Enum.GetValues(typeof(Shapes)).Cast<Shapes>().ToList();
        }

        public static Vector3[] Square(Vector3 point, float a)
        {
            Vector3 A = new Vector3(point.x - a / 2, point.y - a / 2, 0) - DrawableHelper.distanceToBoard;
            Vector3 B = new Vector3(A.x + a, A.y, 0) - DrawableHelper.distanceToBoard;
            Vector3 C = new Vector3(B.x, B.y + a, 0) - DrawableHelper.distanceToBoard;
            Vector3 D = new Vector3(A.x, A.y + a, 0) - DrawableHelper.distanceToBoard;
            return new Vector3[] { A, B, C, D };
        }

        public static Vector3[] Rectanlge(Vector3 point, float a, float b)
        {
            Vector3 A = new Vector3(point.x - a / 2, point.y - b / 2, 0) - DrawableHelper.distanceToBoard;
            Vector3 B = new Vector3(A.x + a, A.y, 0) - DrawableHelper.distanceToBoard;
            Vector3 C = new Vector3(B.x, B.y + b, 0) - DrawableHelper.distanceToBoard;
            Vector3 D = new Vector3(A.x, A.y + b, 0) - DrawableHelper.distanceToBoard;
            return new Vector3[] { A, B, C, D };
        }

        public static Vector3[] Rhombus(Vector3 point, float f, float e)
        {
            Vector3 A = new Vector3(point.x - e / 2, point.y, 0) - DrawableHelper.distanceToBoard;
            Vector3 B = new Vector3(point.x, point.y - f / 2, 0) - DrawableHelper.distanceToBoard;
            Vector3 C = new Vector3(point.x + e / 2, point.y, 0) - DrawableHelper.distanceToBoard;
            Vector3 D = new Vector3(point.x, point.y + f / 2, 0) - DrawableHelper.distanceToBoard;
            return new Vector3[] { A, B, C, D };
        }

        public static Vector3[] Kite(Vector3 point, float f1, float f2, float e)
        {
            Vector3 A = new Vector3(point.x - e / 2, point.y, 0) - DrawableHelper.distanceToBoard;
            Vector3 B = new Vector3(point.x, point.y - f2, 0) - DrawableHelper.distanceToBoard;
            Vector3 C = new Vector3(point.x + e / 2, point.y, 0) - DrawableHelper.distanceToBoard;
            Vector3 D = new Vector3(point.x, point.y + f1, 0) - DrawableHelper.distanceToBoard;
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
            Vector3 A = new Vector3(point.x - c / 2, point.y - h / 2, 0) - DrawableHelper.distanceToBoard;
            Vector3 B = new Vector3(point.x + c / 2, point.y - h / 2, 0) - DrawableHelper.distanceToBoard;
            Vector3 C = new Vector3(point.x, point.y + h / 2, 0) - DrawableHelper.distanceToBoard;
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


        public static Vector3[] Parallelogram(Vector3 point, float a, float h, float offset)//float b, float alpha)
        {/*
            if (alpha == 90.0f)
            {
                Debug.Log(DateTime.Now + ", Call Rectangle");
                return Rectanlge(point, a, b);
            }

            float alpha2 = alpha > 90 ? alpha - 90 : alpha;
            float h = b * Mathf.Sin(alpha2);
            h = h < 0 ? h * -1 : h;
            float x = b * Mathf.Cos(alpha2);
            x = x < 0 ? x * -1 : x;
            Debug.Log(DateTime.Now + ", Alpha: "+ alpha + ", Alpha2: " + alpha2 + ", h: " + h + ", x: " + x);

            Vector3 A = new Vector3(point.x - a / 2, point.y - h / 2, 0) - DrawableHelper.distanceToBoard;
            Vector3 B = new Vector3(point.x + a / 2, point.y - h / 2, 0) - DrawableHelper.distanceToBoard;
            Vector3 C = new Vector3(B.x + x, B.y + h, 0) - DrawableHelper.distanceToBoard;
            Vector3 D = new Vector3(A.x + x, A.y + h, 0) - DrawableHelper.distanceToBoard;
            if (alpha > 90)
            {
                C = new Vector3(B.x - x, B.y + h, 0) - DrawableHelper.distanceToBoard;
                D = new Vector3(A.x - x, A.y + h, 0) - DrawableHelper.distanceToBoard;
            }
            // punkt D = new Vector3(a * sin(alpha), b * sin(alpha), 0)
            return new Vector3[] { A, B, C, D };
            */
            Debug.Log(DateTime.Now + ", a: " + a + ", h: " + h + ", offset: " + offset);
            Vector3 A = new Vector3(point.x - a / 2, point.y - h / 2, 0) - DrawableHelper.distanceToBoard;
            Vector3 B = new Vector3(point.x + a / 2, point.y - h / 2, 0) - DrawableHelper.distanceToBoard;
            Vector3 C = new Vector3(B.x + offset, B.y + h, 0) - DrawableHelper.distanceToBoard;
            Vector3 D = new Vector3(A.x + offset, A.y + h, 0) - DrawableHelper.distanceToBoard;
            Debug.Log(DateTime.Now + ", A: " + A + ", B: " + B + ", C: " + C + ", D: " + D);
            return new Vector3[] { A, B, C, D };
        }

        /// <summary>
        /// Calculates the points for a isosceles trapezoid
        /// </summary>
        /// <param name="point">the middle point. get it from the raycast hit point.</param>
        /// <param name="a">the longest side.</param>
        /// <param name="b">the side line. b = d</param>
        /// <param name="alpha">the degree to draw. alpha = beta</param>
        /// <returns>the calculated points of the trapezoid.</returns>
        public static Vector3[] Trapezoid(Vector3 point, float a, float c, float h)//float alpha)
        {
            /*
            if (alpha == 90)
            {
                return Rectanlge(point, a, b);
            }
            float alpha2 = alpha > 90 ? alpha - 90 : alpha;
            float h = b * Mathf.Sin(alpha2);
            float x = b * Mathf.Cos(alpha2);

            Vector3 A = new Vector3(point.x - a / 2, point.y - h / 2, 0) - DrawableHelper.distanceToBoard;
            Vector3 B = new Vector3(point.x + a / 2, point.y - h / 2, 0) - DrawableHelper.distanceToBoard;
            Vector3 C = new Vector3(B.x - x, B.y + h, 0) - DrawableHelper.distanceToBoard;
            Vector3 D = new Vector3(A.x + x, A.y + h, 0) - DrawableHelper.distanceToBoard;

            if (alpha > 90)
            {
                C = new Vector3(B.x + x, B.y + h, 0) - DrawableHelper.distanceToBoard;
                D = new Vector3(A.x - x, A.y + h, 0) - DrawableHelper.distanceToBoard;
            }

            return new Vector3[] { A, B, C, D };
            */
            Vector3 A = new Vector3(point.x - a / 2, point.y - h / 2, 0) - DrawableHelper.distanceToBoard;
            Vector3 B = new Vector3(point.x + a / 2, point.y - h / 2, 0) - DrawableHelper.distanceToBoard;
            Vector3 C = new Vector3(point.x + c / 2, point.y + h / 2, 0) - DrawableHelper.distanceToBoard;
            Vector3 D = new Vector3(point.x - c / 2, point.y + h / 2, 0) - DrawableHelper.distanceToBoard;

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
    }
}