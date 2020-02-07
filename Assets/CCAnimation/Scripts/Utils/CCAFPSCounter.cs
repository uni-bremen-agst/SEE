using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// A CCAFPSCounter allows the measurement of frames per
/// second in a self defined time frame. After each round
/// the measured values are stored in CSV format and can be saved if required  
/// </summary>
public class CCAFPSCounter
{
    /// <summary>
    /// The index of the actual round. If EndRound is called
    /// the index is increased by one.
    /// </summary>
    public long RoundIndex { get; private set; } = 0;

    /// <summary>
    /// The number of FPS counted in the actual round.
    /// </summary>
    public long RoundCountedFPS { get; private set; } = 0;

    /// <summary>
    /// The sum of all FPS counted in the actual round.
    /// </summary>
    public long RoundFPSSum { get; private set; } = 0;

    /// <summary>
    /// The actual FPS calculated by the last OnUpdate call.
    /// </summary>
    public int ActualFPS { get; private set; } = 0;

    /// <summary>
    /// The lowest FPS of the actual round.
    /// </summary>
    public int RoundLowestFPS { get; private set; } = 0;

    /// <summary>
    /// The highest FPS of the actual round.
    /// </summary>
    public int RoundHighestFPS { get; private set; } = 0;

    /// <summary>
    /// Contains the CSV formatted string, which can be safed if needed.
    /// </summary>
    private StringBuilder CsvStringBuilder = new StringBuilder();

    /// <summary>
    /// Returns a csv formatted string containing the information of all counted
    /// rounds with the Columns [Round Index; Lowest FPS; Average FPS; Highest FPS]
    /// </summary>
    public string AsCsvString => CsvStringBuilder.ToString();


    /// <summary>
    /// Creates a new CCAFPS Counter.
    /// </summary>
    public CCAFPSCounter()
    {
        CsvStringBuilder.AppendLine("Graph Nr; Lowest FPS; Average FPS; Highest FPS");
    }

    /// <summary>
    /// Starts a new Round
    /// </summary>
    public void BeginRound()
    {
        RoundIndex++;
        RoundCountedFPS = 1;
        RoundFPSSum = ActualFPS;
        RoundLowestFPS = ActualFPS;
        RoundHighestFPS = ActualFPS;
    }

    /// <summary>
    /// An update function, that needs to be called on every Unity update.
    /// </summary>
    public void OnUpdate()
    {
        ActualFPS = (int)(1f / Time.unscaledDeltaTime);
        RoundCountedFPS++;

        RoundFPSSum += ActualFPS;

        if (RoundLowestFPS > ActualFPS)
        {
            RoundLowestFPS = ActualFPS;
        }
        if (RoundHighestFPS < ActualFPS)
        {
            RoundHighestFPS = ActualFPS;
        }
    }

    /// <summary>
    /// Ends the active Round and saves the calculated data to the CSV formatted string.
    /// </summary>
    public void EndRound()
    {
        var averageFPS = (int)(RoundFPSSum / RoundCountedFPS);
        CsvStringBuilder.AppendLine($"{RoundIndex}; {RoundLowestFPS}; {averageFPS}; {RoundHighestFPS}");
    }

    /// <summary>
    /// Resets this FPSCounter to the initial state without data.
    /// </summary>
    public void Reset()
    {
        RoundIndex = 0;
        CsvStringBuilder.Clear();
        CsvStringBuilder.AppendLine("Graph Nr; Lowest FPS; Average FPS; Highest FPS");
    }

}
