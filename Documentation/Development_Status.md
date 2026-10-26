# Haptic3D Development Status

**Last Updated:** September 23, 2026

## Current Status

Haptic3D has reached a functional prototype stage.

The complete communication pathway between the Microsoft HoloLens 1,
Unity environment, Python experimenter application, ESP32-S3, and haptic
hardware has been established and tested.

The primary remaining design consideration is determining the most
appropriate hand/finger interaction method for participant testing.

---

## Completed System Components

### Unity Environment

The Unity environment currently includes:

- Flat virtual stimulus
- Ridges virtual stimulus
- Bumps virtual stimulus
- Visual Exploration condition
- Haptic Only condition
- Neutral experiment state
- Black-box visual occlusion
- Participant test boundaries
- Boundary warning system
- Trial management
- Experiment sequencing
- Data logging
- Haptic contact detection
- Variable haptic intensity
- Emergency motor shutdown

---

## Experiment Structure

The current experiment contains six trials:

1. Flat - Visual Exploration
2. Flat - Haptic Only
3. Ridges - Visual Exploration
4. Ridges - Haptic Only
5. Bumps - Visual Exploration
6. Bumps - Haptic Only

The current implementation uses a fixed trial order.

Trial states are controlled through the experimenter application.

---

## Communication Pipeline

The following communication pathway has been successfully tested:

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

Communication between all major components has been successfully
established.

---

## Python Experimenter Application

The Python experimenter application currently supports:

- Participant ID entry
- HoloLens connection
- ESP32 connection
- Trial preparation
- Trial start
- Trial end
- Trial repeat
- Emergency stop
- Current trial monitoring
- Experimental condition monitoring
- Stimulus monitoring
- Motor intensity monitoring
- Remote development controls

Current development communication settings:

- TCP Port: 5055
- ESP32 Baud Rate: 115200

During current development testing, the ESP32 commonly appears on COM3.

COM port and network addresses should be verified before each test session.

---

## Haptic Hardware

The current hardware system uses:

- Seeed Studio XIAO ESP32S3
- PCA9685 PWM controller
- Wearable vibration motors
- Custom haptic glove hardware

The system uses active-low motor control.

Motor intensity is represented in software using a 0-100 percent scale.

The existing hardware/software architecture supports independent motor
commands.

---

## Haptic Safety

Haptic output is restricted to active Haptic Only trials.

Motor output is disabled during:

- Visual Exploration
- Neutral state
- Trial preparation
- Trial completion
- Emergency stop
- Required communication loss
- Application shutdown

These safeguards are intended to prevent unintended motor activation
outside of the haptic portion of the experiment.

---

## HoloLens Hand Tracking

Initial HoloLens 1 hand tracking has been successfully integrated into
the Unity environment.

Current functionality includes:

- Detection of a HoloLens hand interaction source
- Handedness filtering
- Real-time tracked hand position
- Visual tracking marker
- Position smoothing
- Adjustable hand offset
- Connection to the simulated fingertip system

A position smoothing value of 12 is currently being used.

An initial vertical hand offset of approximately +0.06 m has been used
to better align the virtual fingertip system with the physical hand.

---

## Hand Tracking Test Results

Initial testing showed that the tracked hand position moves consistently
with the participant's physical hand.

Raw tracking initially displayed noticeable positional jitter.

Position smoothing was added and reduced the visible jitter while
maintaining acceptable responsiveness.

The primary limitation identified during testing is hand-pose
recognition.

Tracking is highly reliable when the hand is maintained in a native
HoloLens ready/open-hand position.

Tracking becomes inconsistent when the participant uses hand gestures
that are not normally recognized by the HoloLens interaction system.

---

## HoloLens 1 Limitation

HoloLens 1 does not provide the articulated fingertip tracking required
to independently determine the position of the Index, Middle, and Ring
fingers.

The current multi-finger implementation therefore estimates fingertip
positions relative to a single tracked hand reference.

This limitation is being considered before finalizing the participant
interaction method.

---

## Current Unity Hand Configuration

The original multi-finger system has been retained in the Unity scene.

Current development configuration:

- Index: Enabled
- Middle: Disabled
- Ring: Disabled
- Pinky: Disabled

The disabled fingertips have not been deleted.

This allows the multi-finger system to be restored if needed following
review of the experimental design.

---

## Interaction Approaches Under Consideration

### Option 1 - Single Hand / Single Index Finger

Track one hand and use one virtual index-finger contact point.

Potential benefits:

- Simplest tracking configuration
- Easier calibration
- Better alignment with HoloLens 1 capabilities
- Direct relationship between virtual contact and haptic output

---

### Option 2 - Single Hand / Multiple Fingers

Use one tracked hand reference to position multiple virtual fingertips.

Potential benefits:

- Preserves the original multi-finger haptic glove concept
- Allows multiple motors to provide simultaneous feedback

Current limitation:

The individual physical fingertips are not independently tracked by the
HoloLens 1.

---

### Option 3 - Dual Hand / Dual Index Finger

Track the left and right hands independently and assign one virtual
index-finger contact point to each hand.

Potential benefits:

- Two independently controlled interaction points
- Potential for independent left/right motor intensity
- Avoids estimating multiple fingers from one hand reference
- Retains multi-point haptic interaction while using a simpler tracking
  model

This approach still requires additional implementation and testing.

---

## Current Design Decision

No final interaction method has been selected.

The single-index, multi-finger, and dual-index approaches will be
reviewed before major changes are made to the current Unity and hardware
architecture.

Until that decision is made, the working multi-finger components will
remain in the project but disabled when they are not required.

---

## Next Development Steps

Following review of the interaction approach:

1. Select the final hand/finger interaction method.
2. Configure the appropriate virtual fingertip objects.
3. Finalize physical motor placement and motor mapping.
4. Calibrate virtual contact point position.
5. Validate tracking throughout the expected exploration area.
6. Validate independent haptic intensity control.
7. Perform complete system testing.
8. Update the experimental procedure.
9. Conduct internal pilot testing before participant testing.

---

## Known-Good System State

As of September 23, 2026, the following pipeline has been demonstrated:

Unity stimulus interaction
→ HoloLens experiment application
→ Wi-Fi TCP communication
→ Python experimenter application
→ USB serial communication
→ ESP32-S3
→ Haptic motor activation

HoloLens hand-position tracking has also been demonstrated and connected
to the existing Unity fingertip system.

Major architectural changes should be tested against this known-good
configuration to ensure that existing functionality is preserved.