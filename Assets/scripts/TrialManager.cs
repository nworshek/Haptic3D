using UnityEngine;

public class TrialManager : MonoBehaviour
{
    public enum TrialState
    {
        VisualExploration,
        HapticOnly,
        Neutral
    }

    [Header("Trial State")]
    public TrialState currentState = TrialState.Neutral;

    [Header("Trial Status")]
    public bool trialActive = false;

    [Header("Stimuli")]
    public GameObject flatCube;
    public GameObject ridgeCube;
    public GameObject bumpCube;

    [Header("Current Stimulus")]
    public GameObject currentHapticObject;

    [Header("Black Box")]
    public GameObject blackBox;

    [Header("ESP32")]
    public ESP32Serial esp32Serial;

    [Header("Debug Controls")]
    public bool allowKeyboardControl = true;

    [Header("Experimenter Test Mode")]
    public bool experimenterTestMode = false;
    public bool testHapticsEnabled = false;

    private Renderer[] currentRenderers;


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        if (esp32Serial == null)
        {
            esp32Serial =
                FindObjectOfType<ESP32Serial>();
        }

        DisableAllStimuli();

        if (blackBox != null)
        {
            blackBox.SetActive(false);
        }

        currentHapticObject = null;
        currentRenderers = null;

        trialActive = false;

        experimenterTestMode = false;
        testHapticsEnabled = false;

        SetState(
            TrialState.Neutral
        );
    }


    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        // =====================================================
        // EMERGENCY STOP
        // =====================================================

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EmergencyStop();
        }

        if (!allowKeyboardControl)
        {
            return;
        }


        // =====================================================
        // TRIAL CONTROL
        // =====================================================

        // N = Start trial
        if (Input.GetKeyDown(KeyCode.N))
        {
            StartTrial();
        }

        // M = End trial
        if (Input.GetKeyDown(KeyCode.M))
        {
            EndTrial();
        }


        // =====================================================
        // STATE CONTROL
        // =====================================================

        // 1 = Visual Exploration
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetState(
                TrialState.VisualExploration
            );
        }

        // 2 = Haptic Only
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetState(
                TrialState.HapticOnly
            );
        }

        // 3 = Neutral
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetState(
                TrialState.Neutral
            );
        }


        // =====================================================
        // STIMULUS SELECTION
        // =====================================================

        // 7 = Flat
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            SelectStimulus(
                flatCube
            );
        }

        // 8 = Ridges
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            SelectStimulus(
                ridgeCube
            );
        }

        // 9 = Bumps
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            SelectStimulus(
                bumpCube
            );
        }
    }


    // =========================================================
    // START TRIAL
    // =========================================================

    public void StartTrial()
    {
        if (experimenterTestMode)
        {
            Debug.LogWarning(
                "Cannot start an official trial while Experimenter Test Mode is active."
            );

            return;
        }

        if (trialActive)
        {
            Debug.LogWarning(
                "Cannot start trial: a trial is already active."
            );

            return;
        }

        if (currentHapticObject == null)
        {
            Debug.LogWarning(
                "Cannot start trial: no stimulus is selected."
            );

            return;
        }

        if (currentState == TrialState.Neutral)
        {
            Debug.LogWarning(
                "Cannot start trial: select Visual Exploration or Haptic Only first."
            );

            return;
        }

        if (TrialLogger.Instance == null)
        {
            Debug.LogWarning(
                "Cannot start trial: TrialLogger not found."
            );

            return;
        }

        // Always begin with motor off.
        StopMotor();

        TrialLogger.Instance.StartNextTrial(
            currentState.ToString(),
            GetCurrentStimulusName()
        );

        trialActive = true;

        Debug.Log(
            "TRIAL ACTIVE: " +
            TrialLogger.Instance.currentTrialNumber
        );
    }


    // =========================================================
    // END TRIAL
    // =========================================================

    public void EndTrial()
    {
        if (!trialActive)
        {
            Debug.LogWarning(
                "Cannot end trial: no trial is currently active."
            );

            return;
        }

        // Stop motor immediately.
        StopMotor();

        if (TrialLogger.Instance != null)
        {
            TrialLogger.Instance.EndCurrentTrial(
                currentState.ToString(),
                GetCurrentStimulusName()
            );
        }

        trialActive = false;

        // Return environment to neutral.
        currentState =
            TrialState.Neutral;

        ApplyCurrentState();

        Debug.Log(
            "TRIAL ENDED - SYSTEM RETURNED TO NEUTRAL"
        );
    }


    // =========================================================
    // STIMULUS SELECTION
    // =========================================================

    public void SelectStimulus(
        GameObject stimulus
    )
    {
        if (experimenterTestMode)
        {
            Debug.LogWarning(
                "Use the Experimenter Test Mode stimulus controls while Test Mode is active."
            );

            return;
        }

        if (trialActive)
        {
            Debug.LogWarning(
                "Cannot change stimulus while a trial is active."
            );

            return;
        }

        if (stimulus == null)
        {
            Debug.LogWarning(
                "Attempted to select a stimulus that is not assigned."
            );

            return;
        }

        StopMotor();

        DisableAllStimuli();

        currentHapticObject =
            stimulus;

        currentRenderers =
            currentHapticObject
            .GetComponentsInChildren<Renderer>(true);

        Debug.Log(
            "STIMULUS SELECTED: " +
            currentHapticObject.name
        );

        if (TrialLogger.Instance != null)
        {
            TrialLogger.Instance.LogEvent(
                currentState.ToString(),
                currentHapticObject.name,
                "Stimulus Selected"
            );
        }

        ApplyCurrentState();
    }


    // =========================================================
    // STATE CONTROL
    // =========================================================

    public void SetState(
        TrialState newState
    )
    {
        if (experimenterTestMode)
        {
            Debug.LogWarning(
                "Cannot change official trial state while Experimenter Test Mode is active."
            );

            return;
        }

        if (trialActive)
        {
            Debug.LogWarning(
                "Cannot change trial state while a trial is active."
            );

            return;
        }

        StopMotor();

        currentState =
            newState;

        ApplyCurrentState();

        Debug.Log(
            "TRIAL STATE: " +
            currentState
        );

        if (TrialLogger.Instance != null)
        {
            TrialLogger.Instance.LogEvent(
                currentState.ToString(),
                GetCurrentStimulusName(),
                "State Changed"
            );
        }
    }


    // =========================================================
    // APPLY CURRENT STATE
    // =========================================================

    private void ApplyCurrentState()
    {
        switch (currentState)
        {
            case TrialState.VisualExploration:

                EnterVisualExploration();

                break;

            case TrialState.HapticOnly:

                EnterHapticOnly();

                break;

            case TrialState.Neutral:

                EnterNeutral();

                break;
        }
    }


    // =========================================================
    // VISUAL EXPLORATION
    // =========================================================

    private void EnterVisualExploration()
    {
        if (currentHapticObject != null)
        {
            currentHapticObject.SetActive(
                true
            );

            RefreshCurrentRenderers();

            SetObjectVisible(
                true
            );
        }

        if (blackBox != null)
        {
            blackBox.SetActive(
                false
            );
        }
    }


    // =========================================================
    // HAPTIC ONLY
    // =========================================================

    private void EnterHapticOnly()
    {
        if (currentHapticObject != null)
        {
            currentHapticObject.SetActive(
                true
            );

            RefreshCurrentRenderers();

            SetObjectVisible(
                false
            );
        }

        if (blackBox != null)
        {
            blackBox.SetActive(
                true
            );
        }
    }


    // =========================================================
    // NEUTRAL
    // =========================================================

    private void EnterNeutral()
    {
        StopMotor();

        DisableAllStimuli();

        if (blackBox != null)
        {
            blackBox.SetActive(
                false
            );
        }
    }


    // =========================================================
    // RENDERER CONTROL
    // =========================================================

    private void RefreshCurrentRenderers()
    {
        if (currentHapticObject == null)
        {
            currentRenderers =
                null;

            return;
        }

        currentRenderers =
            currentHapticObject
            .GetComponentsInChildren<Renderer>(true);
    }


    private void SetObjectVisible(
        bool visible
    )
    {
        if (currentRenderers == null)
        {
            return;
        }

        foreach (Renderer r in currentRenderers)
        {
            if (r != null)
            {
                r.enabled =
                    visible;
            }
        }
    }


    // =========================================================
    // STIMULUS CONTROL
    // =========================================================

    private void DisableAllStimuli()
    {
        if (flatCube != null)
        {
            flatCube.SetActive(false);
        }

        if (ridgeCube != null)
        {
            ridgeCube.SetActive(false);
        }

        if (bumpCube != null)
        {
            bumpCube.SetActive(false);
        }
    }


    private string GetCurrentStimulusName()
    {
        if (currentHapticObject == null)
        {
            return "None";
        }

        return currentHapticObject.name;
    }


    // =========================================================
    // MOTOR CONTROL
    // =========================================================

    private void StopMotor()
    {
        if (esp32Serial != null)
        {
            esp32Serial.MotorOff();
        }
    }


    // =========================================================
    // EXPERIMENTER TEST MODE
    // =========================================================

    public void EnterExperimenterTestMode()
    {
        // Test Mode can never interrupt an official trial.
        if (trialActive)
        {
            Debug.LogWarning(
                "Cannot enter Experimenter Test Mode while an official trial is active."
            );

            return;
        }

        // Always begin with the motor disabled.
        StopMotor();

        experimenterTestMode = true;
        testHapticsEnabled = false;

        // Test Mode is separate from official trial states.
        currentState =
            TrialState.Neutral;

        // Start with no stimulus.
        DisableAllStimuli();

        currentHapticObject =
            null;

        currentRenderers =
            null;

        // BlackBox is never used during Experimenter Test Mode.
        if (blackBox != null)
        {
            blackBox.SetActive(
                false
            );
        }

        Debug.Log(
            "EXPERIMENTER TEST MODE ENABLED"
        );
    }


    public void ExitExperimenterTestMode()
    {
        // Immediately stop all haptic output.
        StopMotor();

        experimenterTestMode = false;
        testHapticsEnabled = false;

        // Return to normal Neutral state.
        currentState =
            TrialState.Neutral;

        trialActive =
            false;

        DisableAllStimuli();

        currentHapticObject =
            null;

        currentRenderers =
            null;

        if (blackBox != null)
        {
            blackBox.SetActive(
                false
            );
        }

        Debug.Log(
            "EXPERIMENTER TEST MODE DISABLED - SYSTEM RETURNED TO NEUTRAL"
        );
    }


    // =========================================================
    // TEST HAPTICS
    // =========================================================

    public void EnableTestHaptics()
    {
        if (!experimenterTestMode)
        {
            Debug.LogWarning(
                "Cannot enable test haptics: Experimenter Test Mode is not active."
            );

            return;
        }

        testHapticsEnabled =
            true;

        Debug.Log(
            "EXPERIMENTER TEST HAPTICS ENABLED"
        );
    }


    public void DisableTestHaptics()
    {
        testHapticsEnabled =
            false;

        // Immediately clear all motor output.
        StopMotor();

        Debug.Log(
            "EXPERIMENTER TEST HAPTICS DISABLED"
        );
    }


    // =========================================================
    // TEST STIMULUS SELECTION
    // =========================================================

    public void TestFlat()
    {
        SelectTestStimulus(
            flatCube
        );
    }


    public void TestRidges()
    {
        SelectTestStimulus(
            ridgeCube
        );
    }


    public void TestBumps()
    {
        SelectTestStimulus(
            bumpCube
        );
    }


    private void SelectTestStimulus(
        GameObject stimulus
    )
    {
        if (!experimenterTestMode)
        {
            Debug.LogWarning(
                "Cannot select test stimulus: Experimenter Test Mode is not active."
            );

            return;
        }

        if (stimulus == null)
        {
            Debug.LogWarning(
                "Cannot select test stimulus: stimulus is not assigned."
            );

            return;
        }

        // Stop motor before changing surfaces.
        StopMotor();

        DisableAllStimuli();

        currentHapticObject =
            stimulus;

        currentHapticObject.SetActive(
            true
        );

        RefreshCurrentRenderers();

        // Test Mode always shows the real stimulus geometry.
        SetObjectVisible(
            true
        );

        // Never display the BlackBox in Test Mode.
        if (blackBox != null)
        {
            blackBox.SetActive(
                false
            );
        }

        Debug.Log(
            "EXPERIMENTER TEST STIMULUS: " +
            currentHapticObject.name
        );
    }


    // =========================================================
    // EMERGENCY STOP
    // =========================================================

    public void EmergencyStop()
    {
        string previousStimulus =
            GetCurrentStimulusName();

        // Immediately stop all haptic output.
        StopMotor();

        DisableAllStimuli();

        if (blackBox != null)
        {
            blackBox.SetActive(false);
        }

        currentHapticObject =
            null;

        currentRenderers =
            null;

        currentState =
            TrialState.Neutral;

        trialActive =
            false;

        // Emergency stop also exits Experimenter Test Mode.
        experimenterTestMode =
            false;

        testHapticsEnabled =
            false;

        if (TrialLogger.Instance != null)
        {
            TrialLogger.Instance.LogEvent(
                currentState.ToString(),
                previousStimulus,
                "Emergency Stop"
            );
        }

        Debug.LogWarning(
            "EMERGENCY STOP ACTIVATED - Motor OFF, stimuli disabled, trial and Test Mode reset."
        );
    }


    // =========================================================
    // FAIL-SAFE
    // =========================================================

    private void OnDisable()
    {
        StopMotor();

        experimenterTestMode =
            false;

        testHapticsEnabled =
            false;
    }


    private void OnApplicationQuit()
    {
        StopMotor();
    }
}