using UnityEngine;
using System;
using System.Collections.Generic;

#if WINDOWS_UWP && !UNITY_EDITOR
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#else
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
#endif


[DefaultExecutionOrder(400)]
public class ExperimenterTCPServer : MonoBehaviour
{
    [Header("Experiment")]
    public ExperimentController experimentController;

    [Header("Hardware")]
    public ESP32Serial esp32Serial;

    [Header("HoloLens Hand Tracking")]
    public HoloLensFingertipProxy holoLensFingertipProxy;

    [Header("Haptic3D Workspace Calibration")]
    public ExperimentWorkspaceCalibration workspaceCalibration;

    [Header("Temporary Simulated Hand Control")]
    public SimulatedFingertipController simulatedFingertipController;


    // =========================================================
    // SHAPE2D REFERENCES
    // =========================================================

    [Header("Shape2D Experiment")]
    public Shape2DTrialManager shape2DTrialManager;
    public Shape2DLogger shape2DLogger;
    public Shape2DWorkspaceCalibration shape2DWorkspaceCalibration;


    [Header("TCP Settings")]
    public int port = 5055;


    // =========================================================
    // SHARED COMMAND QUEUE
    // =========================================================

    private readonly Queue<string> commandQueue =
        new Queue<string>();

    private readonly object commandLock =
        new object();


    // =========================================================
    // SHARED STATUS SNAPSHOT
    // =========================================================

    private string statusSnapshot =
        "STATUS|0|0|None|None|WAITING|DISCONNECTED|0|0|0|0";

    private readonly object statusLock =
        new object();


    // =========================================================
    // PLATFORM-SPECIFIC SERVER OBJECTS
    // =========================================================

#if WINDOWS_UWP && !UNITY_EDITOR

    private StreamSocketListener uwpListener;

#else

    private TcpListener listener;
    private Thread serverThread;
    private bool serverRunning = false;

#endif


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

        if (holoLensFingertipProxy == null)
        {
            holoLensFingertipProxy =
                FindObjectOfType<HoloLensFingertipProxy>();
        }

        if (workspaceCalibration == null)
        {
            workspaceCalibration =
                FindObjectOfType<ExperimentWorkspaceCalibration>();
        }

        if (simulatedFingertipController == null)
        {
            simulatedFingertipController =
                FindObjectOfType<SimulatedFingertipController>();
        }

        if (shape2DTrialManager == null)
        {
            shape2DTrialManager =
                FindObjectOfType<Shape2DTrialManager>();
        }

        if (shape2DLogger == null)
            shape2DLogger = FindObjectOfType<Shape2DLogger>();

        if (shape2DWorkspaceCalibration == null)
        {
            shape2DWorkspaceCalibration =
                FindObjectOfType<Shape2DWorkspaceCalibration>();
        }


        if (experimentController == null && shape2DTrialManager == null)
        {
            Debug.LogError("No experiment controller available.");
            enabled = false;
            return;
        }

        if (esp32Serial == null)
        {
            Debug.LogWarning(
                "ExperimenterTCPServer: ESP32Serial not found."
            );
        }

        if (holoLensFingertipProxy == null)
        {
            Debug.LogWarning(
                "ExperimenterTCPServer: HoloLensFingertipProxy not found."
            );
        }

        if (workspaceCalibration == null)
        {
            Debug.LogWarning(
                "ExperimenterTCPServer: Haptic3D calibration not found."
            );
        }

        if (simulatedFingertipController == null)
        {
            Debug.LogWarning(
                "ExperimenterTCPServer: SimulatedFingertipController not found."
            );
        }

        if (shape2DTrialManager == null)
        {
            Debug.LogWarning(
                "ExperimenterTCPServer: Shape2DTrialManager not found."
            );
        }

        if (shape2DWorkspaceCalibration == null)
        {
            Debug.LogWarning(
                "ExperimenterTCPServer: Shape2DWorkspaceCalibration not found."
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

            ProcessCommand(commandToProcess);
        }


    }

    void LateUpdate()
    {
        UpdateStatusSnapshot();
    }


    // =========================================================
    // STATUS
    // =========================================================

    private void UpdateStatusSnapshot()
    {
        if (experimentController == null && shape2DTrialManager == null)
        {
            return;
        }


        int trialNumber = 0;
        int totalTrials = 3;
        string condition = "Haptic Only";
        string stimulus = "None";
        string trialStatus = "WAITING";
        if (shape2DTrialManager != null)
        {
            trialNumber = shape2DTrialManager.currentTrialNumber;
            totalTrials = Shape2DTrialManager.TotalTrials;
            stimulus = trialNumber > 0 ? shape2DTrialManager.GetCurrentShape().ToString() : "None";
            trialStatus = shape2DTrialManager.ExperimentComplete() ? "EXPERIMENT COMPLETE" :
                shape2DTrialManager.IsTestModeActive() ? "TEST MODE" :
                shape2DTrialManager.GetTrialState().ToString().ToUpperInvariant();
        }
        else if (experimentController != null)
        {
            trialNumber = experimentController.GetCurrentTrialNumber();
            totalTrials = experimentController.GetTotalTrials();
            condition = GetReadableCondition(experimentController.GetCurrentCondition());
            stimulus = experimentController.GetCurrentStimulus();
            trialStatus = experimentController.GetTrialStatus();
        }

        string espStatus = "DISCONNECTED";

        int motorIntensity = 0;
        int motor1Intensity = 0;
        int motor2Intensity = 0;
        int motor3Intensity = 0;


        if (esp32Serial != null)
        {
            if (esp32Serial.IsConnected)
            {
                espStatus = "CONNECTED";
            }

            motorIntensity =
                esp32Serial.GetCurrentIntensity();

            motor1Intensity =
                esp32Serial.GetMotorIntensity(1);

            motor2Intensity =
                esp32Serial.GetMotorIntensity(2);

            motor3Intensity =
                esp32Serial.GetMotorIntensity(3);
        }


        // IMPORTANT:
        // Keep this as the existing 11-field STATUS packet.

        string snapshot =
            "STATUS|" +
            trialNumber + "|" +
            totalTrials + "|" +
            condition + "|" +
            stimulus + "|" +
            trialStatus + "|" +
            espStatus + "|" +
            motorIntensity + "|" +
            motor1Intensity + "|" +
            motor2Intensity + "|" +
            motor3Intensity;


        lock (statusLock)
        {
            statusSnapshot = snapshot;
        }
    }


    // =========================================================
    // CONDITION NAME
    // =========================================================

    private string GetReadableCondition(string condition)
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
    // START SERVER
    // =========================================================

    private void StartServer()
    {
#if WINDOWS_UWP && !UNITY_EDITOR

        StartUWPServer();

#else

        StartDesktopServer();

#endif
    }


    // =========================================================
    // UWP / HOLOLENS SERVER
    // =========================================================

#if WINDOWS_UWP && !UNITY_EDITOR

    private async void StartUWPServer()
    {
        try
        {
            uwpListener =
                new StreamSocketListener();

            uwpListener.ConnectionReceived +=
                OnUWPConnectionReceived;

            await uwpListener.BindServiceNameAsync(
                port.ToString()
            );

            Debug.Log(
                "Experimenter UWP TCP Server started on port " +
                port
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "UWP TCP Server failed to start: " +
                e.Message
            );
        }
    }


    private async void OnUWPConnectionReceived(
        StreamSocketListener sender,
        StreamSocketListenerConnectionReceivedEventArgs args
    )
    {
        try
        {
            using (StreamSocket socket = args.Socket)
            using (
                DataReader reader =
                    new DataReader(socket.InputStream)
            )
            using (
                DataWriter writer =
                    new DataWriter(socket.OutputStream)
            )
            {
                reader.InputStreamOptions =
                    InputStreamOptions.Partial;

                uint bytesLoaded =
                    await reader.LoadAsync(1024);

                if (bytesLoaded == 0)
                {
                    return;
                }

                string received =
                    reader.ReadString(bytesLoaded);

                if (string.IsNullOrEmpty(received))
                {
                    return;
                }

                string command =
                    received.Trim();


                if (
                    command.Equals(
                        "STATUS",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    string response;

                    lock (statusLock)
                    {
                        response = statusSnapshot;
                    }

                    writer.WriteString(
                        response + "\n"
                    );

                    await writer.StoreAsync();
                    await writer.FlushAsync();

                    return;
                }


                lock (commandLock)
                {
                    commandQueue.Enqueue(command);
                }

                writer.WriteString("OK\n");

                await writer.StoreAsync();
                await writer.FlushAsync();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "UWP TCP connection error: " +
                e.Message
            );
        }
    }

#endif


    // =========================================================
    // DESKTOP TCP SERVER
    // =========================================================

#if !WINDOWS_UWP || UNITY_EDITOR

    private void StartDesktopServer()
    {
        try
        {
            listener =
                new TcpListener(
                    IPAddress.Any,
                    port
                );

            listener.Start();

            serverRunning = true;

            serverThread =
                new Thread(DesktopServerLoop);

            serverThread.IsBackground = true;

            serverThread.Start();

            Debug.Log(
                "Experimenter Desktop TCP Server started on port " +
                port
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Desktop TCP Server failed to start: " +
                e.Message
            );
        }
    }


    private void DesktopServerLoop()
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
                    writer.AutoFlush = true;

                    string received =
                        reader.ReadLine();

                    if (string.IsNullOrEmpty(received))
                    {
                        continue;
                    }

                    string command =
                        received.Trim();


                    if (
                        command.Equals(
                            "STATUS",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        string response;

                        lock (statusLock)
                        {
                            response = statusSnapshot;
                        }

                        writer.WriteLine(response);

                        continue;
                    }


                    lock (commandLock)
                    {
                        commandQueue.Enqueue(command);
                    }

                    writer.WriteLine("OK");
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
                        "Desktop TCP Server error: " +
                        e.Message
                    );
                }
            }
        }
    }

#endif


    // =========================================================
    // PROCESS COMMAND
    // =========================================================

    private void ProcessCommand(string command)
    {
        Debug.Log(
            "Experimenter command received: " +
            command
        );


        // =====================================================
        // PARTICIPANT ID
        // =====================================================

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


            if (TrialLogger.Instance != null)
            {
                TrialLogger.Instance
                    .SetParticipantID(participantID);
            }

            if (shape2DLogger != null)
                shape2DLogger.SetParticipantID(participantID);

            return;
        }


        // =====================================================
        // PARTICIPANT HAND
        // =====================================================

        if (
            command.StartsWith(
                "HAND:",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            string selectedHand =
                command.Substring(
                    "HAND:".Length
                ).Trim();


            if (holoLensFingertipProxy == null)
            {
                Debug.LogWarning(
                    "HoloLensFingertipProxy unavailable."
                );

                return;
            }


            if (
                selectedHand.Equals(
                    "RIGHT",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                holoLensFingertipProxy
                    .SetParticipantHandRight();
            }
            else if (
                selectedHand.Equals(
                    "LEFT",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                holoLensFingertipProxy
                    .SetParticipantHandLeft();
            }

            return;
        }


        string upperCommand =
            command.ToUpper();


        // =====================================================
        // SIMULATED HAND
        // =====================================================

        if (simulatedFingertipController != null)
        {
            switch (upperCommand)
            {
                case "MOVE:LEFT":

                    simulatedFingertipController
                        .SetRemoteMovement(-1f, 0f, 0f);

                    return;


                case "MOVE:RIGHT":

                    simulatedFingertipController
                        .SetRemoteMovement(1f, 0f, 0f);

                    return;


                case "MOVE:UP":

                    simulatedFingertipController
                        .SetRemoteMovement(0f, 1f, 0f);

                    return;


                case "MOVE:DOWN":

                    simulatedFingertipController
                        .SetRemoteMovement(0f, -1f, 0f);

                    return;


                case "MOVE:FORWARD":

                    simulatedFingertipController
                        .SetRemoteMovement(0f, 0f, 1f);

                    return;


                case "MOVE:BACK":

                    simulatedFingertipController
                        .SetRemoteMovement(0f, 0f, -1f);

                    return;


                case "MOVE:STOP":

                    simulatedFingertipController
                        .StopRemoteMovement();

                    return;


                case "RESET_HAND":

                    simulatedFingertipController
                        .ResetFingertip();

                    return;
            }
        }


        // =====================================================
        // SHAPE2D TEST MODE
        // =====================================================

        switch (upperCommand)
        {
            case "SHAPE2D:TRIAL:PREPARE":
                if (shape2DTrialManager != null && shape2DTrialManager.PrepareNextTrial())
                {
                    if (esp32Serial != null) esp32Serial.MotorOff();
                    if (shape2DLogger != null)
                        shape2DLogger.LogShape2DEvent(shape2DTrialManager.currentTrialNumber,
                            shape2DTrialManager.currentShape.ToString(), "Prepared");
                }
                return;

            case "SHAPE2D:TRIAL:START":
                if (shape2DTrialManager != null &&
                    shape2DTrialManager.GetTrialState() == Shape2DTrialManager.TrialState.Prepared)
                {
                    shape2DTrialManager.StartTrial();
                    if (shape2DLogger != null)
                        shape2DLogger.StartShape2DTrial(shape2DTrialManager.currentTrialNumber,
                            shape2DTrialManager.currentShape.ToString());
                }
                return;

            case "SHAPE2D:TRIAL:END":
                if (shape2DTrialManager != null && shape2DTrialManager.IsTrialActive())
                {
                    shape2DTrialManager.EndTrial();
                    if (esp32Serial != null) esp32Serial.MotorOff();
                    if (shape2DLogger != null)
                        shape2DLogger.EndShape2DTrial(shape2DTrialManager.currentTrialNumber,
                            shape2DTrialManager.currentShape.ToString());
                }
                return;

            case "SHAPE2D:RESPONSE:SQUARE":
            case "SHAPE2D:RESPONSE:CIRCLE":
            case "SHAPE2D:RESPONSE:TRIANGLE":
                if (shape2DTrialManager != null)
                {
                    string answer = upperCommand.Substring("SHAPE2D:RESPONSE:".Length);
                    answer = char.ToUpperInvariant(answer[0]) + answer.Substring(1).ToLowerInvariant();
                    if (shape2DTrialManager.RecordResponse(answer) && shape2DLogger != null)
                        shape2DLogger.RecordShape2DResponse(shape2DTrialManager.currentTrialNumber,
                            shape2DTrialManager.currentShape.ToString(), answer);
                }
                return;

            // -------------------------------------------------
            // CALIBRATE
            // -------------------------------------------------

            case "SHAPE2D:CALIBRATE":

                if (shape2DWorkspaceCalibration == null)
                {
                    Debug.LogWarning(
                        "Shape2D workspace calibration unavailable."
                    );

                    return;
                }


                if (esp32Serial != null)
                {
                    esp32Serial.MotorOff();
                }


                shape2DWorkspaceCalibration
                    .Calibrate();


                Debug.Log(
                    "Shape2D workspace calibrated."
                );

                return;


            // -------------------------------------------------
            // ENTER TEST MODE
            // -------------------------------------------------

            case "SHAPE2D:TEST:ENTER":

                if (shape2DTrialManager == null)
                {
                    Debug.LogWarning(
                        "Shape2DTrialManager unavailable."
                    );

                    return;
                }


                if (esp32Serial != null)
                {
                    esp32Serial.MotorOff();
                }


                shape2DTrialManager
                    .EnterTestMode();


                Debug.Log(
                    "Shape2D Test Mode entered."
                );

                return;


            // -------------------------------------------------
            // SQUARE
            // -------------------------------------------------

            case "SHAPE2D:TEST:SQUARE":

                if (shape2DTrialManager == null)
                {
                    return;
                }


                shape2DTrialManager
                    .TestSquare();


                Debug.Log(
                    "Shape2D test shape: Square"
                );

                return;


            // -------------------------------------------------
            // CIRCLE
            // -------------------------------------------------

            case "SHAPE2D:TEST:CIRCLE":

                if (shape2DTrialManager == null)
                {
                    return;
                }


                shape2DTrialManager
                    .TestCircle();


                Debug.Log(
                    "Shape2D test shape: Circle"
                );

                return;


            // -------------------------------------------------
            // TRIANGLE
            // -------------------------------------------------

            case "SHAPE2D:TEST:TRIANGLE":

                if (shape2DTrialManager == null)
                {
                    return;
                }


                shape2DTrialManager
                    .TestTriangle();


                Debug.Log(
                    "Shape2D test shape: Triangle"
                );

                return;


            // -------------------------------------------------
            // HAPTICS ON
            // -------------------------------------------------

            case "SHAPE2D:TEST:HAPTICS:ON":

                if (shape2DTrialManager == null)
                {
                    return;
                }


                shape2DTrialManager
                    .EnableTestHaptics();


                Debug.Log(
                    "Shape2D test haptics ON."
                );

                return;


            // -------------------------------------------------
            // HAPTICS OFF
            // -------------------------------------------------

            case "SHAPE2D:TEST:HAPTICS:OFF":

                if (shape2DTrialManager == null)
                {
                    return;
                }


                shape2DTrialManager
                    .DisableTestHaptics();


                if (esp32Serial != null)
                {
                    esp32Serial.MotorOff();
                }


                Debug.Log(
                    "Shape2D test haptics OFF."
                );

                return;


            // -------------------------------------------------
            // EXIT TEST MODE
            // -------------------------------------------------

            case "SHAPE2D:TEST:EXIT":

                if (shape2DTrialManager == null)
                {
                    return;
                }


                shape2DTrialManager
                    .ExitTestMode();


                if (esp32Serial != null)
                {
                    esp32Serial.MotorOff();
                }


                Debug.Log(
                    "Shape2D Test Mode exited."
                );

                return;


            // -------------------------------------------------
            // EMERGENCY STOP
            // -------------------------------------------------

            case "SHAPE2D:EMERGENCY":

                if (shape2DTrialManager != null)
                {
                    shape2DTrialManager
                        .EmergencyStop();
                }


                if (esp32Serial != null)
                {
                    esp32Serial.MotorOff();
                }


                Debug.Log(
                    "Shape2D EMERGENCY STOP."
                );

                return;
        }


        // =====================================================
        // ORIGINAL HAPTIC3D TEST MODE
        // =====================================================

        switch (upperCommand)
        {
            case "TEST:ENTER":

                if (simulatedFingertipController != null)
                {
                    simulatedFingertipController
                        .StopRemoteMovement();
                }

                if (esp32Serial != null)
                {
                    esp32Serial.MotorOff();
                }

                if (
                    experimentController == null ||
                    experimentController.trialManager == null
                )
                {
                    return;
                }

                experimentController
                    .trialManager
                    .EnterExperimenterTestMode();

                return;


            case "TEST:FLAT":

                if (
                    experimentController == null ||
                    experimentController.trialManager == null
                )
                {
                    return;
                }

                experimentController
                    .trialManager
                    .TestFlat();

                return;


            case "TEST:RIDGES":

                if (
                    experimentController == null ||
                    experimentController.trialManager == null
                )
                {
                    return;
                }

                experimentController
                    .trialManager
                    .TestRidges();

                return;


            case "TEST:BUMPS":

                if (
                    experimentController == null ||
                    experimentController.trialManager == null
                )
                {
                    return;
                }

                experimentController
                    .trialManager
                    .TestBumps();

                return;


            case "TEST:HAPTICS:ON":

                if (
                    experimentController == null ||
                    experimentController.trialManager == null
                )
                {
                    return;
                }

                experimentController
                    .trialManager
                    .EnableTestHaptics();

                return;


            case "TEST:HAPTICS:OFF":

                if (
                    experimentController == null ||
                    experimentController.trialManager == null
                )
                {
                    return;
                }

                experimentController
                    .trialManager
                    .DisableTestHaptics();

                return;


            case "TEST:EXIT":

                if (simulatedFingertipController != null)
                {
                    simulatedFingertipController
                        .StopRemoteMovement();
                }

                if (esp32Serial != null)
                {
                    esp32Serial.MotorOff();
                }

                if (
                    experimentController == null ||
                    experimentController.trialManager == null
                )
                {
                    return;
                }

                experimentController
                    .trialManager
                    .ExitExperimenterTestMode();

                return;
        }


        // =====================================================
        // ORIGINAL HAPTIC3D EXPERIMENT
        // =====================================================

        switch (upperCommand)
        {
            case "CALIBRATE":

                if (simulatedFingertipController != null)
                {
                    simulatedFingertipController
                        .StopRemoteMovement();
                }

                if (esp32Serial != null)
                {
                    esp32Serial.MotorOff();
                }

                if (workspaceCalibration == null)
                {
                    break;
                }

                workspaceCalibration
                    .CalibrateWorkspace();

                break;


            case "PREPARE":

                if (simulatedFingertipController != null)
                {
                    simulatedFingertipController
                        .StopRemoteMovement();
                }

                if (esp32Serial != null)
                {
                    esp32Serial.MotorOff();
                }

                experimentController
                    .PrepareNextTrial();

                break;


            case "START":

                if (simulatedFingertipController != null)
                {
                    simulatedFingertipController
                        .StopRemoteMovement();
                }

                experimentController
                    .StartPreparedTrial();

                break;


            case "END":

                if (simulatedFingertipController != null)
                {
                    simulatedFingertipController
                        .StopRemoteMovement();
                }

                if (esp32Serial != null)
                {
                    esp32Serial.MotorOff();
                }

                experimentController
                    .EndCurrentTrial();

                break;


            case "REPEAT":

                if (simulatedFingertipController != null)
                {
                    simulatedFingertipController
                        .StopRemoteMovement();
                }

                if (esp32Serial != null)
                {
                    esp32Serial.MotorOff();
                }

                experimentController
                    .RepeatCurrentTrial();

                break;


            case "EMERGENCY":

                if (simulatedFingertipController != null)
                {
                    simulatedFingertipController
                        .StopRemoteMovement();
                }

                if (esp32Serial != null)
                {
                    esp32Serial.MotorOff();
                }

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
        if (simulatedFingertipController != null)
        {
            simulatedFingertipController
                .StopRemoteMovement();
        }


        if (shape2DTrialManager != null)
        {
            shape2DTrialManager
                .EmergencyStop();
        }


        if (esp32Serial != null)
        {
            esp32Serial.MotorOff();
        }


#if WINDOWS_UWP && !UNITY_EDITOR

        if (uwpListener != null)
        {
            uwpListener.ConnectionReceived -=
                OnUWPConnectionReceived;

            uwpListener.Dispose();

            uwpListener = null;
        }

#else

        serverRunning = false;


        if (listener != null)
        {
            listener.Stop();

            listener = null;
        }


        if (
            serverThread != null &&
            serverThread.IsAlive
        )
        {
            serverThread.Join(500);
        }

#endif
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