using System.Collections;
using UnityEngine;

/// <summary>
/// Shows the number of frames per seconds in the scene.
/// </summary>
public class FramesPerSeconds : MonoBehaviour
{
    Rect fpsRect;
    GUIStyle style;

    // Start is called before the first frame update
    void Start()
    {
        fpsRect = new Rect(100, 100, 400, 100);
        style = new GUIStyle();
        style.fontSize = 30;

        StartCoroutine(RecalculateFPS());
    }

    // The number of frames per second
    float fps;

    /// <summary>
    /// Recalulates fps every second.
    /// </summary>
    /// <returns></returns>
    private IEnumerator RecalculateFPS()
    {
        while (true)
        {
            fps = 1 / Time.deltaTime;
            yield return new WaitForSeconds(1);
        }
    }

    /// <summary>
    /// Shows the number of frames per seconds on the screen.
    /// </summary>
    private void OnGUI()
    {
        GUI.Label(fpsRect, "FPS: " + fps, style);
    }
}
