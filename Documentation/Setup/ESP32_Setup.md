# Haptic3D ESP32 and Haptic Hardware Setup

**Last Updated:** September 23, 2026

## Purpose

This document describes the current Haptic3D embedded hardware,
ESP32-S3 firmware, PCA9685 configuration, motor control system, and
known hardware considerations.

---

## Hardware Overview

The current haptic hardware uses:

- Seeed Studio XIAO ESP32S3
- PCA9685 PWM controller
- Vibration motors
- Custom haptic glove / wearable hardware
- Windows experimenter computer

The communication path is:

Windows Experimenter Computer
    |
    | USB Serial
    v
XIAO ESP32S3
    |
    | I2C
    v
PCA9685
    |
    v
Motor Driver Circuitry
    |
    v
Vibration Motors

The ESP32 receives motor commands from the Python experimenter
application and controls the appropriate PCA9685 channels.

---

## Microcontroller

Current microcontroller:

Seeed Studio XIAO ESP32S3

Serial baud rate:

115200

The ESP32 connects to the experimenter computer through USB.

During current development, the device commonly appears as:

COM3

The COM port should always be verified before testing.

---

## I2C Configuration

The ESP32 communicates with the PCA9685 using I2C.

Current pins:

SDA: GPIO 5

SCL: GPIO 6

PCA9685 I2C Address:

0x40

Current PWM frequency:

1000 Hz

---

## PCA9685 Logic Power

The PCA9685 logic supply must be compatible with the ESP32-S3's
3.3 V I2C logic.

Current configuration:

PCA9685 VDD: 3.3 V

PCA9685 VSS: Ground

SDA/SCL pull-ups: 3.3 V

A common ground must be maintained between the ESP32, PCA9685, and
motor-control circuitry.

---

## Important Hardware Issue Identified During Development

An earlier PCB configuration connected the PCA9685 VDD incorrectly.

During troubleshooting:

- SDA measured approximately 3.3 V.
- SCL measured approximately 3.3 V.
- The XIAO 5 V supply measured approximately 5 V.
- PCA9685 VDD measured only approximately 0.06-0.07 V.
- The PCA9685 was not detected during I2C scanning.

The hardware was subsequently reworked so that the PCA9685 logic
supply is correctly powered from the 3.3 V logic domain.

If the PCA9685 is not detected in the future, verify VDD before
changing firmware.

---

## Motor Control Architecture

The vibration motors use active-low control.

The physical architecture uses a common positive motor supply, while
the PWM-controlled side determines motor activation/intensity.

Because the system is active-low:

Lower PWM output
    =
Stronger vibration

Higher PWM output
    =
Weaker vibration

The firmware converts the experiment's 0-100% intensity value into the
appropriate PCA9685 output.

---

## PWM Resolution

The PCA9685 uses 12-bit PWM resolution.

Normal PWM range:

0-4095

The firmware handles conversion between the experiment's percentage
intensity and the hardware PWM value.

Experimenter-facing motor intensity:

0-100%

---

## Motor OFF State

Motor shutdown must explicitly place the outputs into the configured
OFF state.

The current implementation uses PCA9685 full-off behavior rather than
relying only on a percentage value.

All motors should be disabled when:

- The system starts
- No trial is active
- A Visual Exploration trial is active
- The experiment is in Neutral
- A trial ends
- An emergency stop occurs
- Required communication is lost
- The experiment software shuts down

---

## Current Motor Mapping

The original three-finger glove mapping is:

| Logical Motor | Finger | PCA9685 Channel |
|---------------|--------|-----------------|
| M1 | Index | LED0 |
| M2 | Middle | LED2 |
| M3 | Ring | LED15 |

Pinky is not currently used.

The current Unity development configuration has only the Index
fingertip enabled while the final interaction method is reviewed.

The original motor mappings should remain documented so that the
multi-finger configuration can be restored if required.

---

## Current Interaction Configuration

Current Unity hand configuration:

Index:
Enabled

Middle:
Disabled

Ring:
Disabled

Pinky:
Disabled

This is a development configuration and does not require removing the
existing M2 or M3 firmware support.

---

## Serial Command Protocol

The ESP32 firmware accepts commands in the following general format:

M#:<intensity>

where:

# = logical motor number

and:

intensity = 0-100

Examples:

M1:80

M2:50

M3:100

---

## Additional Commands

The firmware also supports:

PING

Used to test communication.

Expected response:

PONG

---

STATUS

Used to request current hardware/firmware status.

---

OFF

Immediately disables all haptic motors.

---

## Communication Test

Before using the glove:

1. Connect the ESP32-S3 through USB.
2. Verify the correct COM port.
3. Open the connection at 115200 baud.
4. Send:

   PING

5. Verify:

   PONG

6. Send:

   OFF

7. Confirm that no motors are active.

Only after these checks should motor activation be tested.

---

## Individual Motor Test

Individual motors can be tested using commands such as:

M1:80

or:

M1:100

The motor should respond according to the requested intensity.

Testing should begin at controlled intensity levels before extended
operation.

---

## Python-to-ESP32 Communication

The Python experimenter application controls the physical ESP32 serial
connection.

The HoloLens does NOT directly connect to the ESP32 USB serial port.

The complete architecture is:

HoloLens / Unity
    |
    | Wi-Fi TCP
    v
Python Experimenter Application
    |
    | USB Serial
    v
ESP32-S3
    |
    | I2C
    v
PCA9685
    |
    v
Haptic Motors

---

## Unity Motor Representation

Unity maintains logical motor intensity information for the experiment.

During HoloLens operation, Unity does not need direct access to the
physical ESP32 serial connection.

Unity calculates the requested haptic intensity and communicates the
state to the Python experimenter application.

The Python application then sends the corresponding command to the
physical ESP32.

---

## Haptic Safety

Physical motor output should only be permitted when:

Trial Active = TRUE

and:

Condition = Haptic Only

If these conditions are not satisfied, motor output should remain OFF.

The experimenter application also contains communication-loss
protection to prevent stale motor commands from continuing when the
HoloLens connection is interrupted.

---

## Troubleshooting

### PCA9685 Not Detected

Check:

1. PCA9685 VDD is approximately 3.3 V.
2. VSS is connected to ground.
3. SDA is connected to GPIO 5.
4. SCL is connected to GPIO 6.
5. SDA/SCL pull-ups are connected to the 3.3 V logic domain.
6. ESP32 and PCA9685 share a common ground.
7. PCA9685 address is 0x40.

Do not assume an I2C scanning failure is a firmware problem until the
logic supply has been verified.

---

### ESP32 Does Not Respond to PING

Check:

1. Correct USB connection.
2. Correct COM port.
3. Baud rate is 115200.
4. Firmware is running.
5. No other application currently owns the serial port.
6. Restart/reconnect the ESP32 if necessary.

---

### Motor Does Not Activate

Check:

1. Correct logical motor command.
2. Correct PCA9685 channel mapping.
3. Motor wiring.
4. Motor power.
5. Driver circuitry.
6. Common ground.
7. Trial state if testing through the complete Haptic3D system.

---

### Motor Remains Active

Immediately send:

OFF

If necessary, disconnect motor power.

Then verify:

- Active-low logic
- PWM conversion
- Trial-state safeguards
- Serial communication
- Motor channel mapping

Do not resume participant testing until unintended activation has been
resolved.

---

## Interaction Methods Under Consideration

The hardware/software architecture currently retains M1, M2, and M3
support.

The final experiment may use:

- One Index motor
- Multiple motors on one hand
- Independent left/right Index motors

If the dual-hand approach is selected, the physical motor placement and
logical motor mapping should be updated in this document after hardware
testing.

---

## Known-Good Embedded Configuration

Current known-good baseline:

Microcontroller:
Seeed Studio XIAO ESP32S3

Serial Baud:
115200

I2C SDA:
GPIO 5

I2C SCL:
GPIO 6

PCA9685 Address:
0x40

PCA9685 VDD:
3.3 V

PWM Frequency:
1000 Hz

Motor Logic:
Active-Low

Original Mapping:
M1 = Index / LED0
M2 = Middle / LED2
M3 = Ring / LED15

This configuration should be treated as the baseline before making
hardware or firmware changes.