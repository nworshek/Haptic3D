using UnityEngine;

public class HapticKeyboardTest : MonoBehaviour
{
    [Header("ESP32 Connection")]
    public ESP32Serial esp32Serial;

    void Start()
    {
        // Automatically find ESP32Serial on the same GameObject
        // if the Inspector reference is missing.
        if (esp32Serial == null)
        {
            esp32Serial = GetComponent<ESP32Serial>();
        }

        if (esp32Serial == null)
        {
            Debug.LogError(
                "HapticKeyboardTest: ESP32Serial reference is missing."
            );
        }
    }

    void Update()
    {
        if (esp32Serial == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log("Keyboard Test: Motor 25%");
            esp32Serial.SetMotorIntensity(25);
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Debug.Log("Keyboard Test: Motor 50%");
            esp32Serial.SetMotorIntensity(50);
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Debug.Log("Keyboard Test: Motor 75%");
            esp32Serial.SetMotorIntensity(75);
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            Debug.Log("Keyboard Test: Motor 100%");
            esp32Serial.SetMotorIntensity(100);
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Debug.Log("Keyboard Test: Motor OFF");
            esp32Serial.MotorOff();
        }
    }
}