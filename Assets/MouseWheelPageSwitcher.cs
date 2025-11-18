using UnityEngine;
using TMPro;

public class MouseWheelPageSwitcher : MonoBehaviour
{
    public TextMeshProUGUI textMesh;

    void Update()
    {
        if (textMesh == null) return;

        // Linke Maustaste -> nächste Seite
        if (Input.GetMouseButtonDown(0))
        {
            if (textMesh.pageToDisplay < textMesh.textInfo.pageCount)
                textMesh.pageToDisplay++;
            Debug.Log("Next Page: " + textMesh.pageToDisplay);
        }

        // Rechte Maustaste -> vorherige Seite
        if (Input.GetMouseButtonDown(1))
        {
            if (textMesh.pageToDisplay > 1)
                textMesh.pageToDisplay--;
            Debug.Log("Previous Page: " + textMesh.pageToDisplay);
        }
    }

    void NextPage()
    {
        if (textMesh.pageToDisplay < textMesh.textInfo.pageCount)
        {
            textMesh.pageToDisplay++;
        }
    }

    void PreviousPage()
    {
        if (textMesh.pageToDisplay > 1)
        {
            textMesh.pageToDisplay--;
        }
    }
}
