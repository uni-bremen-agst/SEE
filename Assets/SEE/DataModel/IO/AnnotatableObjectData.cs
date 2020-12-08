using UnityEngine;
using SEE.Controls;
using SEE.GO;
using System;

/// <summary>
/// Converts all relevant attributs of an annoatableObject so the are serialisable and deserialisible.
/// </summary>
[System.Serializable]
public class AnnotatableObjectData
{
    public float[] position;
    public float[] scale;
    public float id;
    public string[] annotations;
    public string textOnPaper;

    public AnnotatableObjectData(AnnotatableObject annotatableObject)
    {
        id = Convert.ToSingle(annotatableObject.id);

        position = new float[3];
        position[0] = annotatableObject.transform.position.x;
        position[1] = annotatableObject.transform.position.y;
        position[2] = annotatableObject.transform.position.z;

        scale = new float[3];
        scale[0] = annotatableObject.transform.localScale.x;
        scale[1] = annotatableObject.transform.localScale.y;
        scale[2] = annotatableObject.transform.localScale.z;

        if (annotatableObject.textOnPaper != null)
        {
            textOnPaper = annotatableObject.textOnPaper.GetComponent<TextGUIAndPaperResizer>().Text;
        }

        if (annotatableObject.isAnnotated)
        {
            annotations = new string[annotatableObject.annotations.Count];
            int count = 0;
            foreach (GameObject annotation in annotatableObject.annotations)
            {
                annotations[count] = annotation.GetComponent<TextGUIAndPaperResizer>().Text.Replace(System.Environment.NewLine, " ");
                count++;
            }
        }
    }
}
