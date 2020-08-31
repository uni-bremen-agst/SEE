using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;
using UnityEngine.UI;
using SEE.GO;
using TMPro;

public class Dictate : MonoBehaviour
{
    private DictationRecognizer m_DictationRecognizer;

    private string record = "";

    private bool recording = false;

    void Start()
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

    void Update()
    {
        if (recording)
        {
            AnnotationEditor annotationEditor = this.gameObject.GetComponent<AnnotationEditor>();
            GameObject input = annotationEditor.transform.Find("Adding/AddingInput").gameObject;
            input.GetComponent<TMP_InputField>().text = record;
        }
        else
        {
            record = "";
        }
    }

    public void Click()
    {
        if (recording == false)
        {
            Go();
        }
        else
        {
            Stop();
        }
    }

    private void Go()
    {
        recording = true;
        m_DictationRecognizer.Start();
        AnnotationEditor annotationEditor = this.gameObject.GetComponent<AnnotationEditor>();
        GameObject button = annotationEditor.transform.Find("Adding/Dictate").gameObject;
        button.GetComponentInChildren<TMP_Text>().text = "Stop";
    }

    private void Stop()
    {
        AnnotationEditor annotationEditor = this.gameObject.GetComponent<AnnotationEditor>();
        GameObject input = annotationEditor.transform.Find("Adding/AddingInput").gameObject;
        input.GetComponent<TMP_InputField>().text = record;
        GameObject button = annotationEditor.transform.Find("Adding/Dictate").gameObject;
        button.GetComponentInChildren<TMP_Text>().text = "Dictate";
        m_DictationRecognizer.Stop();
        recording = false;
    }

    public void Close()
    {
        m_DictationRecognizer.Dispose();
    }
}
