using SEE.DataModel;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// TODO flo: doc und vll die eigentliche Factory nutzen
/// </summary>
internal class ExtendedTextFactory
{
    public static void UpdateText(GameObject textObject, string text, Vector3 position, float width, bool lift = true)
    {
        textObject.name = "Text " + text;
        textObject.transform.position = position;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        // We set width and height of the rectangle and leave the actual size to Unity,
        // which will select a font that matches our size constraints.
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, width);

        TextFacingCamera textFacing = textObject.GetComponent<TextFacingCamera>();
        // Rendering distance is set relative to the text's width
        textFacing.minimalDistance = width;
        textFacing.maximalDistance = 10.0f * width;

        if (lift)
        {
            TextMeshPro tm = textObject.GetComponent<TextMeshPro>();
            // may need to be called before retrieving the bounds to make sure they are up to date
            tm.ForceMeshUpdate();
            // unlike other types of game objects, the renderer does not allow us to retrieve the
            // extents of the text; we need to use tm.textBounds instead
            Bounds bounds = tm.textBounds;
            float yPosition = bounds.extents.y;
            textObject.transform.position += yPosition * Vector3.up;
        }
    }

    /// <summary>
    /// Returns a game object showing the tiven text at given position. The
    /// text rotates towards the main camera.
    /// </summary>
    /// <param name="text">the text to be drawn</param>
    /// <param name="position">the center position at which to draw the text</param>
    /// <param name="width">the width of the rectangle enclosing the text (essentially, 
    /// the text width); the font size will be chose appropriately</param>
    /// <param name="lift">if true, the text will be lifted up by its extent; that is, is y position is actually the bottom line (position.y + extents.y)</param>
    /// <returns>the game object representing the text</returns>
    public static GameObject GetText(string text, Vector3 position, float width, bool lift = true)
    {
        GameObject result = new GameObject("Text " + text)
        {
            tag = Tags.Text
        };
        result.transform.position = position;

        TextMeshPro tm = result.AddComponent<TextMeshPro>();
        tm.text = text;
        tm.color = Color.black;
        tm.alignment = TextAlignmentOptions.Center;

        RectTransform rect = tm.GetComponent<RectTransform>();
        // We set width and height of the rectangle and leave the actual size to Unity,
        // which will select a font that matches our size constraints.
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, width);

        tm.enableAutoSizing = true;
        tm.fontSizeMin = 3;
        tm.fontSizeMax = 400;

        TextFacingCamera textFacing = result.AddComponent<TextFacingCamera>();
        // Rendering distance is set relative to the text's width
        textFacing.minimalDistance = width;
        textFacing.maximalDistance = 10.0f * width;

        // No shading as this might be expensive and even distracts.
        Renderer renderer = result.GetComponent<Renderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        if (lift)
        {
            // may need to be called before retrieving the bounds to make sure they are up to date
            tm.ForceMeshUpdate();
            // unlike other types of game objects, the renderer does not allow us to retrieve the
            // extents of the text; we need to use tm.textBounds instead
            Bounds bounds = tm.textBounds;
            float yPosition = bounds.extents.y;
            result.transform.position += yPosition * Vector3.up;
        }
        return result;
    }

    /// <summary>
    /// Returns a game object showing the tiven text at given position. The
    /// text rotates towards the main camera.
    /// </summary>
    /// <param name="text">the text to be drawn</param>
    /// <param name="position">the center position at which to draw the text</param>
    /// <param name="width">the width of the rectangle enclosing the text (essentially, 
    /// the text width); the font size will be chose appropriately</param>
    /// <param name="lift">if true, the text will be lifted up by its extent; that is, is y position is actually the bottom line (position.y + extents.y)</param>
    /// <returns>the game object representing the text</returns>
    public static GameObject GetEmpty(string text)
    {
        GameObject result = new GameObject("Text " + text)
        {
            tag = Tags.Text
        };

        TextMeshPro tm = result.AddComponent<TextMeshPro>();
        tm.text = text;
        tm.color = Color.black;
        tm.alignment = TextAlignmentOptions.Center;

        RectTransform rect = tm.GetComponent<RectTransform>();
        // We set width and height of the rectangle and leave the actual size to Unity,
        // which will select a font that matches our size constraints.
        //rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        //rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, width);

        tm.enableAutoSizing = true;
        tm.fontSizeMin = 3;
        tm.fontSizeMax = 400;

        TextFacingCamera textFacing = result.AddComponent<TextFacingCamera>();
        // Rendering distance is set relative to the text's width
        //textFacing.minimalDistance = width;
        //textFacing.maximalDistance = 10.0f * width;

        // No shading as this might be expensive and even distracts.
        Renderer renderer = result.GetComponent<Renderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return result;
    }
}
