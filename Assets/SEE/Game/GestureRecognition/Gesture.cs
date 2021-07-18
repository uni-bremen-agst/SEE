using System;

namespace SEE.Game.GestureRecognition
{
    /// <summary>
    /// Implementation of a gesture as a point cloud representation for the $P Point-Cloud Gesture Recognizer.
    /// Automatically applies pre-processed normalization on creation.
    /// </summary>
    public class Gesture
    {
        
        /// <summary>
        /// The size of point samples the input gesture is resampled to.
        /// </summary>
        private const int NUMBER_OF_POINTS = 32;
        
        /// <summary>
        /// The preprocessed gesture points.
        /// </summary>
        public GesturePoint[] Points;
        
        /// <summary>
        /// The name of this gesture
        /// </summary>
        public readonly string Name;
        
        /// <summary>
        /// A point-cloud representation of a give path of gesture points.
        /// </summary>
        /// <param name="points">The gesture points</param>
        /// <param name="name">The gesture name</param>
        public Gesture(GesturePoint[] points, string name = "")
        {
            Name = name;
            Normalize(points, NUMBER_OF_POINTS);
        }
        
        
        /// <summary>
        /// Normalizes the give gesture points. See inline comment for more details.
        /// </summary>
        /// <param name="points">The gesture points.</param>
        /// <param name="n">The target sample size.</param>
        private void Normalize(GesturePoint[] points, int n)
        {
            // Step 1: [RESAMPLE] Resamples the list of points to match the target sample size.
            // The Resample process applies interpolation to generate n evenly spaced points.
            this.Points = Resample(points, n);
            // Step 2: [SCALE] Rescales the given gesture to fit a bounding box of size [0...1]x[0...1].
            // Preserves the shape of the original gesture.
            this.Points = Scale(this.Points);
            // Step 3: [TRANSLATE-TO-ORIGIN] Translates the gesture points to by its centroid.
            this.Points = TranslateToOrigin(this.Points, n);
        }
        
        
        /// <summary>
        /// Translates the given gesture points by its centroid origin.
        /// </summary>
        /// <param name="points">The gesture points.</param>
        /// <param name="n">The sample size.</param>
        /// <returns>The translated gesture points</returns>
        private GesturePoint[] TranslateToOrigin(GesturePoint[] points, int n)
        {
            GesturePoint center = Centroid(points); //Calculates the centroid origin of this gesture
            GesturePoint[] newPoints = new GesturePoint[points.Length];
            foreach (GesturePoint point in points)
            {
                center.X += point.X;
                center.Y += point.Y;
            }
            center.X = center.X / n;
            center.Y = center.Y / n;
            for (int i = 0; i < points.Length; i++)
            {
                newPoints[i] = new GesturePoint(points[i].X - center.X, points[i].Y - center.Y, points[i].StrokeID);
            }
            return newPoints;
        }

        
        /// <summary>
        /// Calculates the center origin point for a given list of gesture points.
        /// </summary>
        /// <param name="points">The gesture points.</param>
        /// <returns>The center point</returns>
        public static GesturePoint Centroid(GesturePoint[] points)
        {
            float cx = 0, cy = 0;
            for (int i = 0; i < points.Length; i++)
            {
                cx += points[i].X;
                cy += points[i].Y;
            }
            return new GesturePoint(cx / points.Length, cy / points.Length, points[0].StrokeID); 
        }
        
        
        /// <summary>
        /// Scales the given list of gesture points to fit a bounding box of size [0...1]x[0...1].
        /// The scaling processing preserves the original shape of the gesture.
        /// </summary>
        /// <param name="points">The gesture points.</param>
        /// <returns>The rescaled gesture points.</returns>
        private GesturePoint[] Scale(GesturePoint[] points)
        {
            float xMin = float.PositiveInfinity,
                yMin = float.PositiveInfinity,
                xMax = float.NegativeInfinity,
                yMax = float.NegativeInfinity;
            GesturePoint[] newPoints = new GesturePoint[points.Length];
            foreach (GesturePoint point in points)
            {
                xMin = Math.Min(xMin, point.X);
                yMin = Math.Min(yMin, point.Y);
                xMax = Math.Max(xMax, point.X);
                yMax = Math.Max(yMax, point.Y);
            }

            float scaleFactor = Math.Max(xMax - xMin, yMax - yMin);
            for (int i = 0; i < points.Length; i++)
            {
                newPoints[i] = new GesturePoint((points[i].X - xMin) / scaleFactor, (points[i].Y - yMin) / scaleFactor,
                    points[i].StrokeID);
            }

            return newPoints;
        }
        
        
        /// <summary>
        /// Resamples a given list of gesture point to the given target sample size of <paramref name="n"/>
        /// Applies interpolation to space the sample points evenly.
        /// </summary>
        /// <param name="points">The gesture points.</param>
        /// <param name="n">The target sample size</param>
        /// <returns>The list of evenly spaced gesture points of size <paramref name="n"/></returns>
        private GesturePoint[] Resample(GesturePoint[] points, int n)
        {
            GesturePoint[] newPoints = new GesturePoint[n];
            newPoints[0] = new GesturePoint(points[0].X, points[0].Y, points[0].StrokeID);
            int numPoints = 1;

            float I = PathLength(points) / (n - 1); // computes interval length
            float D = 0;
            for (int i = 1; i < points.Length; i++)
            {
                if (points[i].StrokeID == points[i - 1].StrokeID)
                {
                    float d = Gesture.EuclideanDistance(points[i - 1], points[i]);
                    if (D + d >= I)
                    {
                        GesturePoint firstStrokePoint = points[i - 1];
                        while (D + d >= I)
                        {
                            // add interpolated point
                            float t = Math.Min(Math.Max((I - D) / d, 0.0f), 1.0f);
                            if (float.IsNaN(t)) t = 0.5f;
                            newPoints[numPoints++] = new GesturePoint(
                                (1.0f - t) * firstStrokePoint.X + t * points[i].X,
                                (1.0f - t) * firstStrokePoint.Y + t * points[i].Y,
                                points[i].StrokeID
                            );

                            // update partial length
                            d = D + d - I;
                            D = 0;
                            firstStrokePoint = newPoints[numPoints - 1];
                        }
                        D = d;
                    }
                    else D += d;
                }
            }
            if (numPoints == n - 1) // sometimes we fall a rounding-error short of adding the last point, so add it if so
                newPoints[numPoints++] = new GesturePoint(points[points.Length - 1].X, points[points.Length - 1].Y, points[points.Length - 1].StrokeID);
            return newPoints;
        }

        /// <summary>
        /// Calculates the length of a path described by the given ordered list of gesture points.
        /// </summary>
        /// <param name="points">The gesture points</param>
        /// <returns>The lenght of the path</returns>
        private float PathLength(GesturePoint[] points)
        {
            float distance = 0f;
            for (int i = 1; i < points.Length; i++)
            {
                if (points[i].StrokeID == points[i - 1].StrokeID)
                {
                    distance += EuclideanDistance(points[i - 1], points[i]);
                }
                
            }
            return distance;
        }


        /// <summary>
        /// Computes the squared euclidean distance in a two dimensional space between two points.
        /// https://en.wikipedia.org/wiki/Euclidean_distance
        /// </summary>
        /// <param name="a">Source point</param>
        /// <param name="b">Target point</param>
        /// <returns>The squared euclidean distance</returns>
        public static float SqrEuclideanDistance(GesturePoint a, GesturePoint b)
        {
            return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
        }

        
        /// <summary>
        /// Computes the euclidean distance in a two dimensional space between two points.
        /// </summary>
        /// <param name="a">Source point</param>
        /// <param name="b">Target point</param>
        /// <returns>The euclidean distance</returns>
        public static float EuclideanDistance(GesturePoint a, GesturePoint b)
        {
            return (float) Math.Sqrt(SqrEuclideanDistance(a, b));
        }
    }
}