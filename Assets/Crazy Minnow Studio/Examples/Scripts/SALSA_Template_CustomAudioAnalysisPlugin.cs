using CrazyMinnow.SALSA;
using UnityEngine;

public class SALSA_Template_CustomAudioAnalysisPlugin : MonoBehaviour {

	void Start ()
	{
		GetComponent<Salsa>().audioAnalyzer = CalcSimpleMaxPeakValue;
	}

	/// <summary>
	/// AudioAnalyzer: Simple
	/// Simple peak value analysis: finds and returns the maximum positive peak value.
	/// </summary>
	private float CalcSimpleMaxPeakValue(int channels, float[] audioData)
	{
		var maxVal = 0f; // collected peak value per data slice.

		// loop through the first channel of the data slice.
		for (int i = 0; i < audioData.Length; i += channels)
			if (audioData[i] > maxVal)
				maxVal = audioData[i]; // update peak value

		return maxVal;
	}
}
