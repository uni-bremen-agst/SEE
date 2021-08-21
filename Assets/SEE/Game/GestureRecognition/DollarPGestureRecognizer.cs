using System;
using System.Linq;
using SEE.Utils;
using Sirenix.Utilities;
using UnityEngine;

namespace SEE.Game.GestureRecognition
{
    public class DollarPGestureRecognizer
    {/// <summary>
        /// The point-cloud sample size.
        /// </summary>
        private const int NUMBER_OF_POINTS = 32;
        
        /// <summary>
        /// How confident the Recognizer needs to be to decide on an matching gesture.
        /// This primarily avoids detecting any other gesture if no gesture set was yet added.
        /// </summary>
        private const float CONFIDENCE_THRESHOLD = 0.85f;

        
        /// <summary>
        /// The result of the recognizer.
        /// </summary>
        public struct RecognizerResult
        {
            public float Score;
            public Gesture Match;
            public int DataSets;
            public float Threshold;
        }

        /// <summary>
        /// Tries to recognize a gesture based on the given gesture game object.
        /// Precondition: The gesture GO has a <see cref="TrailRenderer"/> component that stores the gesture path points.
        /// </summary>
        /// <param name="gestureGO">The gesture gameobject</param>
        /// <param name="result">The result</param>
        /// <returns>Whether the recognition was successful.</returns>
        public static bool TryRecognizeGesture(GameObject gestureGO, out RecognizerResult recognizerResult, out Vector3[] rawPoints)
        {
            rawPoints = ExtractRawPoints(gestureGO);
            GesturePoint[] points = ExtractGesturePoints(gestureGO);
            if (points.IsNullOrEmpty())
            {
                Debug.LogWarning($"The provided gesture has no points!");
                recognizerResult = default(RecognizerResult);
                return false;
            }
            Gesture candidate = new Gesture(points);
            Gesture[] dataSet = GestureIO.DataSet;
            recognizerResult = Recognize(candidate, dataSet);
            if (recognizerResult.Score < CONFIDENCE_THRESHOLD)
            {
                Debug.LogWarning($"There is no exact matching gesture for this input. " +
                                 $"The best match {recognizerResult.Match.Name} only matches with score {recognizerResult.Score}. " +
                                 $"Expected confidence of > {CONFIDENCE_THRESHOLD}");
                return false;
            }
            Debug.Log($"Recognized gesture {recognizerResult.Match.Name} with an score of {recognizerResult.Score}");
            return true;
        }

        
        /// <summary>
        /// Extracts the points from a given gesture GO.
        /// Precondition: The gestureGO has a <see cref="TrailRenderer"/> component attached.
        /// </summary>
        /// <param name="gestureGO">The gesture game object.</param>
        /// <returns>The list of gesture points</returns>
        public static GesturePoint[] ExtractGesturePoints(GameObject gestureGO)
        {
            TrailRenderer renderer = gestureGO.GetComponent<TrailRenderer>();
            Vector3[] points = new Vector3[renderer.positionCount];
            renderer.GetPositions(points);
            return points.Select(VectorToGesturePoint).ToArray();
        }

        
        /// <summary>
        /// Extracts the unprocessed raw points in world space from the gesture game object.
        /// </summary>
        /// <param name="gestureGO">The gesture game object</param>
        /// <returns>The raw list of world space points.</returns>
        public static Vector3[] ExtractRawPoints(GameObject gestureGO)
        {
            TrailRenderer renderer = gestureGO.GetComponent<TrailRenderer>();
            Vector3[] points = new Vector3[renderer.positionCount];
            renderer.GetPositions(points);
            return points;
        }

        
        /// <summary>
        /// Maps a world space point to a gesture point with viewport space coordinates.
        /// </summary>
        /// <param name="vector">The world space point</param>
        /// <returns>The gesture point in viewport space.</returns>
        private static GesturePoint VectorToGesturePoint(Vector3 vector)
        {
            Camera main = MainCamera.Camera;
            Vector2 viewportPoint = main.WorldToViewportPoint(vector);
            return new GesturePoint(viewportPoint.x, viewportPoint.y, 0);
        }
        



        /// <summary>
        /// The main entry point for the $P Point-Cloud Gesture Recognition.
        /// Matches a gesture candidate point-cloud against a set of template gesture point-clouds.
        /// Returns a score in [0..1] with 1 denoting a perfect match.
        /// </summary>
        /// <param name="candidate">The candidate gesture point-cloud</param>
        /// <param name="templates">The set of template gesture point-clouds</param>
        /// <returns>Contains the best match Gesture and a
        /// score denoting the match quality</returns>
        public static RecognizerResult Recognize(Gesture candidate, Gesture[] templates)
        {
            float score = float.MaxValue;
            Gesture result = default(Gesture);
            foreach (var template in templates)
            {
                float diff = GreedyCloudMatch(candidate.Points, template.Points, NUMBER_OF_POINTS);
                if (score > diff)
                {
                    score = diff;
                    result = template;
                }
            }
            score = Math.Max((2.0f - score) / 2.0f, 0f); //normalize score to [0..1]
            return new RecognizerResult
            {
                Match = result,
                Score = score,
                DataSets = templates.Length,
                Threshold = CONFIDENCE_THRESHOLD
            };
        }

        /// <summary>
        /// Tries to match two point clouds by performing repeated alignments between their points.
        /// Returns the minimum
        ///
        /// Precondition: The supplied gesture cloud points are preprocessed.
        /// </summary>
        /// <param name="candidate">The candidate point cloud</param>
        /// <param name="template">The template point cloud</param>
        /// <param name="n">The number of points</param>
        /// <returns>Returns the minimum alignment cost.</returns>
        private static float GreedyCloudMatch(GesturePoint[] candidate, GesturePoint[] template, int n)
        {
            float epsilon = 0.5f;
            int step = (int) Math.Floor(Math.Pow(candidate.Length, 1.0 - epsilon));
            float minimum = float.MaxValue;
            for (int i = 0; i < candidate.Length; i += step)
            {
                float d1 = CloudDistance(candidate, template, n, i);
                float d2 = CloudDistance(candidate, template, n, i);
                minimum = Math.Min(minimum, d1);
                minimum = Math.Min(minimum, d2);
            }
            return minimum;
        }

        /// <summary>
        /// Computes the minimum-cost alignment between the candidate and template point points
        /// starting with the point at i = <paramref name="start"/>.
        /// Precondition: The supplied gesture cloud points are preprocessed.
        /// </summary>
        /// <param name="candidate">The point cloud of the candidate</param>
        /// <param name="template">The point cloud of the template</param>
        /// <param name="n">The number of points</param>
        /// <param name="start">The start index</param>
        /// <returns>The minimum cost-alignment distance for the clouds</returns>
        private static float CloudDistance(GesturePoint[] candidate, GesturePoint[] template, int n, int start)
        {
            bool[] matched = new bool[n];
            float sum = 0f;
            int i = start; // start matching with points
            do
            {
                int index = -1;
                float minimum = float.PositiveInfinity;
                for (int j = 0; j < n; j++)
                {
                    float dist = Gesture.SqrEuclideanDistance(candidate[i], template[j]);
                    if (dist < minimum)
                    {
                        minimum = dist;
                        index = j;
                    }
                }
                matched[index] = true;
                float weight = 1 -((i -start + n) % n) /n;
                sum += weight * minimum;
                i = (i + 1) % n;
            } while (i != start); //all points are processed;

            return sum;
        }
    }
}