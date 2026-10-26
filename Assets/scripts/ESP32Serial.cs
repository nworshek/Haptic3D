using UnityEngine;
using System;
using System.IO.Ports;
using System.Threading;


public class ESP32Serial : MonoBehaviour
{
    [Header("Serial Settings")]
    public string portName = "COM3";
    public int baudRate = 115200;


    [Header("Connection")]
    public bool connectOnStart = true;


    [Header("Motor Status")]
    [Range(0, 100)]
    public int indexIntensity = 0;

    [Range(0, 100)]
    public int middleIntensity = 0;

    [Range(0, 100)]
    public int ringIntensity = 0;


    private SerialPort serialPort;
    private Thread readThread;
    private bool isRunning = false;


    // =========================================================
    // CONNECTION STATUS
    // =========================================================

    public bool IsConnected
    {
        get
        {
            return serialPort != null &&
                   serialPort.IsOpen;
        }
    }


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        if (connectOnStart)
        {
            Connect();
        }
    }


    // =========================================================
    // CONNECT
    // =========================================================

    public void Connect()
    {
        if (IsConnected)
        {
            Debug.Log(
                "ESP32 is already connected."
            );

            return;
        }


        try
        {
            serialPort =
                new SerialPort(
                    portName,
                    baudRate
                );


            serialPort.ReadTimeout =
                100;


            serialPort.WriteTimeout =
                100;


            serialPort.NewLine =
                "\n";


            serialPort.Open();


            Debug.Log(
                "Connected to ESP32 on " +
                portName
            );


            isRunning =
                true;


            readThread =
                new Thread(
                    ReadSerial
                );


            readThread.IsBackground =
                true;


            readThread.Start();


            SendCommand(
                "PING"
            );


            // Always begin with all motors OFF.
            MotorOff();
        }
        catch (Exception e)
        {
            ResetMotorIntensities();


            Debug.LogError(
                "Could not connect to ESP32 on " +
                portName +
                "\n" +
                e.Message
            );
        }
    }


    // =========================================================
    // SERIAL READ THREAD
    // =========================================================

    private void ReadSerial()
    {
        while (isRunning)
        {
            if (
                serialPort != null &&
                serialPort.IsOpen
            )
            {
                try
                {
                    string message =
                        serialPort.ReadLine();


                    if (
                        !string.IsNullOrEmpty(
                            message
                        )
                    )
                    {
                        Debug.Log(
                            "ESP32: " +
                            message
                        );
                    }
                }
                catch (TimeoutException)
                {
                    // Normal when no message is waiting.
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        "Serial read error: " +
                        e.Message
                    );
                }
            }
        }
    }


    // =========================================================
    // SEND COMMAND
    // =========================================================

    public void SendCommand(
        string command
    )
    {
        if (!IsConnected)
        {
            // This is expected on HoloLens.
            //
            // The HoloLens does not directly own the physical
            // ESP32 serial connection. Motor intensities are
            // still stored locally and will later be sent to
            // the Lenovo through the TCP bridge.

            return;
        }


        try
        {
            serialPort.WriteLine(
                command
            );


            Debug.Log(
                "Sent to ESP32: " +
                command
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Serial write error: " +
                e.Message
            );
        }
    }


    // =========================================================
    // SET MOTOR INTENSITY
    // =========================================================

    public void SetMotorIntensity(
        int motorNumber,
        int intensity
    )
    {
        intensity =
            Mathf.Clamp(
                intensity,
                0,
                100
            );


        if (
            motorNumber < 1 ||
            motorNumber > 3
        )
        {
            Debug.LogWarning(
                "Invalid motor number: " +
                motorNumber
            );


            return;
        }


        // -----------------------------------------------------
        // IMPORTANT:
        //
        // Always store the requested intensity.
        //
        // Previously this only happened when IsConnected was
        // true. That worked when Unity directly owned COM3,
        // but it prevented the HoloLens from retaining the
        // calculated motor intensities.
        //
        // HoloLens now stores M1/M2/M3 even though it does not
        // have a local ESP32 serial connection.
        // -----------------------------------------------------

        SetStoredIntensity(
            motorNumber,
            intensity
        );


        // -----------------------------------------------------
        // LOCAL SERIAL OUTPUT
        //
        // If this script is running on a computer that actually
        // owns the ESP32 serial port, this sends the command.
        //
        // On HoloLens there is no local serial connection, so
        // SendCommand() safely returns without doing anything.
        // -----------------------------------------------------

        SendCommand(
            "M" +
            motorNumber +
            ":" +
            intensity
        );
    }


    // =========================================================
    // LEGACY SINGLE-MOTOR SUPPORT
    // =========================================================

    public void SetMotorIntensity(
        int intensity
    )
    {
        SetMotorIntensity(
            1,
            intensity
        );
    }


    // =========================================================
    // TURN OFF ONE MOTOR
    // =========================================================

    public void MotorOff(
        int motorNumber
    )
    {
        if (
            motorNumber < 1 ||
            motorNumber > 3
        )
        {
            Debug.LogWarning(
                "Invalid motor number: " +
                motorNumber
            );


            return;
        }


        // Always clear the stored intensity.
        SetStoredIntensity(
            motorNumber,
            0
        );


        // If local serial exists, send the OFF command.
        SendCommand(
            "M" +
            motorNumber +
            ":0"
        );
    }


    // =========================================================
    // TURN OFF ALL MOTORS
    // =========================================================

    public void MotorOff()
    {
        // Always clear all stored motor intensities.
        ResetMotorIntensities();


        // If local serial exists, send global OFF.
        SendCommand(
            "OFF"
        );
    }


    // =========================================================
    // STORE MOTOR INTENSITY
    // =========================================================

    private void SetStoredIntensity(
        int motorNumber,
        int intensity
    )
    {
        switch (motorNumber)
        {
            case 1:

                indexIntensity =
                    intensity;

                break;


            case 2:

                middleIntensity =
                    intensity;

                break;


            case 3:

                ringIntensity =
                    intensity;

                break;
        }
    }


    // =========================================================
    // RESET MOTOR INTENSITIES
    // =========================================================

    private void ResetMotorIntensities()
    {
        indexIntensity =
            0;


        middleIntensity =
            0;


        ringIntensity =
            0;
    }


    // =========================================================
    // GET MOTOR INTENSITY
    // =========================================================

    public int GetMotorIntensity(
        int motorNumber
    )
    {
        switch (motorNumber)
        {
            case 1:
                return indexIntensity;


            case 2:
                return middleIntensity;


            case 3:
                return ringIntensity;


            default:
                return 0;
        }
    }


    // =========================================================
    // GET CURRENT INTENSITY
    // =========================================================
    //
    // Kept so ExperimenterTCPServer continues working.
    //
    // Returns the strongest currently active motor.

    public int GetCurrentIntensity()
    {
        return Mathf.Max(
            indexIntensity,
            Mathf.Max(
                middleIntensity,
                ringIntensity
            )
        );
    }


    // =========================================================
    // PING
    // =========================================================

    public void Ping()
    {
        SendCommand(
            "PING"
        );
    }


    // =========================================================
    // STATUS REQUEST
    // =========================================================

    public void RequestStatus()
    {
        SendCommand(
            "STATUS"
        );
    }


    // =========================================================
    // DISCONNECT
    // =========================================================

    public void Disconnect()
    {
        // -----------------------------------------------------
        // SAFETY:
        // Always clear the locally stored haptic state.
        // -----------------------------------------------------

        ResetMotorIntensities();


        isRunning =
            false;


        if (
            readThread != null &&
            readThread.IsAlive
        )
        {
            readThread.Join(
                200
            );
        }


        if (
            serialPort != null &&
            serialPort.IsOpen
        )
        {
            try
            {
                // Physical ESP32 gets an OFF command before
                // the serial connection is closed.

                serialPort.WriteLine(
                    "OFF"
                );
            }
            catch
            {
                // Ignore shutdown write errors.
            }


            serialPort.Close();
        }


        Debug.Log(
            "ESP32 disconnected."
        );
    }


    // =========================================================
    // APPLICATION SHUTDOWN
    // =========================================================

    private void OnApplicationQuit()
    {
        Disconnect();
    }


    private void OnDestroy()
    {
        Disconnect();
    }
}