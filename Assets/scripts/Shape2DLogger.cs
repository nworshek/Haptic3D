using UnityEngine;
using System;
using System.IO;
using System.Globalization;

// Independent Shape2D logging. Does not modify or depend on TrialLogger (Haptic3D).
public class Shape2DLogger : MonoBehaviour
{
    public static Shape2DLogger Instance { get; private set; }
    public bool enableLogging = true;
    public string participantID = "";

    private string sessionID;
    private string filePath;
    private float trialStartTime;
    private float trialElapsed;
    private bool timerRunning;
    private int activeTrial;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public void SetParticipantID(string id)
    {
        if (timerRunning)
        {
            Debug.LogWarning("Shape2D: cannot change participant during an active trial.");
            return;
        }
        string cleaned = (id ?? "").Trim();
        if (cleaned.Length == 0 || cleaned.IndexOfAny(new char[] {'/', '\\', ':', '*', '?', '"', '<', '>', '|'}) >= 0)
        {
            Debug.LogWarning("Shape2D: enter a valid coded participant ID.");
            return;
        }
        if (filePath != null && participantID != cleaned)
        {
            Debug.LogWarning("Shape2D: restart the Unity session before changing participants.");
            return;
        }
        participantID = cleaned;
    }

    private bool EnsureFile()
    {
        if (!enableLogging) return false;
        if (!string.IsNullOrEmpty(filePath)) return true;
        if (string.IsNullOrWhiteSpace(participantID))
        {
            Debug.LogError("Shape2D: participant ID must be set before logging.");
            return false;
        }
        try
        {
            string root = Path.Combine(Application.persistentDataPath, "Shape2D Test Data");
            Directory.CreateDirectory(root);
            sessionID = "Shape2D_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string folder = Path.Combine(root, sessionID);
            Directory.CreateDirectory(folder);
            filePath = Path.Combine(folder, "Shape2DResults.csv");
            File.WriteAllText(filePath, "Session ID,Participant ID,Trial Number,Shape,Event,Response,Correct,Elapsed Time (s),Timestamp\n");
            Debug.Log("Shape2D log: " + filePath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Shape2D logging initialization failed: " + e.Message);
            return false;
        }
    }

    private static string Csv(string value)
    {
        value = value ?? "";
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private void Append(int number, string shape, string evt, string response, string correct, float elapsed)
    {
        if (!EnsureFile()) return;
        try
        {
            string row = Csv(sessionID) + "," + Csv(participantID) + "," + number + "," +
                Csv(shape) + "," + Csv(evt) + "," + Csv(response) + "," + Csv(correct) + "," +
                elapsed.ToString("F3", CultureInfo.InvariantCulture) + "," +
                Csv(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")) + "\n";
            File.AppendAllText(filePath, row);
        }
        catch (Exception e)
        {
            Debug.LogError("Shape2D CSV write failed: " + e.Message);
        }
    }

    public void LogShape2DEvent(int number, string shape, string evt)
    {
        Append(number, shape, evt, "", "", 0f);
    }

    public void StartShape2DTrial(int number, string shape)
    {
        activeTrial = number;
        trialStartTime = Time.realtimeSinceStartup;
        trialElapsed = 0f;
        timerRunning = true;
        Append(number, shape, "Started", "", "", 0f);
    }

    public void EndShape2DTrial(int number, string shape)
    {
        if (timerRunning && activeTrial == number)
            trialElapsed = Time.realtimeSinceStartup - trialStartTime;
        timerRunning = false;
        Append(number, shape, "Ended", "", "", trialElapsed);
    }

    public void RecordShape2DResponse(int number, string shape, string response)
    {
        string correct = string.Equals(shape, response, StringComparison.OrdinalIgnoreCase) ? "1" : "0";
        Append(number, shape, "Response", response, correct, trialElapsed);
    }

    public string GetLogFilePath() { return filePath; }

    void OnDisable()
    {
        if (timerRunning)
        {
            timerRunning = false;
            Append(activeTrial, "", "Interrupted", "", "", Time.realtimeSinceStartup - trialStartTime);
        }
    }
}
