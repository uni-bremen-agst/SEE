using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelpSystemEntry
{
    private string title;

    private string videoPath;

    private List<string> keyPoints;

    private string audioText;

    public HelpSystemEntry(string title, string videoPath, List<string> keyPoints, string audioText)
    {
        this.title = title;
        this.videoPath = videoPath;
        this.keyPoints = keyPoints;
        this.audioText = audioText;
    }
}
