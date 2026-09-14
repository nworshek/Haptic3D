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

// Motor channel mapping
#define INDEX_CHANNEL 0
#define MIDDLE_CHANNEL 2
#define RING_CHANNEL 15

// PCA9685 PWM frequency
#define PWM_FREQUENCY 1000

// Serial communication baud rate
#define SERIAL_BAUD 115200


// ============================================================
// PCA9685 OBJECT
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

    delay(1000);

    Serial.println();
    Serial.println(
        "ESP32S3 Haptic Controller Starting..."
    );


    // --------------------------------------------------------
    // Start I2C
    // --------------------------------------------------------

    Wire.begin(
        SDA_PIN,
        SCL_PIN
    );


    // --------------------------------------------------------
    // Start PCA9685
    // --------------------------------------------------------

    pwm.begin();

    pwm.setPWMFreq(
        PWM_FREQUENCY
    );

    delay(10);


    // --------------------------------------------------------
    // Start with all motors OFF
    // --------------------------------------------------------

    allMotorsOff();

    Serial.println(
        "M1 = Index  -> LED0"
    );

    Serial.println(
        "M2 = Middle -> LED2"
    );

    Serial.println(
        "M3 = Ring   -> LED15"
    );

    Serial.println(
        "READY"
    );
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
            processCommand(
                command
            );
        }
    }
}


// ============================================================
// COMMAND PROCESSING
// ============================================================

void processCommand(
    String command
)
{
    command.toUpperCase();


    // --------------------------------------------------------
    // PING
    // --------------------------------------------------------

    if (command == "PING")
    {
        Serial.println(
            "PONG"
        );

        return;
    }


    // --------------------------------------------------------
    // ALL MOTORS OFF
    // --------------------------------------------------------

    if (command == "OFF")
    {
        allMotorsOff();

        Serial.println(
            "OK:OFF"
        );

        return;
    }


    // --------------------------------------------------------
    // STATUS
    // --------------------------------------------------------

    if (command == "STATUS")
    {
        Serial.print(
            "STATUS:M1="
        );

        Serial.print(
            indexIntensity
        );

        Serial.print(
            ",M2="
        );

        Serial.print(
            middleIntensity
        );

        Serial.print(
            ",M3="
        );

        Serial.println(
            ringIntensity
        );

        return;
    }


    // --------------------------------------------------------
    // MOTOR 1 - INDEX
    // --------------------------------------------------------

    if (command.startsWith("M1:"))
    {
        int intensity =
            command.substring(3)
            .toInt();

        intensity =
            constrain(
                intensity,
                0,
                100
            );

        setMotorIntensity(
            INDEX_CHANNEL,
            intensity
        );

        indexIntensity =
            intensity;

        Serial.print(
            "OK:M1:"
        );

        Serial.println(
            intensity
        );

        return;
    }


    // --------------------------------------------------------
    // MOTOR 2 - MIDDLE
    // --------------------------------------------------------

    if (command.startsWith("M2:"))
    {
        int intensity =
            command.substring(3)
            .toInt();

        intensity =
            constrain(
                intensity,
                0,
                100
            );

        setMotorIntensity(
            MIDDLE_CHANNEL,
            intensity
        );

        middleIntensity =
            intensity;

        Serial.print(
            "OK:M2:"
        );

        Serial.println(
            intensity
        );

        return;
    }


    // --------------------------------------------------------
    // MOTOR 3 - RING
    // --------------------------------------------------------

    if (command.startsWith("M3:"))
    {
        int intensity =
            command.substring(3)
            .toInt();

        intensity =
            constrain(
                intensity,
                0,
                100
            );

        setMotorIntensity(
            RING_CHANNEL,
            intensity
        );

        ringIntensity =
            intensity;

        Serial.print(
            "OK:M3:"
        );

        Serial.println(
            intensity
        );

        return;
    }


    // --------------------------------------------------------
    // UNKNOWN COMMAND
    // --------------------------------------------------------

    Serial.print(
        "ERROR:UNKNOWN_COMMAND:"
    );

    Serial.println(
        command
    );
}


// ============================================================
// MOTOR INTENSITY CONTROL
// ============================================================

void setMotorIntensity(
    int channel,
    int intensity
)
{
    intensity =
        constrain(
            intensity,
            0,
            100
        );


    // --------------------------------------------------------
    // 0% = OFF
    // --------------------------------------------------------

    if (intensity == 0)
    {
        motorOff(
            channel
        );

        return;
    }


    // --------------------------------------------------------
    // 100% = FULL ON
    // --------------------------------------------------------

    if (intensity == 100)
    {
        motorFullOn(
            channel
        );

        return;
    }


    /*
        ACTIVE-LOW CONFIGURATION

        HIGH = motor OFF
        LOW  = motor ON

        0% intensity   = mostly HIGH
        100% intensity = mostly LOW
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
        channel,
        0,
        pwmValue
    );
}


// ============================================================
// TURN OFF ONE MOTOR
// ============================================================

void motorOff(
    int channel
)
{
    /*
        Force output HIGH.

        Active-low:
        HIGH = motor OFF
    */

    pwm.setPWM(
        channel,
        4096,
        0
    );
}


// ============================================================
// FULL ON ONE MOTOR
// ============================================================

void motorFullOn(
    int channel
)
{
    /*
        Force output LOW.

        Active-low:
        LOW = motor ON
    */

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

    motorOff(
        INDEX_CHANNEL
    );

    motorOff(
        MIDDLE_CHANNEL
    );

    motorOff(
        RING_CHANNEL
    );
}