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
            Debug.LogWarning(
                "ESP32 is not connected. Command not sent: " +
                command
            );

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

        if (IsConnected)
        {
            SetStoredIntensity(
                motorNumber,
                intensity
            );
        }

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

        SetStoredIntensity(
            motorNumber,
            0
        );

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
        ResetMotorIntensities();

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
    // Kept so your ExperimenterTCPServer does not break.
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
                serialPort.WriteLine(
                    "OFF"
                );
            }
            catch
            {
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