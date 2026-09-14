using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections.Generic;

public class ExperimenterTCPServer : MonoBehaviour
{
    [Header("Experiment")]
    public ExperimentController experimentController;

    [Header("Hardware")]
    public ESP32Serial esp32Serial;

    [Header("TCP Settings")]
    public int port = 5055;

    private TcpListener listener;
    private Thread serverThread;
    private bool serverRunning = false;

    // Queue commands so rapid commands cannot overwrite one another.
    private readonly Queue<string> commandQueue =
        new Queue<string>();

    private readonly object commandLock =
        new object();

    // Current Unity status that can safely be returned
    // from the TCP background thread.
    private string statusSnapshot =
        "STATUS|0|0|None|None|WAITING|DISCONNECTED|0";

    private readonly object statusLock =
        new object();


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        if (experimentController == null)
        {
            experimentController =
                FindObjectOfType<ExperimentController>();
        }

        if (esp32Serial == null)
        {
            esp32Serial =
                FindObjectOfType<ESP32Serial>();
        }

        if (experimentController == null)
        {
            Debug.LogError(
                "ExperimenterTCPServer: ExperimentController not found."
            );

            enabled = false;
            return;
        }

        if (esp32Serial == null)
        {
            Debug.LogWarning(
                "ExperimenterTCPServer: ESP32Serial not found. " +
                "Hardware status will show DISCONNECTED."
            );
        }

        UpdateStatusSnapshot();
        StartServer();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        // Process every command waiting in the queue.
        while (true)
        {
            string commandToProcess = null;

            lock (commandLock)
            {
                if (commandQueue.Count > 0)
                {
                    commandToProcess =
                        commandQueue.Dequeue();
                }
            }

            if (string.IsNullOrEmpty(commandToProcess))
            {
                break;
            }

            ProcessCommand(
                commandToProcess
            );
        }

        // Refresh the information returned to Python.
        UpdateStatusSnapshot();
    }


    // =========================================================
    // STATUS SNAPSHOT
    // =========================================================

    private void UpdateStatusSnapshot()
    {
        if (experimentController == null)
        {
            return;
        }

        int trialNumber =
            experimentController.GetCurrentTrialNumber();

        int totalTrials =
            experimentController.GetTotalTrials();

        string condition =
            GetReadableCondition(
                experimentController.GetCurrentCondition()
            );

        string stimulus =
            experimentController.GetCurrentStimulus();

        string trialStatus =
            experimentController.GetTrialStatus();

        string espStatus =
            "DISCONNECTED";

        int motorIntensity =
            0;

        if (esp32Serial != null)
        {
            if (esp32Serial.IsConnected)
            {
                espStatus =
                    "CONNECTED";
            }

            motorIntensity =
                esp32Serial.GetCurrentIntensity();
        }

        string snapshot =
            "STATUS|" +
            trialNumber + "|" +
            totalTrials + "|" +
            condition + "|" +
            stimulus + "|" +
            trialStatus + "|" +
            espStatus + "|" +
            motorIntensity;

        lock (statusLock)
        {
            statusSnapshot =
                snapshot;
        }
    }


    // =========================================================
    // READABLE CONDITION NAMES
    // =========================================================

    private string GetReadableCondition(
        string condition
    )
    {
        switch (condition)
        {
            case "VisualExploration":
                return "Visual Exploration";

            case "HapticOnly":
                return "Haptic Only";

            case "Neutral":
                return "Neutral";

            default:
                return condition;
        }
    }


    // =========================================================
    // START TCP SERVER
    // =========================================================

    private void StartServer()
    {
        try
        {
            listener =
                new TcpListener(
                    IPAddress.Loopback,
                    port
                );

            listener.Start();

            serverRunning =
                true;

            serverThread =
                new Thread(
                    ServerLoop
                );

            serverThread.IsBackground =
                true;

            serverThread.Start();

            Debug.Log(
                "Experimenter TCP Server started on 127.0.0.1:" +
                port
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "TCP Server failed to start: " +
                e.Message
            );
        }
    }


    // =========================================================
    // TCP SERVER LOOP
    // =========================================================

    private void ServerLoop()
    {
        while (serverRunning)
        {
            try
            {
                using (
                    TcpClient client =
                        listener.AcceptTcpClient()
                )
                using (
                    NetworkStream stream =
                        client.GetStream()
                )
                using (
                    StreamReader reader =
                        new StreamReader(stream)
                )
                using (
                    StreamWriter writer =
                        new StreamWriter(stream)
                )
                {
                    writer.AutoFlush =
                        true;

                    string received =
                        reader.ReadLine();

                    if (string.IsNullOrEmpty(received))
                    {
                        continue;
                    }

                    string command =
                        received.Trim();

                    // STATUS requests do not need to be queued.
                    // Return the most recent Unity snapshot.
                    if (
                        command.ToUpper() ==
                        "STATUS"
                    )
                    {
                        string response;

                        lock (statusLock)
                        {
                            response =
                                statusSnapshot;
                        }

                        writer.WriteLine(
                            response
                        );

                        continue;
                    }

                    // All experiment-changing commands must run
                    // on Unity's main thread.
                    lock (commandLock)
                    {
                        commandQueue.Enqueue(
                            command
                        );
                    }

                    writer.WriteLine(
                        "OK"
                    );
                }
            }
            catch (SocketException)
            {
                if (!serverRunning)
                {
                    return;
                }
            }
            catch (Exception e)
            {
                if (serverRunning)
                {
                    Debug.LogWarning(
                        "TCP Server error: " +
                        e.Message
                    );
                }
            }
        }
    }


    // =========================================================
    // PROCESS COMMAND
    // =========================================================

    private void ProcessCommand(
        string command
    )
    {
        Debug.Log(
            "Experimenter command received: " +
            command
        );

        // -------------------------------------------------
        // PARTICIPANT ID
        // -------------------------------------------------

        if (
            command.StartsWith(
                "PARTICIPANT:",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            string participantID =
                command.Substring(
                    "PARTICIPANT:".Length
                ).Trim();

            if (
                TrialLogger.Instance != null
            )
            {
                TrialLogger.Instance
                    .SetParticipantID(
                        participantID
                    );

                Debug.Log(
                    "Participant ID received from Experimenter App: " +
                    participantID
                );
            }
            else
            {
                Debug.LogWarning(
                    "TrialLogger instance not found."
                );
            }

            return;
        }

        // Commands are case-insensitive.
        string upperCommand =
            command.ToUpper();

        // -------------------------------------------------
        // EXPERIMENT COMMANDS
        // -------------------------------------------------

        switch (upperCommand)
        {
            case "PREPARE":

                experimentController
                    .PrepareNextTrial();

                break;


            case "START":

                experimentController
                    .StartPreparedTrial();

                break;


            case "END":

                experimentController
                    .EndCurrentTrial();

                break;


            case "REPEAT":

                experimentController
                    .RepeatCurrentTrial();

                break;


            case "EMERGENCY":

                experimentController
                    .EmergencyStop();

                break;


            default:

                Debug.LogWarning(
                    "Unknown experimenter command: " +
                    command
                );

                break;
        }
    }


    // =========================================================
    // STOP SERVER
    // =========================================================

    private void StopServer()
    {
        serverRunning =
            false;

        if (listener != null)
        {
            listener.Stop();
        }

        if (
            serverThread != null &&
            serverThread.IsAlive
        )
        {
            serverThread.Join(
                500
            );
        }
    }


    // =========================================================
    // SHUTDOWN
    // =========================================================

    void OnApplicationQuit()
    {
        StopServer();
    }

    void OnDestroy()
    {
        StopServer();
    }
}