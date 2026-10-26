# Haptic3D Experimental Procedure

**Last Updated:** September 23, 2026

## Purpose

This document describes the current experimental procedure for the
Haptic3D mixed-reality haptic interaction study.

The experiment uses Microsoft HoloLens 1, virtual surface stimuli, and
wearable vibrotactile feedback to investigate interaction with virtual
surface characteristics.

The final hand/finger interaction method is currently under review and
will be updated following discussion of the experimental design.

---

## Experimental System

The participant interacts with virtual stimuli presented through the
Microsoft HoloLens 1.

During haptic trials, virtual contact information is processed by Unity
and transmitted to the experimenter computer.

The system follows the communication pathway:

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
Wearable Vibration Motor(s)

---

## Virtual Stimuli

Three virtual surface types are currently included:

- Flat
- Ridges
- Bumps

Each stimulus is based on an approximately 0.057 m cube.

The Ridges and Bumps stimuli contain geometric surface features designed
to produce different virtual contact and haptic responses.

---

## Experimental Conditions

Two primary experimental conditions are currently used.

### Visual Exploration

The participant can visually inspect the virtual stimulus.

Haptic motor output is disabled during this condition.

### Haptic Only

The virtual stimulus is visually occluded using the BlackBox system.

The participant explores the virtual surface using the designated
hand/finger interaction method.

Haptic feedback is enabled during valid contact with the virtual
stimulus.

---

## Trial Structure

The current experiment contains six trials:

| Trial | Stimulus | Condition |
|------:|----------|-----------|
| 1 | Flat | Visual Exploration |
| 2 | Flat | Haptic Only |
| 3 | Ridges | Visual Exploration |
| 4 | Ridges | Haptic Only |
| 5 | Bumps | Visual Exploration |
| 6 | Bumps | Haptic Only |

The current implementation uses a fixed trial order.

---

## Pre-Experiment Setup

Before beginning a participant session, the experimenter should:

1. Start the Windows experimenter computer.
2. Connect the ESP32-S3 through USB.
3. Verify the correct serial COM port.
4. Start the Python experimenter application.
5. Verify communication with the ESP32.
6. Power on the HoloLens.
7. Connect the HoloLens and experimenter computer to the required
   network.
8. Launch the Haptic3D application on the HoloLens.
9. Verify communication between the HoloLens and Python application.
10. Verify that all motors are initially OFF.
11. Verify that the correct virtual stimuli are available.
12. Confirm that the experiment environment and participant area are
    clear.

---

## Participant Setup

The participant should be introduced to the experimental equipment
before beginning the trials.

Setup currently includes:

1. Explain the general purpose of the Haptic3D system.
2. Fit the participant with the HoloLens.
3. Adjust the HoloLens for a clear and comfortable view.
4. Fit the participant with the required haptic hardware.
5. Position the participant at the designated starting location.
6. Explain the experiment boundaries.
7. Explain how the participant should interact with the virtual
   stimulus.
8. Allow the participant to become familiar with the interaction method
   before experimental trials begin.

The exact hand/finger instructions are currently pending final selection
of the interaction method.

---

## Hand/Finger Interaction

**STATUS: UNDER REVIEW**

The final participant hand configuration has not yet been selected.

Current approaches under consideration include:

### Single Index Finger

The participant uses one designated index finger to explore the virtual
surface.

One tracked hand reference controls one virtual contact point.

### Multiple Fingers

The participant uses multiple fingers to explore the virtual surface.

Multiple virtual fingertip positions are estimated relative to one
HoloLens hand-tracking reference.

### Left and Right Index Fingers

The participant uses one index finger from each hand.

The left and right hands would be tracked independently and could
control separate haptic motors.

The final instructions in this section should be updated after the
interaction method is selected.

---

## Trial Preparation

Before each trial, the experimenter:

1. Confirms the participant is ready.
2. Selects/prepares the next trial using the Python experimenter
   application.
3. Verifies the displayed stimulus and experimental condition.
4. Confirms that haptic motors remain OFF before the trial starts.
5. Instructs the participant to prepare for the trial.

The experimenter then starts the trial from the experimenter
application.

---

## Visual Exploration Trial

During a Visual Exploration trial:

1. The virtual stimulus is visible.
2. The participant observes the stimulus.
3. Haptic output remains disabled.
4. The participant follows the visual exploration instructions defined
   by the experimental protocol.
5. The experimenter ends the trial when the trial procedure is
   complete.

The system then returns to the Neutral state.

---

## Haptic Only Trial

During a Haptic Only trial:

1. The virtual stimulus is visually occluded.
2. The participant uses the designated hand/finger interaction method
   to explore the virtual stimulus.
3. Unity detects virtual contact.
4. Unity determines the appropriate haptic intensity.
5. Haptic information is transmitted to the experimenter computer.
6. The experimenter application sends the corresponding motor command
   to the ESP32.
7. The participant receives vibrotactile feedback.
8. Interaction data is recorded.
9. The experimenter ends the trial when the trial procedure is
   complete.

The system then returns to the Neutral state and motor output is
disabled.

---

## Between Trials

Between trials:

1. The system remains in the Neutral state.
2. Haptic motors remain OFF.
3. The experimenter verifies the next stimulus and condition.
4. The participant may reposition if necessary.
5. The next trial is prepared only when the participant and
   experimenter are ready.

---

## Data Collection

The system currently records experimental interaction information to
CSV.

Recorded information includes:

- Session ID
- Participant ID
- Trial number
- Time
- Elapsed trial time
- Condition
- Stimulus
- Event
- Finger
- Motor
- Motor intensity

Additional questionnaire or participant-response data should be
recorded according to the approved experimental protocol.

---

## Experiment Completion

After the final trial:

1. End the active trial.
2. Confirm that all haptic motors are OFF.
3. Return the system to Neutral.
4. Remove the haptic hardware from the participant.
5. Remove the HoloLens.
6. Complete any required post-study questionnaire or assessment.
7. Save and verify the participant's experimental data.
8. Prepare the system for the next participant or shut down the
   equipment.

---

## Emergency Stop

If unexpected behavior occurs during a trial, the experimenter should
use the Emergency Stop control.

The emergency stop should:

- Stop haptic output.
- Set motor intensity to zero.
- Stop the current interaction.
- Prevent unintended continued vibration.

Testing should not resume until the cause of the issue has been
identified.

---

## Current Procedure Limitation

The primary unresolved component of the experimental procedure is the
participant hand/finger interaction method.

HoloLens 1 reliably tracks the hand while it is maintained in a
compatible ready/open-hand pose but does not provide articulated
tracking of individual fingertips.

The final interaction method must therefore balance:

- Tracking reliability
- Natural participant movement
- Virtual-to-physical contact correspondence
- Haptic motor placement
- Experimental repeatability

The experimental procedure should be updated once this design decision
has been finalized.