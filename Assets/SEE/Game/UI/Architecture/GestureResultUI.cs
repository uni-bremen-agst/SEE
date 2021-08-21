using TMPro;
using UnityEngine;

namespace SEE.Game.UI.Architecture
{
    /// <summary>
    /// Script for showing results of the <see cref="GestureRecognition.GestureRecorder"/> on screen.
    /// </summary>
    public class GestureResultUI : MonoBehaviour
    {

        
        /// <summary>
        /// The ui element that shows the gesture result.
        /// </summary>
        public TextMeshProUGUI outputText;
        
        /// <summary>
        /// The ui element that shows the confidence score of the $P-Recognizer.
        /// </summary>
        public TextMeshProUGUI confidenceScore;
        
        /// <summary>
        /// The ui element that shows the configured confidence threshold. See
        /// <see cref="GestureRecognition.DollarPGestureRecognizer"/> for more info.
        /// </summary>
        public TextMeshProUGUI threshold;
        
        /// <summary>
        /// The ui element that shows the amount of existing data sets for this gesture.
        /// </summary>
        public TextMeshProUGUI dataSets;

        
        /// <summary>
        /// The UI element that shows the amount of points within the gesture path.
        /// </summary>
        public TextMeshProUGUI rawPoints;

        
        /// <summary>
        /// The ui text that shows info when the matched result is not exact.
        /// </summary>
        public TextMeshProUGUI info;



        /// <summary>
        /// Shows the result of the sampling/recognizing within the ui panel.
        /// </summary>
        /// <param name="result">The result of the $P-Recognizer</param>
        /// <param name="rawPointsLength">The amount of raw gesture path points.</param>
        /// <param name="sampling">Whether the current mode is sampling.</param>
        /// <param name="gestureName">If the current mode is sampling, this is set to the target gesturename.</param>
        /// <param name="noMatch">Whether the $P-Recognizer did not found an exact match.</param>
        public void ShowResult(GestureRecognition.DollarPGestureRecognizer.RecognizerResult result, int rawPointsLength, bool sampling, string gestureName, bool noMatch = false)
        {
            if(sampling)
            {
                confidenceScore.text = "Confidence:";
                threshold.text = "Threshold:";
                dataSets.text = "DataSets:";
                outputText.text = "Gesture: " + gestureName;
                rawPoints.text = "Points: " + rawPointsLength;
            }
            else
            {
                if (noMatch)
                {
                    info.text = "Info: No exact match found";
                }
                else
                {
                    info.text = "Info: ";
                }
                confidenceScore.text = "Confidence: " + result.Score;
                threshold.text = "Threshold: " + result.Threshold;
                dataSets.text = "DataSets: " + result.DataSets;
                outputText.text = "Gesture: " + result.Match.Name;
                rawPoints.text = "Points: " + rawPointsLength;
            }
        }
    }
}