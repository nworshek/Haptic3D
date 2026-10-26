
#include <Wire.h>
#include <Adafruit_PWMServoDriver.h>

// ============================================================
// HARDWARE CONFIGURATION
// ============================================================

#define SDA_PIN 5
#define SCL_PIN 6

#define PCA_ADDRESS 0x40

#define INDEX_CHANNEL 0
#define MIDDLE_CHANNEL 2
#define RING_CHANNEL 15

#define PWM_FREQUENCY 1000
#define SERIAL_BAUD 115200

// ============================================================
// COMMUNICATION WATCHDOG
// ============================================================

// Python will send PING every 250 ms.
// Stop motors if no valid communication for 1500 ms.

const unsigned long WATCHDOG_TIMEOUT_MS = 1500;

unsigned long lastHeartbeatTime = 0;

bool watchdogExpired = false;
bool motorsActive = false;

// ============================================================
// PCA9685
// ============================================================

Adafruit_PWMServoDriver pwm =
    Adafruit_PWMServoDriver(PCA_ADDRESS);

// ============================================================
// MOTOR STATUS
// ============================================================

int indexIntensity = 0;
int middleIntensity = 0;
int ringIntensity = 0;

// ============================================================
// SETUP
// ============================================================

void setup()
{
    Serial.begin(SERIAL_BAUD);
    Serial.setTimeout(50);

    delay(1000);

    Serial.println();
    Serial.println("ESP32S3 Haptic Controller Starting...");

    Wire.begin(SDA_PIN, SCL_PIN);

    pwm.begin();
    pwm.setPWMFreq(PWM_FREQUENCY);

    delay(10);

    // Always initialize in a safe OFF state.
    allMotorsOff();

    lastHeartbeatTime = millis();

    Serial.println("M1 = Index  -> LED0");
    Serial.println("M2 = Middle -> LED2");
    Serial.println("M3 = Ring   -> LED15");
    Serial.println("WATCHDOG = 1500 ms");
    Serial.println("READY");
}

// ============================================================
// MAIN LOOP
// ============================================================

void loop()
{
    // Check for serial commands.
    if (Serial.available() > 0)
    {
        String command = Serial.readStringUntil('\n');

        command.trim();

        if (command.length() > 0)
        {
            processCommand(command);
        }
    }

    // Check communication watchdog.
    checkWatchdog();
}

// ============================================================
// WATCHDOG
// ============================================================

void refreshHeartbeat()
{
    lastHeartbeatTime = millis();

    // A heartbeat restores communication status,
    // but never restores a previous motor intensity.
    watchdogExpired = false;
}

void checkWatchdog()
{
    unsigned long elapsed =
        millis() - lastHeartbeatTime;

    if (elapsed >= WATCHDOG_TIMEOUT_MS)
    {
        if (!watchdogExpired)
        {
            watchdogExpired = true;

            // Force all motor channels OFF.
            allMotorsOff();

            Serial.println("WATCHDOG:TIMEOUT:MOTORS_OFF");
        }
    }
}

// ============================================================
// COMMAND PROCESSING
// ============================================================

void processCommand(String command)
{
    command.toUpperCase();

    // --------------------------------------------------------
    // PING / HEARTBEAT
    // --------------------------------------------------------

    if (command == "PING")
    {
        refreshHeartbeat();

        Serial.println("PONG");
        return;
    }

    // --------------------------------------------------------
    // ALL MOTORS OFF
    // --------------------------------------------------------

    if (command == "OFF")
    {
        allMotorsOff();

        refreshHeartbeat();

        Serial.println("OK:OFF");
        return;
    }

    // --------------------------------------------------------
    // STATUS
    // --------------------------------------------------------

    if (command == "STATUS")
    {
        // STATUS is diagnostic only.
        // It does not refresh the watchdog.

        Serial.print("STATUS:M1=");
        Serial.print(indexIntensity);

        Serial.print(",M2=");
        Serial.print(middleIntensity);

        Serial.print(",M3=");
        Serial.println(ringIntensity);

        return;
    }

    // --------------------------------------------------------
    // MOTOR 1 - INDEX
    // --------------------------------------------------------

    if (command.startsWith("M1:"))
    {
        int intensity;

        if (!parseIntensity(command, intensity))
        {
            Serial.println("ERROR:INVALID_INTENSITY");
            return;
        }

        refreshHeartbeat();

        setMotorIntensity(INDEX_CHANNEL, intensity);

        indexIntensity = intensity;

        updateMotorActiveStatus();

        Serial.print("OK:M1:");
        Serial.println(intensity);

        return;
    }

    // --------------------------------------------------------
    // MOTOR 2 - MIDDLE
    // --------------------------------------------------------

    if (command.startsWith("M2:"))
    {
        int intensity;

        if (!parseIntensity(command, intensity))
        {
            Serial.println("ERROR:INVALID_INTENSITY");
            return;
        }

        refreshHeartbeat();

        setMotorIntensity(MIDDLE_CHANNEL, intensity);

        middleIntensity = intensity;

        updateMotorActiveStatus();

        Serial.print("OK:M2:");
        Serial.println(intensity);

        return;
    }

    // --------------------------------------------------------
    // MOTOR 3 - RING
    // --------------------------------------------------------

    if (command.startsWith("M3:"))
    {
        int intensity;

        if (!parseIntensity(command, intensity))
        {
            Serial.println("ERROR:INVALID_INTENSITY");
            return;
        }

        refreshHeartbeat();

        setMotorIntensity(RING_CHANNEL, intensity);

        ringIntensity = intensity;

        updateMotorActiveStatus();

        Serial.print("OK:M3:");
        Serial.println(intensity);

        return;
    }

    // --------------------------------------------------------
    // UNKNOWN COMMAND
    // --------------------------------------------------------

    Serial.print("ERROR:UNKNOWN_COMMAND:");
    Serial.println(command);
}

// ============================================================
// INTENSITY VALIDATION
// ============================================================

bool parseIntensity(
    const String &command,
    int &intensity
)
{
    String value = command.substring(3);
    value.trim();

    if (value.length() == 0)
    {
        return false;
    }

    for (unsigned int i = 0; i < value.length(); i++)
    {
        if (value[i] < '0' || value[i] > '9')
        {
            return false;
        }
    }

    long parsed = value.toInt();

    intensity = constrain(parsed, 0L, 100L);

    return true;
}

// ============================================================
// MOTOR INTENSITY CONTROL
// ============================================================

void setMotorIntensity(
    int channel,
    int intensity
)
{
    intensity = constrain(intensity, 0, 100);

    // --------------------------------------------------------
    // 0% = OFF
    // --------------------------------------------------------

    if (intensity == 0)
    {
        motorOff(channel);
        return;
    }

    // --------------------------------------------------------
    // 100% = FULL ON
    // --------------------------------------------------------

    if (intensity == 100)
    {
        motorFullOn(channel);
        return;
    }

    /*
        ACTIVE-LOW PWM

        HIGH = motor OFF
        LOW  = motor ON

        Lower PWM OFF count = stronger vibration.
    */

    int pwmValue =
        map(intensity, 0, 100, 4095, 0);

    pwm.setPWM(
        channel,
        0,
        pwmValue
    );
}

// ============================================================
// TURN OFF ONE MOTOR
// ============================================================

void motorOff(int channel)
{
    // Force PCA9685 output HIGH.
    // Active-low motor driver: HIGH = OFF.

    pwm.setPWM(
        channel,
        4096,
        0
    );
}

// ============================================================
// FULL ON ONE MOTOR
// ============================================================

void motorFullOn(int channel)
{
    // Force PCA9685 output LOW.
    // Active-low motor driver: LOW = ON.

    pwm.setPWM(
        channel,
        0,
        4096
    );
}

// ============================================================
// TURN ALL MOTORS OFF
// ============================================================

void allMotorsOff()
{
    indexIntensity = 0;
    middleIntensity = 0;
    ringIntensity = 0;

    motorOff(INDEX_CHANNEL);
    motorOff(MIDDLE_CHANNEL);
    motorOff(RING_CHANNEL);

    motorsActive = false;
}

// ============================================================
// MOTOR ACTIVITY STATUS
// ============================================================

void updateMotorActiveStatus()
{
    motorsActive =
        indexIntensity > 0 ||
        middleIntensity > 0 ||
        ringIntensity > 0;
}
