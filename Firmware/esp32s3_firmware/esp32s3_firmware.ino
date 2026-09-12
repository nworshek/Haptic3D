#include <Wire.h>
#include <Adafruit_PWMServoDriver.h>

// ============================================================
// HARDWARE CONFIGURATION
// ============================================================

// XIAO ESP32S3 I2C pins
#define SDA_PIN 5
#define SCL_PIN 6

// PCA9685 I2C address
#define PCA_ADDRESS 0x40

// Motor is connected to PCA9685 LED0
// PCA channels are numbered starting at 0:
// LED0 = 0
// LED1 = 1
#define MOTOR_CHANNEL 0

// PCA9685 PWM frequency
#define PWM_FREQUENCY 1000

// Serial communication baud rate
#define SERIAL_BAUD 115200


// ============================================================
// PCA9685 OBJECT
// ============================================================

Adafruit_PWMServoDriver pwm =
    Adafruit_PWMServoDriver(PCA_ADDRESS);


// Store current motor intensity
int currentIntensity = 0;


// ============================================================
// SETUP
// ============================================================

void setup()
{
    // Start USB serial communication
    Serial.begin(SERIAL_BAUD);

    delay(1000);

    Serial.println();
    Serial.println("ESP32S3 Haptic Controller Starting...");


    // --------------------------------------------------------
    // Start I2C
    // --------------------------------------------------------

    Wire.begin(SDA_PIN, SCL_PIN);


    // --------------------------------------------------------
    // Start PCA9685
    // --------------------------------------------------------

    pwm.begin();

    pwm.setPWMFreq(PWM_FREQUENCY);

    delay(10);


    // --------------------------------------------------------
    // Start with motor OFF
    // --------------------------------------------------------

    motorOff();

    Serial.println("Motor Channel: LED0");
    Serial.println("READY");
}


// ============================================================
// MAIN LOOP
// ============================================================

void loop()
{
    if (Serial.available() > 0)
    {
        String command =
            Serial.readStringUntil('\n');

        command.trim();

        if (command.length() > 0)
        {
            processCommand(command);
        }
    }
}


// ============================================================
// COMMAND PROCESSING
// ============================================================

void processCommand(String command)
{
    // Convert command to uppercase
    // Makes commands like "off" and "OFF" both work
    command.toUpperCase();


    // --------------------------------------------------------
    // PING
    // --------------------------------------------------------

    if (command == "PING")
    {
        Serial.println("PONG");
        return;
    }


    // --------------------------------------------------------
    // OFF
    // --------------------------------------------------------

    if (command == "OFF")
    {
        motorOff();

        Serial.println("OK:OFF");
        return;
    }


    // --------------------------------------------------------
    // STATUS
    // --------------------------------------------------------

    if (command == "STATUS")
    {
        Serial.print("STATUS:M1=");
        Serial.println(currentIntensity);

        return;
    }


    // --------------------------------------------------------
    // MOTOR COMMAND
    //
    // Examples:
    //
    // M1:25
    // M1:50
    // M1:100
    //
    // --------------------------------------------------------

    if (command.startsWith("M1:"))
    {
        String intensityText =
            command.substring(3);

        int intensity =
            intensityText.toInt();

        intensity =
            constrain(intensity, 0, 100);

        setMotorIntensity(intensity);

        Serial.print("OK:M1:");
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
// MOTOR INTENSITY CONTROL
// ============================================================

void setMotorIntensity(int intensity)
{
    // Limit intensity to 0-100%
    intensity =
        constrain(intensity, 0, 100);

    currentIntensity = intensity;


    // --------------------------------------------------------
    // 0% = Motor OFF
    // --------------------------------------------------------

    if (intensity == 0)
    {
        motorOff();
        return;
    }


    // --------------------------------------------------------
    // 100% = Motor fully ON
    // --------------------------------------------------------

    if (intensity == 100)
    {
        motorFullOn();
        return;
    }


    /*
        ACTIVE-LOW MOTOR CONFIGURATION

        PCA9685 HIGH  -> Motor OFF
        PCA9685 LOW   -> Motor ON

        Therefore:

        0% intensity   = output mostly HIGH
        100% intensity = output mostly LOW

        PCA9685 PWM range:

        0 - 4095
    */

    int pwmValue =
        map(
            intensity,
            0,
            100,
            4095,
            0
        );


    pwm.setPWM(
        MOTOR_CHANNEL,
        0,
        pwmValue
    );
}


// ============================================================
// MOTOR OFF
// ============================================================

void motorOff()
{
    currentIntensity = 0;

    /*
        Force PCA9685 LED0 output HIGH.

        Active-low configuration:
        HIGH = motor OFF
    */

    pwm.setPWM(
        MOTOR_CHANNEL,
        4096,
        0
    );
}


// ============================================================
// MOTOR FULL ON
// ============================================================

void motorFullOn()
{
    currentIntensity = 100;

    /*
        Force PCA9685 LED0 output LOW.

        Active-low configuration:
        LOW = motor ON
    */

    pwm.setPWM(
        MOTOR_CHANNEL,
        0,
        4096
    );
}