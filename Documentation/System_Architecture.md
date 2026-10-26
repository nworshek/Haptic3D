# Haptic3D System Architecture

## Overview

Haptic3D is a mixed-reality haptic interaction system that connects a
Microsoft HoloLens 1 virtual environment to wearable vibrotactile hardware.

The current system consists of five primary components:

1. Microsoft HoloLens 1
2. Unity Haptic3D application
3. Python experimenter application
4. Seeed Studio XIAO ESP32S3
5. Haptic glove hardware

## System Pipeline

HoloLens 1
    |
    | Hand Tracking / Mixed Reality
    v
Unity Haptic3D
    |
    | Wi-Fi TCP
    v
Python Experimenter Application
    |
    | USB Serial
    v
XIAO ESP32S3
    |
    | PWM Control
    v
PCA9685
    |
    v
Vibration Motors

## HoloLens 1

The HoloLens provides:

- Mixed-reality visualization
- Head tracking
- Hand interaction-source tracking
- Participant view of virtual stimuli

The HoloLens runs the Unity Haptic3D application as a UWP application.

## Unity Haptic3D

Unity is responsible for:

- Rendering the virtual environment
- Presenting virtual haptic stimuli
- Managing experiment states
- Detecting virtual contact
- Calculating haptic intensity
- Tracking HoloLens hand position
- Logging interaction events
- Communicating experiment state over TCP

Unity version:

Unity 2019.4.41f2

## Experimenter Application

The experimenter application runs on the Windows experimenter computer.

It provides:

- Participant ID entry
- Trial preparation
- Trial start/end control
- Trial repeat control
- Emergency stop
- HoloLens connection monitoring
- ESP32 connection monitoring
- Motor intensity monitoring
- Communication between Unity and the ESP32

The application is written in Python.

## Network Communication

The HoloLens and experimenter computer communicate over Wi-Fi using TCP.

Development TCP port:

5055

The IP address of the HoloLens may change depending on the network
configuration and should be verified before running the experiment.

## ESP32 Communication

The experimenter computer communicates with the XIAO ESP32S3 through USB
serial.

Serial configuration:

Baud Rate: 115200

During development, the ESP32 commonly appears as:

COM3

The COM port should be verified before each experiment because Windows may
assign a different port.

## Motor Control

The ESP32 controls vibration motors through a PCA9685 PWM controller.

The hardware uses active-low motor control:

- Lower PWM output corresponds to stronger vibration.
- Higher PWM output corresponds to weaker vibration.
- Motors are explicitly disabled when haptic feedback is not permitted.

The software exposes motor intensity to the experiment system using a
0-100 percent scale.

## Haptic Safety

Haptic output is only allowed when:

1. A trial is active.
2. The current trial condition is Haptic Only.

Motor output is disabled when:

- A trial ends.
- An emergency stop occurs.
- Required communication is lost.
- The experiment application shuts down.

## Current Hand Tracking Architecture

HoloLens 1 hand tracking currently provides a hand interaction-source
position rather than articulated individual fingertip positions.

The current pipeline is:

HoloLens Hand Position
    |
    v
HoloLensHandTracker
    |
    | Position Smoothing
    v
HoloLensFingertipProxy
    |
    v
FingertipManager
    |
    v
Virtual Fingertip Contact
    |
    v
Haptic Intensity Calculation

A visual tracking marker can be enabled for development and calibration.

Current development settings include:

Position Smoothing: 12

Initial Y Offset: +0.06 m

These values may be adjusted following additional calibration.

## Current Interaction Configuration

The Unity project retains support for multiple simulated fingertips.

Current development configuration:

- Index: Enabled
- Middle: Disabled
- Ring: Disabled
- Pinky: Disabled

The Middle, Ring, and Pinky objects have been retained so that alternative
interaction methods can be evaluated without rebuilding the original
multi-finger system.

## Interaction Approach Under Review

The final participant interaction method has not yet been selected.

Current approaches under consideration include:

- Single-hand / single-index interaction
- Multi-finger interaction using estimated fingertip positions
- Dual-hand interaction using independently tracked left and right hand
  references

The final approach will be selected following additional technical testing
and review of the experimental design.