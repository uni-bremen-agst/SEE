using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This class is responsible for the median-calculation of the vectors and sizes of gameObjects of a city.
/// It is important for the default node-size.
/// </summary>
public static class MathFunctions
{

    /// <summary>
    /// Calculates the median of a vector list. PreCondition: The list of vectors does not have to be sorted.
    /// </summary>
    /// <param name="vectors"></param>
    /// <returns> A vector3 with the calculated median of the vector list or null in 
    /// case the given vector list is empty or null itself</returns>
    public static Vector3 CalcMedian(List<Vector3> vectors)
    {
        if (vectors.Count == 0 || vectors == null)
        {
            return new Vector3(0, 0, 0);
        }

        Vector3 result = new Vector3();
        List<float> xAxis = new List<float>();
        List<float> yAxis = new List<float>();
        List<float> zAxis = new List<float>();

        foreach (Vector3 vect in vectors)
        {
            xAxis.Add(vect.x);
            yAxis.Add(vect.y);
            zAxis.Add(vect.z);
        }

        xAxis.Sort();
        yAxis.Sort();
        zAxis.Sort();

        result.x = CalcMedian(xAxis);
        result.y = CalcMedian(yAxis);
        result.z = CalcMedian(zAxis);

        if (!(vectors.Count % 2 == 0))
        {
            return result;
        }

        int indexSecondMedian = (xAxis.Count + 1) / 2;
        float SecondXCoordinate = CalcMedian(xAxis);
        float SecondYCoordinate = CalcMedian(yAxis);
        float SecondZCoordinate = CalcMedian(zAxis);

        result.x = (result.x + SecondXCoordinate) / 2;
        result.y = (result.y + SecondYCoordinate) / 2;
        result.z = (result.z + SecondZCoordinate) / 2;

        return result;
    }


    /// <summary>
    /// Calculates the median of a list of floats. Precondition: The list of vectors does not have to be sorted.
    /// </summary>
    /// <param name="floatList"></param>
    /// <returns> The single median of the list as a float or null in 
    /// case the given vector list is empty or null itself</returns>
    public static float CalcMedian(List<float> floatList)
    {
        float median = 0;
        if (floatList.Count == 0 | floatList == null)
        {
            return median;
        }
        int indexOfMid = floatList.Count;
        indexOfMid /= 2;
        if (indexOfMid != 0)
        {
            indexOfMid -= 1;
        }
        median = floatList.ElementAt(indexOfMid);

        // If the amount of the list is impair, we will return the element which is located at the middle of the list,
        // e.g. the amount = 13 , i.e the element at the index 6.
        if (!(floatList.Count % 2 == 0))
        {
            return median;
        }

        // If the amount is impair, we have to interpolate linearly between the value at the index at the half of the lists size,
        // and the value of the following index. E.g. size = 13 -> index 6 and 7 .
        int indexSecondMedianValue = indexOfMid + 1;
        float SecondCoordinate = floatList.ElementAt(indexSecondMedianValue);
        median += SecondCoordinate;
        median /= 2;
        return median;
    }

    public static float medianOfFloats(List<float> floatList)
    {
        int lengthOfList = floatList.Count;
        float median = 0;


        switch (lengthOfList)
        {
            //nothing to be calculated, list is empty
            case 0:
                return median;

            // we can just return the single element of the list
            case 1:
                return floatList.ElementAt(0);

            //As the amount of the length of the list is pair, we have to interpolate linearly and divide the result by 2 .
            case 2:
                median = floatList.ElementAt(0) + floatList.ElementAt(1);
                return median / 2;

            default:
                // this is the case, the list consists of more than 2 entries and we have to determine the median.
                floatList.Sort();
                int indexOfMedian = floatList.Count / 2;
                indexOfMedian -= 1;

                //if the lists length is impair, we can just return the entry stored directly "in the middle".
                if (!(floatList.Count % 2 == 0))
                {
                    return median;
                }
                // else we have to add the next entry and divive the sum of each entries by 2 
                median = median + (floatList.ElementAt(indexOfMedian + 1));
                return median / 2;
        }

    }
}
