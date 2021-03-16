using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SEE.Utils
{
    /// <summary>
    /// This class is provides median calculation of vectors.
    /// </summary>
    public static class Medians
    {
        /// <summary>
        /// Returns the median over all dimension of the given <paramref name="vectors"/>,
        /// where each element (x/y/z) of the resulting vector is the median
        /// of the respective elements (x/y/z) of the input <paramref name="vectors"/>.
        /// To put it differently (let result be the returned value):
        /// 
        ///   result.x = median over set {v.x | v in <paramref name="vectors"/>}
        ///   result.y = median over set {v.y | v in <paramref name="vectors"/>}
        ///   result.z = median over set {v.z | v in <paramref name="vectors"/>}
        ///   
        /// Precondition: vectors != null && vectors.Count > 0. Otherwise an
        /// ArgumentException will be thrown.
        /// </summary>
        /// <param name="vectors">vectors whose three medians are to be determined</param>
        /// <returns>three medians over the three dimensions as described above</returns>
        public static Vector3 Median(ICollection<Vector3> vectors)
        {
            if (vectors == null || vectors.Count == 0)
            {
                throw new System.ArgumentException("Input must neither be null nor empty.");
            }

            switch (vectors.Count)
            {
                // We can just return the single element of the list.
                case 1:
                    return vectors.ElementAt(0);

                // As there are only two elements, which is an even number, we return the average.
                case 2:
                    return (vectors.ElementAt(0) + vectors.ElementAt(1)) / 2;

                // If there are more than two elements:
                default:
                    List<float> xAxis = new List<float>();
                    List<float> yAxis = new List<float>();
                    List<float> zAxis = new List<float>();

                    foreach (Vector3 vect in vectors)
                    {
                        xAxis.Add(vect.x);
                        yAxis.Add(vect.y);
                        zAxis.Add(vect.z);
                    }
                    return new Vector3(Median(xAxis), Median(yAxis), Median(zAxis));
            }
        }

        /// <summary>
        /// Calculates the median over <paramref name="values"/>. If <paramref name="values"/>
        /// has an odd number of elements, the value in the middle of the sorted list
        /// will be returned. If there is an even number instead, we return the
        /// average of the two values in the middle of the sorted list of values.
        /// 
        /// Precondition: <paramref name="values"/> must neither be null
        /// nor empty. Otherwise an ArgumentException is thrown.
        /// 
        /// Note: <paramref name="values"/> may or may not be sorted. <paramref name="values"/>
        /// will not be modified.
        /// <param name="values">list of values for which to calculate the median</param>
        /// <returns>the median value over <paramref name="values"/></returns>
        public static float Median(ICollection<float> values)
        {
            if (values == null || values.Count == 0)
            {
                throw new System.ArgumentException("Input must neither be null nor empty.");
            }
            switch (values.Count)
            {
                // We can just return the single element of the list.
                case 1:
                    return values.ElementAt(0);

                // As the length is just two, which is an even number, we return the average.
                case 2:
                    return (values.ElementAt(0) + values.ElementAt(1)) / 2;

                default:
                    // If there are more than two elements:
                    // We make a copy of values that we will sort. We do not want to change
                    // the original order of values.
                    List<float> list = values.ToList<float>();
                    list.Sort();
                    // Note that the integer division will truncate to the next lower integer value.
                    int indexOfMedian = values.Count / 2;

                    // If the lists has an odd number of elements, we can just return the entry stored 
                    // directly "in the middle".
                    if ((values.Count % 2 != 0))
                    {
                        return list.ElementAt(indexOfMedian);
                    }
                    else
                    {
                        // otherwise we return the average of the two values in the middle
                        return (values.ElementAt(indexOfMedian) + values.ElementAt(indexOfMedian - 1)) / 2;
                    }
            }
        }
    }
}