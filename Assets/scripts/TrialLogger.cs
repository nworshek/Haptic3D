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

    private float trialStartTime = 0f;
    private bool trialTimerRunning = false;


    // =========================================================
    // AWAKE
    // =========================================================

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
            "Session ID," +
            "Participant ID," +
            "Trial Number," +
            "Time," +
            "Elapsed Time (s)," +
            "Condition," +
            "Stimulus," +
            "Event," +
            "Finger," +
            "Motor," +
            "Motor Intensity (%)\n";

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

    public void SetParticipantID(
        string id
    )
    {
        if (string.IsNullOrEmpty(id))
        {
            participantID = "";
            return;
        }

        participantID =
            id.Trim();

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

        trialStartTime =
            Time.time;

        trialTimerRunning =
            true;

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
            " | Condition: " +
            GetReadableCondition(
                trialState
            ) +
            " | Stimulus: " +
            GetReadableStimulus(
                stimulus
            )
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

        trialTimerRunning =
            false;

        Debug.Log(
            "TRIAL " +
            currentTrialNumber +
            " ENDED"
        );
    }


    // =========================================================
    // STANDARD EVENT LOGGING
    // =========================================================
    //
    // Existing scripts can continue calling:
    //
    // LogEvent(state, stimulus, event);
    //
    // or:
    //
    // LogEvent(state, stimulus, event, intensity);
    //
    // =========================================================

    public void LogEvent(
        string trialState,
        string stimulus,
        string eventType,
        int intensity = -1
    )
    {
        LogEvent(
            trialState,
            stimulus,
            eventType,
            intensity,
            "",
            ""
        );
    }


    // =========================================================
    // MULTI-FINGER EVENT LOGGING
    // =========================================================
    //
    // Example:
    //
    // LogEvent(
    //     state,
    //     stimulus,
    //     "Haptic Update",
    //     55,
    //     "Middle",
    //     "M2"
    // );
    //
    // =========================================================

    public void LogEvent(
        string trialState,
        string stimulus,
        string eventType,
        int intensity,
        string finger,
        string motor
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


        // -----------------------------------------------------
        // TRIAL ELAPSED TIME
        // -----------------------------------------------------

        float elapsed =
            0f;

        if (trialTimerRunning)
        {
            elapsed =
                Time.time -
                trialStartTime;
        }

        string elapsedTime =
            elapsed.ToString(
                "F3"
            );


        // -----------------------------------------------------
        // MOTOR INTENSITY
        // -----------------------------------------------------

        string intensityText =
            intensity >= 0
            ? intensity.ToString()
            : "";


        // -----------------------------------------------------
        // READABLE LABELS
        // -----------------------------------------------------

        string readableCondition =
            GetReadableCondition(
                trialState
            );

        string readableStimulus =
            GetReadableStimulus(
                stimulus
            );


        // -----------------------------------------------------
        // CSV LINE
        // -----------------------------------------------------

        string line =
            EscapeCsv(sessionID) + "," +
            EscapeCsv(participantID) + "," +
            currentTrialNumber + "," +
            EscapeCsv(timestamp) + "," +
            elapsedTime + "," +
            EscapeCsv(readableCondition) + "," +
            EscapeCsv(readableStimulus) + "," +
            EscapeCsv(eventType) + "," +
            EscapeCsv(finger) + "," +
            EscapeCsv(motor) + "," +
            EscapeCsv(intensityText) + "\n";


        File.AppendAllText(
            filePath,
            line
        );
    }


    // =========================================================
    // CSV SAFETY
    // =========================================================

    private string EscapeCsv(
        string value
    )
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        bool needsQuotes =
            value.Contains(",") ||
            value.Contains("\"") ||
            value.Contains("\n") ||
            value.Contains("\r");

        if (!needsQuotes)
        {
            return value;
        }

        value =
            value.Replace(
                "\"",
                "\"\""
            );

        return
            "\"" +
            value +
            "\"";
    }


    // =========================================================
    // READABLE CONDITION
    // =========================================================

    private string GetReadableCondition(
        string trialState
    )
    {
        if (string.IsNullOrEmpty(trialState))
        {
            return "";
        }

        switch (trialState)
        {
            case "VisualExploration":
                return "Visual Exploration";

            case "HapticOnly":
                return "Haptic Only";

            case "Neutral":
                return "Neutral";

            default:
                return trialState;
        }
    }


    // =========================================================
    // READABLE STIMULUS
    // =========================================================

    private string GetReadableStimulus(
        string stimulus
    )
    {
        if (string.IsNullOrEmpty(stimulus))
        {
            return "";
        }

        switch (stimulus)
        {
            case "HapticCube_Flat":
                return "Flat";

            case "HapticCube_Ridges":
                return "Ridges";

            case "HapticCube_Bumps":
                return "Bumps";

            default:
                return stimulus;
        }
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