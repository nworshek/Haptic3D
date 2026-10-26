# Haptic3D Troubleshooting Guide

**Last Updated:** September 23, 2026

## Purpose

This document contains common problems encountered during development
of Haptic3D and the troubleshooting steps that have successfully been
used to resolve them.

The known-good system architecture is:

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

When troubleshooting, avoid changing several parts of the system at
once. Verify each stage of the communication pipeline individually.

---

# 1. HoloLens Cannot Connect to Python Application

## Symptoms

- HoloLens status does not update.
- Python application reports communication failure.
- Commands sent from the experimenter application do not receive a
  response.

## Check

1. Confirm Haptic3D is running on the HoloLens.
2. Confirm the HoloLens and experimenter computer have network
   connectivity.
3. Verify the current HoloLens IP address.
4. Confirm TCP port 5055 is being used.
5. Confirm the HoloLens TCP server is active.
6. Test whether the experimenter computer can reach port 5055 on the
   HoloLens.

Do not assume the HoloLens IP address remains the same between
sessions.

---

# 2. Repeated HoloLens Communication Timeouts

## Symptoms

The Python console repeatedly reports communication timeouts and may
send motor OFF commands even though direct communication appears to
work.

## Cause Encountered During Development

The original polling and watchdog values were too aggressive for the
HoloLens 1 Wi-Fi connection.

## Known-Good Settings

HAPTIC_POLL_MS = 100

HOLOLENS_WATCHDOG_SECONDS = 1.0

STATUS request timeout = 0.75 seconds

These settings produced stable communication during development
testing.

Avoid reducing these values unless additional testing demonstrates
that the connection remains reliable.

---

# 3. ESP32 Does Not Connect

## Symptoms

- Python application cannot open the serial connection.
- ESP32 status remains disconnected.
- PING does not return PONG.

## Check

1. Verify the ESP32 USB connection.
2. Open Windows Device Manager.
3. Identify the current COM port.
4. Confirm baud rate is 115200.
5. Confirm no other application is using the COM port.
6. Restart or reconnect the ESP32 if necessary.

During current development the ESP32 commonly appears as:

COM3

The COM port may change.

---

# 4. Serial WriteFile / Permission Error

## Example Symptom

A serial error may appear similar to:

WriteFile failed

or:

PermissionError(13)

## Check

1. Stop the experiment.
2. Ensure motors are OFF.
3. Disconnect the Python serial connection.
4. Close any other serial monitors or applications.
5. Disconnect and reconnect the ESP32 if necessary.
6. Verify the correct COM port.
7. Reconnect at 115200 baud.
8. Test using PING.
9. Verify PONG is returned.

Only one application should control the ESP32 serial port at a time.

---

# 5. PCA9685 Not Detected

## Symptoms

An I2C scan does not detect the PCA9685 at address 0x40.

## Hardware Issue Previously Encountered

During development:

- SDA measured approximately 3.3 V.
- SCL measured approximately 3.3 V.
- XIAO 5 V measured approximately 5 V.
- PCA9685 VDD measured approximately 0.06-0.07 V.
- No PCA9685 device was detected.

The problem was related to the PCA9685 logic supply rather than the
I2C firmware.

## Check

1. PCA9685 VDD is approximately 3.3 V.
2. PCA9685 VSS is connected to ground.
3. SDA is connected to GPIO 5.
4. SCL is connected to GPIO 6.
5. SDA/SCL pull-ups use the 3.3 V logic domain.
6. ESP32 and PCA9685 share a common ground.
7. PCA9685 address is 0x40.

Verify power before modifying firmware.

---

# 6. Motor Does Not Vibrate

## Check

1. Confirm ESP32 communication.
2. Send PING and verify PONG.
3. Verify the correct motor command.
4. Verify the logical motor mapping.
5. Check PCA9685 communication.
6. Check motor power.
7. Check driver circuitry.
8. Check physical motor wiring.

Original mapping:

M1 = Index = LED0

M2 = Middle = LED2

M3 = Ring = LED15

Remember that the current motor system uses active-low control.

---

# 7. Motor Vibrates Unexpectedly

Immediately stop haptic output.

Use:

OFF

or use the experimenter's Emergency Stop control.

If necessary, disconnect motor power.

Then verify:

- Current trial state
- Current experimental condition
- Motor command
- Active-low conversion
- Communication status
- Motor mapping
- ESP32 firmware state

Physical haptic output should only occur when:

Trial Active = TRUE

and:

Condition = Haptic Only

Do not resume participant testing until unintended motor activation has
been resolved.

---

# 8. Haptic Feedback Does Not Occur During Contact

Verify the complete chain:

Virtual Fingertip
    |
    v
HapticObject Contact
    |
    v
HapticFingertip
    |
    v
Unity Motor Intensity
    |
    v
TCP Status
    |
    v
Python Application
    |
    v
ESP32 Motor Command
    |
    v
Physical Motor

Check whether the problem exists in Unity contact detection,
network communication, serial communication, or physical hardware
before changing code.

---

# 9. Haptic Feedback Does Not Occur During Visual Trials

This is expected behavior.

Haptic output is intentionally disabled during:

- Visual Exploration
- Neutral
- Trial preparation
- Inactive trials

Physical motor output is intended only during an active Haptic Only
trial.

---

# 10. HoloLens Hand Tracking Marker Does Not Appear

## Check

1. Confirm the correct Haptic3D build is deployed.
2. Confirm HoloLens hand tracking is enabled in the current Unity
   configuration.
3. Place the hand within the HoloLens tracking region.
4. Use the HoloLens-compatible ready/open-hand position.
5. Move the hand slowly in front of the headset.

The white HandTrackingMarker should appear when the hand is recognized.

---

# 11. Hand Tracking Marker Appears and Disappears

## Behavior Observed During Development

Tracking is highly reliable when the hand is maintained in the native
HoloLens ready/open-hand position.

Tracking becomes inconsistent when the hand uses gestures that are not
normally recognized by the HoloLens 1 interaction system.

This behavior is a limitation of the current interaction approach and
does not necessarily indicate a software failure.

---

# 12. Hand Tracking Is Jittery

Raw HoloLens hand-position tracking displayed noticeable positional
jitter during initial testing.

Position smoothing was added to HoloLensHandTracker.

Current development value:

Position Smoothing = 12

This value reduced visible jitter while maintaining responsive
movement.

If smoothing is modified:

Higher values:
Faster response / less smoothing

Lower values:
More smoothing / potentially more lag

Use the current value of 12 as the known-good baseline.

---

# 13. Virtual Fingertip Appears Too Low

The HoloLens interaction-source position does not correspond directly
to the physical fingertip.

An initial hand offset was added during development.

Current approximate value:

Y Offset = +0.06 m

This moved the virtual fingertip proxy from approximately the
palm/back-of-hand region toward the physical finger position.

This value is preliminary and may require additional calibration.

---

# 14. Simulated Fingertips Do Not Follow HoloLens Hand

Check the HoloLensFingertipProxy component.

Verify:

Use Hand Tracking = Enabled

Verify that the component references:

- HoloLensHandTracker
- FingertipManager

The current single-finger development configuration has:

Index = Enabled

Middle = Disabled

Ring = Disabled

Pinky = Disabled

---

# 15. HoloLens Hand Moves Off Screen / Tracking Stops

The HoloLens 1 has a limited usable hand-tracking region.

Keep the hand within the region where the headset can recognize the
interaction-ready hand pose.

This limitation should be considered when positioning:

- Virtual stimuli
- Participant starting position
- Exploration area

The final experimental interaction method remains under review partly
because of this limitation.

---

# 16. Unity Changes Do Not Appear on HoloLens

After changing Unity content:

1. Save the scene.
2. Build the UWP project again from Unity.
3. Open the generated solution in Visual Studio 2019.
4. Select:

   Release | x86 | Device

5. Build Solution.
6. Deploy Solution.
7. Relaunch Haptic3D on the HoloLens.

Editing the Unity project does not automatically update the installed
HoloLens application.

---

# 17. Visual Studio Code Reports NuGet Restore Failure

A VS Code message such as:

Failed to restore NuGet packages for the solution

does not necessarily indicate that the HoloLens UWP build is broken.

The known-working HoloLens workflow uses:

Visual Studio 2019

for UWP Build and Deploy.

If Visual Studio 2019 successfully builds and deploys the generated UWP
solution, a VS Code C# tooling warning can be investigated separately.

---

# 18. HoloLens Application Builds but Does Not Update

Verify that the most recent Unity build was generated before building
the Visual Studio solution.

Recommended workflow:

Unity
    |
    v
Save Scene
    |
    v
Build UWP
    |
    v
Visual Studio 2019
    |
    v
Build Solution
    |
    v
Deploy Solution

If necessary, verify that Visual Studio is opening the current
generated UWP solution rather than an older build directory.

---

# 19. BlackBox or Stimulus Appears Incorrectly

Verify the current TrialManager state.

Expected behavior:

Visual Exploration:
- Stimulus visible
- BlackBox disabled
- Haptics disabled

Haptic Only:
- Stimulus available for contact
- Stimulus renderers hidden
- BlackBox enabled
- Haptics allowed during active trial

Neutral:
- No active experimental interaction
- Haptics disabled

---

# 20. Boundary Warning Problems

The boundary system is managed by TestBoundaryManager.

If warnings appear incorrectly:

1. Verify the participant/camera position.
2. Verify the configured test boundaries.
3. Verify BoundaryLeft, BoundaryRight, BoundaryFront, and BoundaryBack.
4. Verify the configured warning distance.
5. Verify the BoundaryWarning / WarningText references.

Current development warning distance:

0.20 m

---

# 21. General Recovery Procedure

If the cause of a problem is unclear:

1. Stop the current trial.
2. Turn all motors OFF.
3. Return Unity to Neutral.
4. Verify ESP32 communication using PING.
5. Verify HoloLens TCP communication.
6. Verify Unity status.
7. Verify the correct experimental condition.
8. Test each system independently.
9. Reconnect the full pipeline only after the individual components
   work.

Use the known-good architecture:

HoloLens / Unity
→ TCP
→ Python
→ USB Serial
→ ESP32
→ PCA9685
→ Motor

Avoid changing multiple systems simultaneously while troubleshooting.

---

# Known-Good Baseline

## Unity / HoloLens

Unity:
2019.4.41f2

HoloLens Build:
UWP

Architecture:
x86

Visual Studio:
2019

Deployment:
Release | x86 | Device

## Networking

TCP Port:
5055

Haptic Poll:
100 ms

HoloLens Watchdog:
1.0 s

STATUS Timeout:
0.75 s

## ESP32

Microcontroller:
Seeed Studio XIAO ESP32S3

Serial:
115200 baud

I2C SDA:
GPIO 5

I2C SCL:
GPIO 6

PCA9685:
0x40

PWM:
1000 Hz

Logic:
Active-Low

## Hand Tracking

Position Smoothing:
12

Initial Y Offset:
+0.06 m

Current Fingertip:
Index enabled

Middle / Ring / Pinky:
Disabled