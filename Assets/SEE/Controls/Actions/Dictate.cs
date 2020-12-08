using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;
using UnityEngine.UI;
using SEE.GO;
using SEE.Game;
using TMPro;

/// <summary>
/// Implements interactions with the dictationRecognizer.
/// </summary>
public class Dictate : MonoBehaviour
{
    private DictationRecognizer m_DictationRecognizer;

    private string record = "";

    /// <summary>
    /// True if currently recording.
    /// </summary>
    private bool recording = false;

    void Start()
    {
        if (!SystemInfo.operatingSystem.StartsWith("Windows 10"))
        {
            Debug.LogWarning("Dictate funktion is only available under windows 10");
        }
        else if (transform.root.gameObject.GetComponent<SEECity>().dictate)
        {
            m_DictationRecognizer = new DictationRecognizer();

            m_DictationRecognizer.DictationResult += (text, confidence) =>
            {
                Debug.LogFormat("Dictation result: {0}", text);
                record += text;
            };

            m_DictationRecognizer.DictationComplete += (completionCause) =>
            {
                if (completionCause != DictationCompletionCause.Complete)
                    Debug.LogErrorFormat("Dictation completed unsuccessfully: {0}.", completionCause);
            };

            m_DictationRecognizer.DictationError += (error, hresult) =>
            {
                Debug.LogErrorFormat("Dictation error: {0}; HResult = {1}.", error, hresult);
            };
            recording = false;
        }
        else
        {
            AnnotationEditor annotationEditor = this.gameObject.GetComponent<AnnotationEditor>();
            GameObject dictate = annotationEditor.transform.Find("Adding/Dictate").gameObject;
            dictate.SetActive(false);
        }
    }

    void Update()
    {
        if (this.transform.Find("Adding").gameObject.activeSelf == true && recording)
        {
            AnnotationEditor annotationEditor = this.gameObject.GetComponent<AnnotationEditor>();
            GameObject input = annotationEditor.transform.Find("Adding/AddingInput").gameObject;
            input.GetComponent<TMP_InputField>().text = record;
        }
        else if (this.transform.Find("Editing").gameObject.activeSelf == true && recording)
        {
            AnnotationEditor annotationEditor = this.gameObject.GetComponent<AnnotationEditor>();
            GameObject input = annotationEditor.transform.Find("Editing/EditingInput").gameObject;
            input.GetComponent<TMP_InputField>().text = record;
        }
    }

    /// <summary>
    /// Switches between recording and not recording.
    /// </summary>
    public void Click()
    {
        if (recording == false)
        {
            recording = true;
            Go();
        }
        else
        {
            recording = false;
            Stop();
        }
    }

    /// <summary>
    /// Start the dictation process and switches the button text from start to stop.
    /// </summary>
    private void Go()
    {
        m_DictationRecognizer.Start();
        record = "";
        AnnotationEditor annotationEditor = this.gameObject.GetComponent<AnnotationEditor>();
        if (this.transform.Find("Adding").gameObject.activeSelf == true)
        {
            GameObject button = annotationEditor.transform.Find("Adding/Dictate").gameObject;
            button.GetComponentInChildren<TMP_Text>().text = "Stop";
        }
        else if (this.transform.Find("Editing").gameObject.activeSelf == true)
        {
            GameObject button = annotationEditor.transform.Find("Editing/Dictate").gameObject;
            button.GetComponentInChildren<TMP_Text>().text = "Stop";
        }
    }

    /// <summary>
    /// Stops the dictation process and adds the dictated text to the textfield.
    /// </summary>
    private void Stop()
    {
        AnnotationEditor annotationEditor = this.gameObject.GetComponent<AnnotationEditor>();
        if (this.transform.Find("Adding").gameObject.activeSelf == true)
        {
            GameObject input = annotationEditor.transform.Find("Adding/AddingInput").gameObject;
            input.GetComponent<TMP_InputField>().text = record;
            GameObject button = annotationEditor.transform.Find("Adding/Dictate").gameObject;
            button.GetComponentInChildren<TMP_Text>().text = "Dictate";
        }else if (this.transform.Find("Editing").gameObject.activeSelf == true)
        {
            GameObject input = annotationEditor.transform.Find("Editing/EditingInput").gameObject;
            input.GetComponent<TMP_InputField>().text = record;
            GameObject button = annotationEditor.transform.Find("Editing/Dictate").gameObject;
            button.GetComponentInChildren<TMP_Text>().text = "Dictate";
        }
            m_DictationRecognizer.Stop();
    }

    public void Quit()
    {
        if (recording)
        {
            Stop();
        }
    }
    public void Close()
    {
        m_DictationRecognizer.Dispose();
    }
}
