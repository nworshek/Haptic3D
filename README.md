# Haptic3D

Haptic3D is a Unity-based mixed-reality haptic interaction system designed to
investigate how virtual surface characteristics can be experienced through
wearable vibrotactile feedback.

The system integrates Microsoft HoloLens, Unity, a Python experimenter
application, an ESP32-S3 microcontroller, a PCA9685 motor controller, and
wearable vibration motors.

The project currently supports virtual surface exploration, real-time haptic
feedback, experiment control, participant trial management, and data logging.

---

## System Overview

The current system architecture is:

HoloLens
    ↓
Unity / Haptic3D
    ↓ Wi-Fi TCP
Python Experimenter Application
    ↓ USB Serial
ESP32-S3
    ↓
PCA9685
    ↓
Haptic Glove / Vibration Motors

Unity performs virtual environment rendering, hand/contact detection, surface
interaction, trial management, and haptic intensity calculation.

The Python experimenter application communicates with the HoloLens over Wi-Fi
and with the ESP32-S3 over USB serial.

---

## Repository Structure

### `Assets/`
Main Unity project assets.

Contains:
- Unity scenes
- C# scripts
- 3D models
- Materials
- XR configuration

### `ExperimenterApp/`
Python-based experimenter interface used to control trials and communicate
between the HoloLens and ESP32.

### `Firmware/`
Firmware for the Seeed Studio XIAO ESP32S3 and haptic motor controller.

### `Hardware/`
Hardware-related project files and documentation.

### `SourceModels/`
Original/source 3D models used to create the experimental stimuli.

### `Documentation/`
Detailed setup, experiment, hardware, networking, and development
documentation.

---

## Hardware

Current hardware includes:

- Microsoft HoloLens 1
- Seeed Studio XIAO ESP32S3
- PCA9685 PWM controller
- Wearable vibration motors
- Custom haptic glove hardware
- Windows experimenter computer

The ESP32-S3 communicates with the experimenter computer through USB serial.

---

## Software

Primary software:

- Unity 2019.4.41f2
- Visual Studio 2019
- Python 3
- Arduino / ESP32 firmware environment
- Microsoft HoloLens UWP deployment tools

The HoloLens application is built from Unity as a UWP project and deployed
through Visual Studio.

---

## Virtual Haptic Surfaces

The current experiment contains three virtual surface stimuli:

- Flat
- Ridges
- Bumps

The stimuli are approximately 0.057 m cubes.

Each object contains haptic properties used by Unity to determine the feedback
generated during virtual contact.

---

## Experimental Conditions

The current experimental framework supports:

### Visual Exploration
The participant can visually inspect the virtual stimulus.

### Haptic Only
The stimulus is visually occluded while the participant explores the virtual
surface using haptic feedback.

### Neutral
No active stimulus interaction or haptic output.

The current trial structure contains six trials:

1. Flat — Visual Exploration
2. Flat — Haptic Only
3. Ridges — Visual Exploration
4. Ridges — Haptic Only
5. Bumps — Visual Exploration
6. Bumps — Haptic Only

---

## Haptic Feedback

Unity detects interaction between the virtual fingertip/contact point and the
haptic stimulus.

The surface interaction is used to calculate vibration intensity.

Motor commands follow the system path:

Unity → TCP → Python Experimenter App → USB Serial → ESP32 → Haptic Motor

Haptic output is restricted to active Haptic Only trials.

---

## HoloLens Hand Tracking

Haptic3D currently contains an experimental HoloLens 1 hand-tracking system.

The system uses the HoloLens interaction-source hand position to control a
virtual fingertip proxy.

Current features include:

- Right-hand detection
- Real-time hand-position tracking
- Position smoothing
- Adjustable hand-to-fingertip offset
- Visual tracking marker for debugging
- Connection to the existing simulated fingertip system

The current smoothing value used during development is:

Position Smoothing: 12

An initial vertical calibration offset of approximately:

Y: +0.06 m

has been used during testing.

---

## Hand Tracking Limitations

Microsoft HoloLens 1 does not provide articulated tracking of individual
fingertip joints.

The current implementation therefore uses the HoloLens hand interaction
position as a reference and estimates the location of a virtual fingertip.

Testing has shown that tracking is highly reliable when the user's hand is in
a HoloLens-compatible ready/open-hand position.

Tracking becomes less reliable when the hand is placed into gestures that are
not normally recognized by the HoloLens interaction system.

Because of this limitation, the final participant interaction method is still
under evaluation.

---

## Interaction Methods Under Evaluation

Three approaches are currently being considered:

### Multi-Finger Interaction

One tracked hand reference controls multiple estimated fingertip positions.

Advantages:
- Supports the original multi-finger glove concept
- Allows multiple haptic motors

Limitation:
- Individual fingertips are not independently tracked by HoloLens 1

### Single-Finger Interaction

One tracked hand controls one virtual index-finger contact point.

Advantages:
- Simpler calibration
- Better alignment with HoloLens 1 tracking capabilities
- Simpler relationship between virtual contact and physical vibration

### Dual-Hand / Dual-Index Interaction

Left and right hands would be tracked independently.

Each hand would control one virtual index-finger contact point and one
independent vibration motor.

This approach is currently under consideration and has not yet been fully
implemented or validated.

---

## Current Development Configuration

The current Unity scene retains the original multi-finger architecture, but
only the Index fingertip is currently enabled for hand-tracking development.

Current configuration:

- Index — Enabled
- Middle — Disabled
- Ring — Disabled
- Pinky — Disabled

The disabled fingertip objects have been retained so that the multi-finger
configuration can be restored if needed.

---

## Experiment Control

The experiment is controlled from a Python-based experimenter application.

The application supports:

- Participant identification
- Trial preparation
- Trial start
- Trial end
- Trial repeat
- Emergency stop
- HoloLens communication status
- ESP32 connection status
- Motor intensity monitoring
- Remote development controls

---

## Networking

The HoloLens and experimenter computer communicate using TCP over Wi-Fi.

Current development port:

5055

The experimenter computer communicates with the ESP32-S3 separately through
USB serial at:

115200 baud

Network addresses may change depending on the network configuration and should
not be assumed to remain constant between systems or sessions.

---

## Data Logging

The system records experimental interaction data to CSV.

Logged information includes:

- Session ID
- Participant ID
- Trial number
- Timestamp
- Elapsed trial time
- Experimental condition
- Stimulus
- Event
- Finger
- Motor
- Motor intensity

---

## Safety Features

Several software safeguards are implemented.

Haptic feedback is only permitted when:

- A trial is active
- The trial condition is Haptic Only

Additional safeguards include:

- Motor shutdown at trial end
- Emergency motor shutdown
- Motor shutdown when required communication is lost
- Motor shutdown when the application exits
- Experiment-state validation

---

## Development and Debug Tools

The project retains several tools used during development.

These include:

- Simulated fingertip keyboard control
- Remote fingertip movement
- Haptic keyboard testing
- MockHMD testing
- Visual hand-tracking marker
- Experimenter Unity UI

Some development tools are disabled during normal HoloLens testing but are
retained for troubleshooting and future development.

---

## Current Project Status

As of September 2026, the primary Haptic3D communication pipeline has been
successfully demonstrated:

HoloLens / Unity
→ Wi-Fi TCP
→ Python Experimenter Application
→ USB Serial
→ ESP32-S3
→ Haptic Hardware

Virtual stimulus selection, trial control, haptic intensity communication,
motor activation, data logging, and HoloLens hand-position tracking have been
tested during development.

The primary current design decision is determining the most appropriate
participant hand-interaction method given the hand-tracking capabilities of
Microsoft HoloLens 1.

No major changes to the interaction architecture will be made until the
single-finger, multi-finger, and potential dual-hand approaches are reviewed.

---

## Documentation

Additional documentation is located in:

`Documentation/`

Detailed setup, experiment procedure, hardware, networking, troubleshooting,
and development documentation can be maintained separately from this README.