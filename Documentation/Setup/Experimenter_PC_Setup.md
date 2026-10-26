# Haptic3D Experimenter PC Setup

**Last Updated:** September 23, 2026

## Purpose

This document describes the setup and operation of the Windows
experimenter computer used by Haptic3D.

The experimenter computer acts as the communication bridge between:

- Microsoft HoloLens 1 / Unity
- Python Experimenter Application
- ESP32-S3
- Haptic hardware

---

## System Architecture

The experimenter computer sits between the HoloLens and physical
haptic hardware.

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
    v
PCA9685
    |
    v
Haptic Motors

The HoloLens does not directly access the ESP32 USB serial connection.

---

## Software

The current development computer uses:

- Windows
- Python 3
- Haptic3D Python Experimenter Application
- USB serial communication
- TCP network communication

The current development environment has been tested with Python 3.13.

---

## Experimenter Application

The experimenter application is located in:

`ExperimenterApp/`

The application provides controls for:

- Participant identification
- HoloLens connection
- ESP32 connection
- Trial preparation
- Trial start
- Trial end
- Trial repeat
- Emergency stop
- Trial status monitoring
- Stimulus monitoring
- Haptic intensity monitoring
- Development/debug movement controls

---

## ESP32 Connection

The ESP32-S3 connects to the experimenter computer through USB.

Current serial configuration:

Port:
COM3

Baud Rate:
115200

The COM port may change depending on the computer or USB connection.

Always verify the assigned COM port before running an experiment.

---

## ESP32 Connection Test

The firmware supports serial commands including:

- PING
- STATUS
- OFF
- M1:<intensity>
- M2:<intensity>
- M3:<intensity>

A successful PING should return:

PONG

Before beginning participant testing, verify that the experimenter
application can communicate with the ESP32.

---

## HoloLens Connection

The Python application communicates with the HoloLens over TCP.

Current TCP configuration:

Port:
5055

The HoloLens IP address should be checked before each testing session.

During one development configuration, the HoloLens used:

192.168.137.214

This address should NOT be assumed to remain permanent.

---

## Development Hotspot Configuration

During development, the Windows experimenter computer has been used as
a mobile hotspot for communication with the HoloLens.

The development configuration used:

Frequency:
2.4 GHz

Windows hotspot interface address:
192.168.137.1

HoloLens development address:
192.168.137.214

These addresses may change and should be verified rather than
hard-coded as permanent system configuration.

---

## Network Test

Before launching a full experiment, verify that the experimenter
computer can reach the HoloLens TCP server.

The Haptic3D TCP server uses:

Port 5055

A successful network test confirms that the experimenter computer can
reach the HoloLens application.

---

## Known-Good Communication Timing

Initial development testing produced intermittent HoloLens
communication timeouts because the Python polling and watchdog timing
were too aggressive for the HoloLens 1 Wi-Fi connection.

The current known-good values are:

HAPTIC_POLL_MS = 100

HOLOLENS_WATCHDOG_SECONDS = 1.0

STATUS request timeout = 0.75 seconds

These values successfully stabilized communication during development
testing.

Avoid reducing these values without additional testing.

---

## HoloLens Status Format

The HoloLens TCP server currently provides status information using the
following general format:

STATUS
| Trial
| Total Trials
| Condition
| Stimulus
| Trial Status
| ESP Status
| Maximum Intensity
| M1
| M2
| M3

This allows the Python experimenter application to monitor the current
Unity experiment state and calculated motor intensities.

---

## Experiment Commands

The current experimenter communication system supports commands
including:

PARTICIPANT:<ID>

PREPARE

START

END

REPEAT

EMERGENCY

Additional remote movement commands are retained for development and
debug testing.

---

## Trial Communication

During an experiment:

1. The experimenter enters the Participant ID.
2. The Python application sends the participant information to Unity.
3. PREPARE configures the next trial.
4. START begins the trial.
5. Unity processes interaction and haptic intensity.
6. The Python application reads the current motor intensity state.
7. The appropriate commands are sent to the ESP32.
8. END completes the trial and disables haptic output.

---

## Motor Communication

Motor intensity is represented using a software scale of:

0-100%

The Python application sends the appropriate motor commands to the
ESP32 only when necessary.

Example:

M1:80

This requests an intensity of 80% for Motor 1.

The ESP32 firmware converts the requested intensity into the
appropriate PCA9685 output.

---

## Communication Watchdog

The experimenter application contains a communication watchdog.

If communication with the HoloLens is lost for longer than the
configured watchdog period, the system disables haptic motor output.

Current watchdog threshold:

1.0 second

The watchdog prevents stale HoloLens motor-intensity information from
continuing to drive the physical haptic hardware.

---

## Motor Shutdown Behavior

The experimenter application can send:

OFF

to disable all motors.

Motor output should be disabled:

- Before an experiment begins
- Outside Haptic Only trials
- At trial completion
- During an emergency stop
- Following required communication loss
- When shutting down the experiment

---

## Haptic Safety Logic

Physical haptic output should only occur when Unity reports:

Condition:
Haptic Only

and:

Trial Status:
ACTIVE

If either condition is not satisfied, physical motor output should
remain OFF.

---

## Startup Procedure

Use the following order when preparing Haptic3D:

1. Start the experimenter computer.
2. Connect the ESP32-S3 through USB.
3. Verify the ESP32 COM port.
4. Start the Python experimenter application.
5. Connect to the ESP32.
6. Verify successful ESP32 communication.
7. Start/connect the HoloLens.
8. Launch Haptic3D on the HoloLens.
9. Verify the current HoloLens IP address.
10. Configure the Python application with the correct HoloLens IP if
    necessary.
11. Connect the Python application to HoloLens.
12. Verify that HoloLens status information is updating.
13. Confirm ESP32 status is connected in the Python application.
14. Confirm all motors are OFF.
15. Begin experiment preparation.

---

## Pre-Experiment Check

Before starting Trial 1, verify:

- HoloLens application is running
- HoloLens TCP communication is stable
- ESP32 is connected
- Correct COM port is selected
- Motor output is OFF
- Participant ID is entered
- Trial status is not ACTIVE
- Correct virtual environment is loaded
- Haptic hardware is properly connected

---

## Development Controls

The experimenter application currently retains remote movement controls
used during development.

These allow the simulated fingertip system to be moved remotely through
the HoloLens TCP connection.

These controls are intended for:

- Debugging
- Calibration
- Contact testing
- Development without physical hand tracking

They are not intended to define the final participant interaction
procedure.

---

## Troubleshooting Communication

### HoloLens Does Not Connect

Check:

1. Haptic3D is running on the HoloLens.
2. HoloLens and PC have network connectivity.
3. The current HoloLens IP address is correct.
4. TCP port 5055 is being used.
5. The HoloLens TCP server is running.

---

### Repeated HoloLens Communication Timeouts

Confirm the known-good timing values:

HAPTIC_POLL_MS = 100

HOLOLENS_WATCHDOG_SECONDS = 1.0

STATUS request timeout = 0.75 seconds

Earlier, more aggressive polling caused unnecessary communication
timeouts even though direct STATUS requests worked correctly.

---

### ESP32 Does Not Connect

Check:

1. USB cable connection.
2. Windows Device Manager.
3. Assigned COM port.
4. Baud rate is 115200.
5. No other program currently owns the COM port.
6. ESP32 firmware is running.

---

### Serial Write Errors

If serial write errors occur:

1. Stop the experiment.
2. Ensure motors are OFF.
3. Disconnect the Python serial connection.
4. Verify the ESP32 is still visible in Windows.
5. Reconnect the ESP32.
6. Reopen the serial connection.
7. Test with PING before resuming.

Only one application should control the ESP32 serial port at a time.

---

## Known-Good State

The following communication chain has been successfully demonstrated:

HoloLens
→ Unity Haptic3D
→ Wi-Fi TCP
→ Python Experimenter Application
→ USB COM3
→ ESP32-S3
→ Haptic Motors

Stable communication was achieved using:

TCP Port: 5055

Serial Baud: 115200

Haptic Poll Interval: 100 ms

HoloLens Watchdog: 1.0 s

STATUS Timeout: 0.75 s

These settings should be treated as the baseline before modifying the
communication system.