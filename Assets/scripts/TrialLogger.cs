using UnityEngine;
using System;
using System.IO;

public class TrialLogger : MonoBehaviour
{
    public static TrialLogger Instance;

    [Header("Logging")]
    public bool enableLogging = true;

    [Header("Session")]
    public string sessionPrefix = "InternalTest";

    [Header("Participant")]
    public string participantID = "";

    [Header("Trial")]
    public int currentTrialNumber = 0;

    private string sessionID;
    private string sessionFolder;
    private string filePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        CreateSessionFolder();
        CreateLogFile();
    }

    // =========================================================
    // SESSION SETUP
    // =========================================================

    private void CreateSessionFolder()
    {
        string rootFolder =
            Path.Combine(
                Application.persistentDataPath,
                "Haptic3D Test Data"
            );

        if (!Directory.Exists(rootFolder))
        {
            Directory.CreateDirectory(rootFolder);
        }

        string timestamp =
            DateTime.Now.ToString(
                "yyyy-MM-dd_HH-mm-ss"
            );

        sessionID =
            sessionPrefix +
            "_" +
            timestamp;

        sessionFolder =
            Path.Combine(
                rootFolder,
                sessionID
            );

        Directory.CreateDirectory(
            sessionFolder
        );
    }

    // =========================================================
    // LOG FILE SETUP
    // =========================================================

    private void CreateLogFile()
    {
        filePath =
            Path.Combine(
                sessionFolder,
                "TrialLog.csv"
            );

        string header =
            "SessionID," +
            "ParticipantID," +
            "TrialNumber," +
            "Timestamp," +
            "ElapsedTime," +
            "TrialState," +
            "Stimulus," +
            "Event," +
            "HapticIntensity\n";

        File.WriteAllText(
            filePath,
            header
        );

        Debug.Log(
            "Trial log created at: " +
            filePath
        );
    }

    // =========================================================
    // PARTICIPANT
    // =========================================================

    public void SetParticipantID(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            participantID = "";
            return;
        }

        participantID = id.Trim();

        Debug.Log(
            "Participant ID set to: " +
            participantID
        );
    }

    public string GetParticipantID()
    {
        return participantID;
    }

    // =========================================================
    // TRIAL CONTROL
    // =========================================================

    public void StartNextTrial(
        string trialState,
        string stimulus
    )
    {
        currentTrialNumber++;

        LogEvent(
            trialState,
            stimulus,
            "Trial Started"
        );

        Debug.Log(
            "TRIAL " +
            currentTrialNumber +
            " STARTED" +
            " | Participant: " +
            participantID +
            " | State: " +
            trialState +
            " | Stimulus: " +
            stimulus
        );
    }

    public void EndCurrentTrial(
        string trialState,
        string stimulus
    )
    {
        if (currentTrialNumber <= 0)
        {
            return;
        }

        LogEvent(
            trialState,
            stimulus,
            "Trial Ended"
        );

        Debug.Log(
            "TRIAL " +
            currentTrialNumber +
            " ENDED"
        );
    }

    // =========================================================
    // EVENT LOGGING
    // =========================================================

    public void LogEvent(
        string trialState,
        string stimulus,
        string eventType,
        int intensity = -1
    )
    {
        if (!enableLogging)
        {
            return;
        }

        string timestamp =
            DateTime.Now.ToString(
                "HH:mm:ss.fff"
            );

        string elapsedTime =
            Time.time.ToString("F3");

        string intensityText =
            intensity >= 0
            ? intensity.ToString()
            : "";

        string line =
            sessionID + "," +
            participantID + "," +
            currentTrialNumber + "," +
            timestamp + "," +
            elapsedTime + "," +
            trialState + "," +
            stimulus + "," +
            eventType + "," +
            intensityText + "\n";

        File.AppendAllText(
            filePath,
            line
        );
    }

    // =========================================================
    // FILE INFORMATION
    // =========================================================

    public string GetLogFilePath()
    {
        return filePath;
    }

    public string GetSessionFolder()
    {
        return sessionFolder;
    }

    public string GetSessionID()
    {
        return sessionID;
    }
}