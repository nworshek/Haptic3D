using UnityEngine;
using System;
using System.IO.Ports;
using System.Threading;

public class ESP32Serial : MonoBehaviour
{
    [Header("Serial Settings")]
    public string portName = "/dev/cu.usbmodem1101";
    public int baudRate = 115200;

    [Header("Connection")]
    public bool connectOnStart = true;

    [Header("Motor Status")]
    [Range(0, 100)]
    public int currentIntensity = 0;

    private SerialPort serialPort;
    private Thread readThread;
    private bool isRunning = false;

    // ============================================================
    // CONNECTION STATUS
    // ============================================================

    public bool IsConnected
    {
        get
        {
            return serialPort != null && serialPort.IsOpen;
        }
    }


    // ============================================================
    // START
    // ============================================================

    void Start()
    {
        if (connectOnStart)
        {
            Connect();
        }
    }


    // ============================================================
    // CONNECT
    // ============================================================

    public void Connect()
    {
        if (IsConnected)
        {
            Debug.Log("ESP32 is already connected.");
            return;
        }

        try
        {
            serialPort = new SerialPort(portName, baudRate);

            serialPort.ReadTimeout = 100;
            serialPort.WriteTimeout = 100;

            serialPort.NewLine = "\n";

            serialPort.Open();

            Debug.Log("Connected to ESP32 on " + portName);

            isRunning = true;

            readThread = new Thread(ReadSerial);
            readThread.IsBackground = true;
            readThread.Start();

            SendCommand("PING");

            // Always begin with motor OFF
            MotorOff();
        }
        catch (Exception e)
        {
            currentIntensity = 0;

            Debug.LogError(
                "Could not connect to ESP32 on " +
                portName +
                "\n" +
                e.Message
            );
        }
    }


    // ============================================================
    // SERIAL READ THREAD
    // ============================================================

    private void ReadSerial()
    {
        while (isRunning)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    string message = serialPort.ReadLine();

                    if (!string.IsNullOrEmpty(message))
                    {
                        Debug.Log("ESP32: " + message);
                    }
                }
                catch (TimeoutException)
                {
                    // Normal when no serial message is waiting.
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


    // ============================================================
    // SEND COMMAND
    // ============================================================

    public void SendCommand(string command)
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
            serialPort.WriteLine(command);

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


    // ============================================================
    // MOTOR INTENSITY
    // ============================================================

    public void SetMotorIntensity(int intensity)
    {
        intensity =
            Mathf.Clamp(intensity, 0, 100);

        // Only record an intensity if the ESP32
        // is actually connected.
        if (IsConnected)
        {
            currentIntensity = intensity;
        }

        SendCommand(
            "M1:" + intensity
        );
    }


    // ============================================================
    // MOTOR OFF
    // ============================================================

    public void MotorOff()
    {
        currentIntensity = 0;

        SendCommand("OFF");
    }


    // ============================================================
    // GET CURRENT MOTOR INTENSITY
    // ============================================================

    public int GetCurrentIntensity()
    {
        return currentIntensity;
    }


    // ============================================================
    // PING
    // ============================================================

    public void Ping()
    {
        SendCommand("PING");
    }


    // ============================================================
    // STATUS REQUEST
    // ============================================================

    public void RequestStatus()
    {
        SendCommand("STATUS");
    }


    // ============================================================
    // DISCONNECT
    // ============================================================

    public void Disconnect()
    {
        currentIntensity = 0;

        isRunning = false;

        if (readThread != null && readThread.IsAlive)
        {
            readThread.Join(200);
        }

        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                serialPort.WriteLine("OFF");
            }
            catch
            {
            }

            serialPort.Close();
        }

        Debug.Log("ESP32 disconnected.");
    }


    // ============================================================
    // APPLICATION SHUTDOWN
    // ============================================================

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    private void OnDestroy()
    {
        Disconnect();
    }
}