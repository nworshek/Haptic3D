# Haptic3D HoloLens 1 Setup and Deployment

**Last Updated:** September 23, 2026

## Purpose

This document describes the current known-working development and
deployment workflow for running Haptic3D on Microsoft HoloLens 1.

The current configuration has been successfully used to build, deploy,
and run the Haptic3D Unity application on the laboratory HoloLens 1.

---

## Development Software

The current known-working development environment uses:

- Unity 2019.4.41f2
- Visual Studio 2019 Community
- Universal Windows Platform (UWP)
- Windows Mixed Reality / Built-in XR
- Windows 10 SDK
- HoloLens 1

Unity 2019.4 is intentionally used because the project targets the
legacy HoloLens 1 development environment.

---

## Visual Studio Components

Visual Studio 2019 should include the components required for:

- Universal Windows Platform development
- C++ UWP development
- Unity development
- Windows 10 SDK
- USB device connectivity

The current development computer has successfully used:

Windows SDK: 10.0.19041.0

Visual Studio 2019 Community 16.11.x has been used successfully with
the project.

---

## HoloLens Configuration

Before deployment, verify that the HoloLens is powered on and connected
to the same development network as the experimenter computer when
network communication is required.

The HoloLens should have:

- Developer Mode enabled
- Device Portal enabled
- Network connectivity
- Sufficient battery charge

The development computer should already be paired with the HoloLens
when required by the deployment workflow.

---

## Unity Project

Open the Haptic3D Unity project using:

Unity 2019.4.41f2

Do not upgrade the project to a newer Unity version without first
creating a backup or separate development branch.

Changes to the Unity version may affect HoloLens 1 compatibility.

---

## XR Configuration

The UWP build uses the Windows Mixed Reality / Built-in XR
configuration.

The existing XR configuration should be preserved unless a specific
development change requires modifying it.

The scene uses the existing XR camera hierarchy:

XRRig
    |
    v
Camera Offset
    |
    v
Main Camera

The Main Camera uses a Tracked Pose Driver for headset tracking.

Current tracking configuration:

Device:
Generic XR Device

Pose Source:
Center Eye - HMD Reference

Tracking Type:
Rotation And Position

Update Type:
Update And Before Render

---

## Build Configuration

The current Haptic3D HoloLens build targets:

- Platform: Universal Windows Platform
- Target Device: HoloLens
- Architecture: x86
- Build Type: D3D
- Build Configuration: Release

The generated Unity UWP build is stored locally in:

UWP/

The `UWP/` directory is generated during the build process and is
excluded from Git version control.

---

## Building From Unity

After making changes to the Unity project:

1. Save the current Unity scene.
2. Open the Unity Build Settings.
3. Verify Universal Windows Platform is selected.
4. Verify the HoloLens/UWP build configuration.
5. Select Build.
6. Select the local `UWP` directory as the build destination.
7. Wait for Unity to generate the Visual Studio UWP project.

Unity generates the Visual Studio solution used for HoloLens
deployment.

---

## Opening the UWP Build

After Unity finishes building:

1. Open the generated `UWP` directory.
2. Open the generated `.sln` file using Visual Studio 2019.
3. Allow Visual Studio to load the project completely.

Visual Studio 2019 should be used for the HoloLens UWP deployment
workflow.

VS Code is useful for editing project files but should not be used as
the primary UWP deployment environment.

---

## Visual Studio Deployment Configuration

Before building or deploying, verify the Visual Studio toolbar shows:

Configuration:

Release

Architecture:

x86

Target:

Device

This should appear approximately as:

Release | x86 | Device

---

## Build Solution

After Unity generates a new UWP project:

1. In Visual Studio 2019, select:

   Build > Build Solution

2. Wait for the build to complete.

3. Check the Output window for build errors.

`Build Solution` compiles the generated UWP application.

It does not by itself install the application onto the HoloLens.

---

## Deploy Solution

After the solution builds successfully:

1. Make sure the HoloLens is connected and available.
2. In Visual Studio 2019, select:

   Build > Deploy Solution

3. Wait for Visual Studio to install/update Haptic3D on the HoloLens.

`Deploy Solution` transfers the compiled application to the HoloLens.

---

## Normal Development Workflow

The normal development cycle is:

Edit Unity Project
    |
    v
Save Scene
    |
    v
Unity Build
    |
    v
Generate UWP Project
    |
    v
Open in Visual Studio 2019
    |
    v
Release / x86 / Device
    |
    v
Build Solution
    |
    v
Deploy Solution
    |
    v
Run Haptic3D on HoloLens

When Unity source or scene content changes, regenerate the UWP build
before deploying the updated application.

---

## HoloLens Hand Tracking

The current project contains experimental HoloLens 1 hand tracking.

The implementation uses the legacy HoloLens interaction-source system.

The hand tracker currently supports:

- Hand detection
- Handedness filtering
- Tracked hand position
- Position smoothing
- Visual debug marker
- Connection to the Unity fingertip proxy

The current development configuration tracks the right hand.

Current smoothing value:

12

The current approximate vertical calibration offset is:

+0.06 m

These settings may change as the interaction method is finalized.

---

## Hand Tracking Behavior

During current testing, hand tracking is most reliable when the
participant maintains a HoloLens-compatible ready/open-hand pose.

The hand-tracking marker has been observed to follow overall hand
translation consistently while the hand remains recognized.

Tracking can be lost when the participant uses hand configurations that
are not normally recognized by the HoloLens 1 interaction system.

This behavior is a current experimental-design consideration rather
than a Unity communication failure.

---

## HoloLens Networking

The HoloLens communicates with the Python experimenter application
using TCP over Wi-Fi.

Current TCP port:

5055

The HoloLens IP address may change between network sessions.

The current HoloLens IP should therefore be verified before running the
experimenter application.

The HoloLens and experimenter computer must be able to communicate over
the selected network.

---

## Important Architecture Note

The HoloLens does not communicate directly with the physical ESP32 USB
serial port.

The architecture is:

HoloLens / Unity
    |
    | Wi-Fi TCP
    v
Windows Experimenter Computer
    |
    | USB Serial
    v
ESP32-S3

The Python experimenter application acts as the bridge between the
HoloLens and physical haptic hardware.

---

## Testing After Deployment

After deploying a new Haptic3D build:

1. Launch Haptic3D on the HoloLens.
2. Verify that the Unity environment appears correctly.
3. Verify headset movement/tracking.
4. Verify HoloLens-to-PC TCP communication.
5. Verify the hand-tracking marker if hand tracking is being tested.
6. Verify virtual fingertip movement.
7. Verify that motors remain OFF outside an active Haptic Only trial.
8. Run a controlled haptic test if hardware functionality needs to be
   verified.

---

## Known-Good Deployment

The following configuration has successfully deployed Haptic3D:

Unity:
2019.4.41f2

Platform:
Universal Windows Platform

Device:
HoloLens

Architecture:
x86

Visual Studio:
2019 Community

Visual Studio Configuration:
Release | x86 | Device

This configuration should be treated as the baseline before changing
Unity, Visual Studio, SDK, XR, or deployment settings.

---

## Troubleshooting Principle

If a future change causes deployment or runtime problems, first compare
the environment against the known-good configuration documented above.

Avoid changing multiple Unity, XR, networking, or deployment settings
simultaneously.

Restore the known-good configuration first, confirm basic operation,
and then introduce changes individually.