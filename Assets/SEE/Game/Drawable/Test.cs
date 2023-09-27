using SEE.GO.Menu;
using SimpleFileBrowser;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    public class Test : MonoBehaviour
    {

        // Use this for initialization
        void Start()
        {
            Debug.Log("Start");
            StartCoroutine(ShowLoadDialogCoroutine());
        }

        IEnumerator ShowLoadDialogCoroutine()
        {
            Debug.Log("Is started");
            // Show a load file dialog and wait for a response from user
            // Load file/folder: both, Allow multiple selection: true
            // Initial path: default (Documents), Initial filename: empty
            // Title: "Load File", Submit button text: "Load"
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, false, null, null, "Load Files and Folders", "Load");

            // Dialog is closed
            // Print whether the user has selected some files/folders or cancelled the operation (FileBrowser.Success)
            Debug.Log(FileBrowser.Success);

            if (FileBrowser.Success)
            {
                Debug.Log("Success");
            }
        }
    }
}