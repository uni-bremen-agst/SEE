using UnityEngine;
using System.Collections;
using Curve;
using System.Collections.Generic;
using TinySpline;

namespace Assets.SEE.Utils
{
    public class SplineCurve : Curve.Curve
    {
        private BSpline spline;

        public SplineCurve(List<Vector3> points) : base(points)
        {
            IList<double> list = new List<double>();
            foreach( var p in points)
            {
                list.Add(p.x);
                list.Add(p.y);
                list.Add(p.z);
            }

            spline = BSpline.InterpolateCatmullRom(list, 3, 0.5);
        }


        protected override Vector3 GetPoint(float t)
        {
            IList<double> list = spline.Eval(t).Result;

            return new Vector3((float)list[0], (float)list[1], (float)list[2]);
        }
    }
}