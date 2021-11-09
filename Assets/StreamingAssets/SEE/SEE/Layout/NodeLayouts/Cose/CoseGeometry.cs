// Copyright 2020 Nina Unterberg
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO
// EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR
// THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.Cose
{
    public static class CoseGeometry
    {
        /// <summary>
        /// Calculates the amount of intersection between two rectangles
        /// </summary>
        /// <param name="rectA">the first rectangle</param>
        /// <param name="rectB">the second rectangle</param>
        /// <param name="result">the result values</param>
        /// <returns>a tuple(intersect, result), intersect indictates if they intersect and result contains the resulting values</returns>
        public static Tuple<bool, double[]> GetIntersection(Rect rectA, Rect rectB, double[] result)
        {
            //result[0-1] will contain clipPoint of rectA, result[2-3] will contain clipPoint of rectB

            double p1x = rectA.center.x;
            double p1y = rectA.center.y;
            double p2x = rectB.center.x;
            double p2y = rectB.center.y;

            //if two rectangles intersect, then clipping points are centers
            if (rectA.Overlaps(rectB))
            {
                result[0] = p1x;
                result[1] = p1y;
                result[2] = p2x;
                result[3] = p2y;
                return new Tuple<bool, double[]>(true, result);
            }

            //variables for rectA
            double topLeftAx = rectA.x;
            double topLeftAy = rectA.y;
            double topRightAx = rectA.x + rectA.width;
            double bottomLeftAx = rectA.x;
            double bottomLeftAy = rectA.y + rectA.height;
            double bottomRightAx = rectA.x + rectA.width;
            double halfWidthA = rectA.width / 2;
            double halfHeightA = rectA.height / 2;

            //variables for rectB
            double topLeftBx = rectB.x;
            double topLeftBy = rectB.y;
            double topRightBx = rectB.x + rectB.width;
            double bottomLeftBx = rectB.x;
            double bottomLeftBy = rectB.y + rectB.height;
            double bottomRightBx = rectB.x + rectB.width;
            double halfWidthB = rectB.width / 2;
            double halfHeightB = rectB.height / 2;

            //flag whether clipping points are found
            bool clipPointAFound = false;
            bool clipPointBFound = false;


            // line is vertical
            if (p1x == p2x)
            {
                if (p1y > p2y)
                {
                    result[0] = p1x;
                    result[1] = topLeftAy;
                    result[2] = p2x;
                    result[3] = bottomLeftBy;
                    return new Tuple<bool, double[]>(false, result);
                }
                else if (p1y < p2y)
                {
                    result[0] = p1x;
                    result[1] = bottomLeftAy;
                    result[2] = p2x;
                    result[3] = topLeftBy;
                    return new Tuple<bool, double[]>(false, result);
                }
                else
                {
                    //not line, return null;
                }
            }
            // line is horizontal
            else if (p1y == p2y)
            {
                if (p1x > p2x)
                {
                    result[0] = topLeftAx;
                    result[1] = p1y;
                    result[2] = topRightBx;
                    result[3] = p2y;
                    return new Tuple<bool, double[]>(false, result);
                }
                else if (p1x < p2x)
                {
                    result[0] = topRightAx;
                    result[1] = p1y;
                    result[2] = topLeftBx;
                    result[3] = p2y;
                    return new Tuple<bool, double[]>(false, result);
                }
                else
                {
                    //not valid line, return null;
                }
            }
            else
            {
                //slopes of rectA's and rectB's diagonals
                double slopeA = rectA.height / rectA.width;
                double slopeB = rectB.height / rectB.width;

                //slope of line between center of rectA and center of rectB
                double slopePrime = (p2y - p1y) / (p2x - p1x);
                int cardinalDirectionA;
                int cardinalDirectionB;
                double tempPointAx;
                double tempPointAy;
                double tempPointBx;
                double tempPointBy;

                //determine whether clipping point is the corner of nodeA
                if ((-slopeA) == slopePrime)
                {
                    if (p1x > p2x)
                    {
                        result[0] = bottomLeftAx;
                        result[1] = bottomLeftAy;
                        clipPointAFound = true;
                    }
                    else
                    {
                        result[0] = topRightAx;
                        result[1] = topLeftAy;
                        clipPointAFound = true;
                    }
                }
                else if (slopeA == slopePrime)
                {
                    if (p1x > p2x)
                    {
                        result[0] = topLeftAx;
                        result[1] = topLeftAy;
                        clipPointAFound = true;
                    }
                    else
                    {
                        result[0] = bottomRightAx;
                        result[1] = bottomLeftAy;
                        clipPointAFound = true;
                    }
                }

                //determine whether clipping point is the corner of nodeB
                if ((-slopeB) == slopePrime)
                {
                    if (p2x > p1x)
                    {
                        result[2] = bottomLeftBx;
                        result[3] = bottomLeftBy;
                        clipPointBFound = true;
                    }
                    else
                    {
                        result[2] = topRightBx;
                        result[3] = topLeftBy;
                        clipPointBFound = true;
                    }
                }
                else if (slopeB == slopePrime)
                {
                    if (p2x > p1x)
                    {
                        result[2] = topLeftBx;
                        result[3] = topLeftBy;
                        clipPointBFound = true;
                    }
                    else
                    {
                        result[2] = bottomRightBx;
                        result[3] = bottomLeftBy;
                        clipPointBFound = true;
                    }
                }

                //if both clipping points are corners
                if (clipPointAFound && clipPointBFound)
                {
                    return new Tuple<bool, double[]>(false, result);
                }

                //determine Cardinal Direction of rectangles
                if (p1x > p2x)
                {
                    if (p1y > p2y)
                    {
                        cardinalDirectionA = GetCardinalDirection(slopeA, slopePrime, 4);
                        cardinalDirectionB = GetCardinalDirection(slopeB, slopePrime, 2);
                    }
                    else
                    {
                        cardinalDirectionA = GetCardinalDirection(-slopeA, slopePrime, 3);
                        cardinalDirectionB = GetCardinalDirection(-slopeB, slopePrime, 1);
                    }
                }
                else
                {
                    if (p1y > p2y)
                    {
                        cardinalDirectionA = GetCardinalDirection(-slopeA, slopePrime, 1);
                        cardinalDirectionB = GetCardinalDirection(-slopeB, slopePrime, 3);
                    }
                    else
                    {
                        cardinalDirectionA = GetCardinalDirection(slopeA, slopePrime, 2);
                        cardinalDirectionB = GetCardinalDirection(slopeB, slopePrime, 4);
                    }
                }
                //calculate clipping Point if it is not found before
                if (!clipPointAFound)
                {
                    switch (cardinalDirectionA)
                    {
                        case 1:
                            tempPointAy = topLeftAy;
                            tempPointAx = p1x + (-halfHeightA) / slopePrime;
                            result[0] = tempPointAx;
                            result[1] = tempPointAy;
                            break;
                        case 2:
                            tempPointAx = bottomRightAx;
                            tempPointAy = p1y + halfWidthA * slopePrime;
                            result[0] = tempPointAx;
                            result[1] = tempPointAy;
                            break;
                        case 3:
                            tempPointAy = bottomLeftAy;
                            tempPointAx = p1x + halfHeightA / slopePrime;
                            result[0] = tempPointAx;
                            result[1] = tempPointAy;
                            break;
                        case 4:
                            tempPointAx = bottomLeftAx;
                            tempPointAy = p1y + (-halfWidthA) * slopePrime;
                            result[0] = tempPointAx;
                            result[1] = tempPointAy;
                            break;
                    }
                }
                if (!clipPointBFound)
                {
                    switch (cardinalDirectionB)
                    {
                        case 1:
                            tempPointBy = topLeftBy;
                            tempPointBx = p2x + (-halfHeightB) / slopePrime;
                            result[2] = tempPointBx;
                            result[3] = tempPointBy;
                            break;
                        case 2:
                            tempPointBx = bottomRightBx;
                            tempPointBy = p2y + halfWidthB * slopePrime;
                            result[2] = tempPointBx;
                            result[3] = tempPointBy;
                            break;
                        case 3:
                            tempPointBy = bottomLeftBy;
                            tempPointBx = p2x + halfHeightB / slopePrime;
                            result[2] = tempPointBx;
                            result[3] = tempPointBy;
                            break;
                        case 4:
                            tempPointBx = bottomLeftBx;
                            tempPointBy = p2y + (-halfWidthB) * slopePrime;
                            result[2] = tempPointBx;
                            result[3] = tempPointBy;
                            break;
                    }
                }

            }

            return new Tuple<bool, double[]>(false, result);
        }

        private static int GetCardinalDirection(double slope, double slopePrime, int line)
        {
            if (slope > slopePrime)
            {
                return line;
            }
            else
            {
                return 1 + (line % 4);
            }
        }

        /// <summary>
        /// Calculates in which direction two rectangles overlap
        /// </summary>
        /// <param name="rectA">the first rectangle</param>
        /// <param name="rectB">the second rectangle</param>
        /// <param name="directions">the directions, -1 = left/top, 1 = right/bottom</param>
        private static void DecideDirectionsForOverlappingNodes(Rect rectA, Rect rectB, double[] directions)
        {
            if (rectA.center.x < rectB.center.x)
            {
                directions[0] = -1;
            }
            else
            {
                directions[0] = 1;
            }

            if (rectA.center.y < rectB.center.y)
            {
                directions[1] = -1;
            }
            else
            {
                directions[1] = 1;
            }
        }

        /// <summary>
        /// calculates the separation amount between two given rectangles
        /// </summary>
        /// <param name="_rectA">the first rectangle</param>
        /// <param name="rectB">the second rectangle</param>
        /// <param name="overlapAmount">the amount of overlapping</param>
        /// <param name="separationBuffer">the separation buffer</param>
        public static void CalcSeparationAmount(Rect rectA, Rect rectB, double[] overlapAmount, double separationBuffer)
        {
            //Rect rectA = _rectA;
            if (!rectA.Overlaps(rectB))
            {
                throw new System.Exception("Needs overlap");
            }

            double[] directions = new double[2];

            DecideDirectionsForOverlappingNodes(rectA, rectB, directions);

            overlapAmount[0] = Mathf.Min(rectA.xMax, rectB.xMax) -
                Mathf.Max(rectA.x, rectB.x);
            overlapAmount[1] = Mathf.Min(rectA.yMax, rectB.yMax) -
                Mathf.Max(rectA.y, rectB.y);

            // update the overlapping amounts for the following cases:

            if ((rectA.x <= rectB.x) && (rectA.xMax >= rectB.xMax))
            /* Case x.1:
             *
             * rectA
		     * 	|                       |
		     * 	|        _________      |
		     * 	|        |       |      |
		     * 	|________|_______|______|
		     * 			 |       |
		     *           |       |
		     *        rectB
             */
            {
                overlapAmount[0] += Mathf.Min((rectB.x - rectA.x),
                    (rectA.xMax - rectB.xMax));
            }
            else if ((rectB.x <= rectA.x) && (rectB.xMax >= rectA.xMax))
            /* Case x.2:
             *
             * rectB
		     * 	|                       |
		     * 	|        _________      |
		     * 	|        |       |      |
		     * 	|________|_______|______|
		     * 			 |       |
		     *           |       |
	    	 *        rectA
             */
            {
                overlapAmount[0] += Mathf.Min((rectA.x - rectB.x),
                    (rectB.xMax - rectA.xMax));
            }

            if ((rectA.y <= rectB.y) && (rectA.yMax >= rectB.yMax))
            /* Case y.1:
             *          ________ rectA
		     *         |
		     *         |
		     *   ______|____  rectB
	    	 *         |    |
	    	 *         |    |
	    	 *   ______|____|
	    	 *         |
	    	 *         |
	    	 *         |________
	    	 *
             */
            {
                overlapAmount[1] += Mathf.Min((rectB.y - rectA.y),
                    (rectA.yMax - rectB.yMax));
            }
            else if ((rectB.y <= rectA.y) && (rectB.yMax >= rectA.yMax))
            /* Case y.2:
             *          ________ rectB
		     *         |
	    	 *         |
	    	 *   ______|____  rectA
	    	 *         |    |
	    	 *         |    |
	    	 *   ______|____|
	    	 *         |
	    	 *         |
	    	 *         |________
    		 *
             */
            {
                overlapAmount[1] += Mathf.Min((rectA.y - rectB.y),
                    (rectB.yMax - rectA.yMax));
            }

            // find slope of the line passes two centers
            double slope =
                Mathf.Abs((rectB.center.y - rectA.center.y) /
                    (rectB.center.x - rectA.center.x));

            // if centers are overlapped
            if ((rectB.center.y == rectA.center.y) &&
                (rectB.center.x == rectA.center.x))
            {
                // assume the slope is 1 (45 degree)
                slope = 1.0;
            }

            // change y
            double moveByY = slope * overlapAmount[0];
            // change x
            double moveByX = overlapAmount[1] / slope;

            // now we have two pairs:
            // 1) overlapAmount[0], moveByY
            // 2) moveByX, overlapAmount[1]

            // use pair no:1
            if (overlapAmount[0] < moveByX)
            {
                moveByX = overlapAmount[0];
            }
            // use pair no:2
            else
            {
                moveByY = overlapAmount[1];
            }

            // return half the amount so that if each rectangle is moved by these
            // amounts in opposite directions, overlap will be resolved

            overlapAmount[0] = -1 * directions[0] * ((moveByX / 2) + separationBuffer);
            overlapAmount[1] = -1 * directions[1] * ((moveByY / 2) + separationBuffer);
        }
    }
}

