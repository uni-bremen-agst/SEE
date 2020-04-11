using UnityEngine;

/// <summary>
/// Displays the frames per second (FPS) on the upper left corner.
/// 
/// The code was taken from: https://wiki.unity3d.com/index.php/FramesPerSecond
/// 
/// Author: Dave Hampson
/// </summary>
public class FPSDisplay : MonoBehaviour
{
	/// <summary>
	/// Stores the time delta between two frames.
	/// </summary>
	float deltaTime = 0.0f;

	/// <summary>
	/// Updates <see cref="deltaTime"/>.
	/// </summary>
	void Update()
	{
		deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
	}

	/// <summary>
	/// Displays the FPS.
	/// </summary>
	void OnGUI()
	{
		int w = Screen.width, h = Screen.height;
		GUIStyle style = new GUIStyle();
		Rect rect = new Rect(0, 0, w, h * 2 / 100);
		style.alignment = TextAnchor.UpperLeft;
		style.fontSize = h * 2 / 100;
		style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);
		float msec = deltaTime * 1000.0f;
		float fps = 1.0f / deltaTime;
		string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
		GUI.Label(rect, text, style);
	}
}
