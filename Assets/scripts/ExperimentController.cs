using UnityEngine;

public class ExperimentController : MonoBehaviour
{
    public enum StimulusType
    {
        Flat,
        Ridges,
        Bumps
    }

    [System.Serializable]
    public class TrialDefinition
    {
        public string trialName;
        public TrialManager.TrialState condition;
        public StimulusType stimulus;
    }

    [Header("References")]
    public TrialManager trialManager;
    public SimulatedFingertipController fingertipController;

    [Header("Trial Sequence")]
    public TrialDefinition[] trials;

    [Header("Current Trial")]
    public int currentTrialIndex = -1;
    public bool trialPrepared = false;

    void Start()
    {
        if (trialManager == null)
        {
            trialManager = FindObjectOfType<TrialManager>();
        }

        if (fingertipController == null)
        {
            fingertipController =
                FindObjectOfType<SimulatedFingertipController>();
        }

        if (trialManager == null)
        {
            Debug.LogError(
                "ExperimentController: TrialManager not found."
            );

            enabled = false;
            return;
        }

        Debug.Log(
            "ExperimentController ready. Trials configured: " +
            GetTotalTrials()
        );
    }

    // =========================================================
    // PREPARE NEXT TRIAL
    // =========================================================

    public void PrepareNextTrial()
    {
        if (trialManager.trialActive)
        {
            Debug.LogWarning(
                "Cannot prepare next trial while a trial is active."
            );

            return;
        }

        int nextIndex =
            currentTrialIndex + 1;

        if (nextIndex >= trials.Length)
        {
            Debug.Log(
                "ALL TRIALS COMPLETE."
            );

            return;
        }

        currentTrialIndex =
            nextIndex;

        ConfigureCurrentTrial();
    }

    // =========================================================
    // CONFIGURE CURRENT TRIAL
    // =========================================================

    private void ConfigureCurrentTrial()
    {
        if (currentTrialIndex < 0 ||
            currentTrialIndex >= trials.Length)
        {
            return;
        }

        TrialDefinition trial =
            trials[currentTrialIndex];

        // Return system to safe baseline first.
        trialManager.SetState(
            TrialManager.TrialState.Neutral
        );

        // Select stimulus.
        SelectStimulus(
            trial.stimulus
        );

        // Apply condition.
        trialManager.SetState(
            trial.condition
        );

        // Reset simulated fingertip.
        if (fingertipController != null)
        {
            fingertipController.ResetFingertip();
        }

        trialPrepared =
            true;

        Debug.Log(
            "TRIAL PREPARED: " +
            GetCurrentTrialNumber() +
            " / " +
            GetTotalTrials() +
            " | " +
            trial.trialName +
            " | " +
            trial.condition +
            " | " +
            trial.stimulus
        );
    }

    // =========================================================
    // START TRIAL
    // =========================================================

    public void StartPreparedTrial()
    {
        if (!trialPrepared)
        {
            Debug.LogWarning(
                "No trial is prepared."
            );

            return;
        }

        if (trialManager.trialActive)
        {
            Debug.LogWarning(
                "A trial is already active."
            );

            return;
        }

        trialManager.StartTrial();

        if (trialManager.trialActive)
        {
            trialPrepared = false;

            Debug.Log(
                "TRIAL STARTED: " +
                GetCurrentTrialNumber()
            );
        }
    }

    // =========================================================
    // END TRIAL
    // =========================================================

    public void EndCurrentTrial()
    {
        if (!trialManager.trialActive)
        {
            Debug.LogWarning(
                "No active trial to end."
            );

            return;
        }

        trialManager.EndTrial();

        Debug.Log(
            "TRIAL ENDED: " +
            GetCurrentTrialNumber()
        );
    }

    // =========================================================
    // REPEAT TRIAL
    // =========================================================

    public void RepeatCurrentTrial()
    {
        if (currentTrialIndex < 0 ||
            currentTrialIndex >= trials.Length)
        {
            Debug.LogWarning(
                "No trial is available to repeat."
            );

            return;
        }

        if (trialManager.trialActive)
        {
            trialManager.EndTrial();
        }

        ConfigureCurrentTrial();

        Debug.Log(
            "TRIAL READY TO REPEAT: " +
            GetCurrentTrialNumber()
        );
    }

    // =========================================================
    // EMERGENCY STOP
    // =========================================================

    public void EmergencyStop()
    {
        trialManager.EmergencyStop();

        trialPrepared =
            false;

        Debug.LogWarning(
            "EXPERIMENT EMERGENCY STOP."
        );
    }

    // =========================================================
    // STIMULUS SELECTION
    // =========================================================

    private void SelectStimulus(
        StimulusType stimulus
    )
    {
        switch (stimulus)
        {
            case StimulusType.Flat:

                trialManager.SelectStimulus(
                    trialManager.flatCube
                );

                break;

            case StimulusType.Ridges:

                trialManager.SelectStimulus(
                    trialManager.ridgeCube
                );

                break;

            case StimulusType.Bumps:

                trialManager.SelectStimulus(
                    trialManager.bumpCube
                );

                break;
        }
    }

    // =========================================================
    // STATUS
    // =========================================================

    public string GetCurrentTrialName()
    {
        if (currentTrialIndex < 0 ||
            currentTrialIndex >= trials.Length)
        {
            return "None";
        }

        return trials[currentTrialIndex].trialName;
    }

    public int GetCurrentTrialNumber()
    {
        if (currentTrialIndex < 0)
        {
            return 0;
        }

        return currentTrialIndex + 1;
    }

    public int GetTotalTrials()
    {
        if (trials == null)
        {
            return 0;
        }

        return trials.Length;
    }

    public string GetCurrentCondition()
    {
        if (currentTrialIndex < 0 ||
            currentTrialIndex >= trials.Length)
        {
            return "None";
        }

        return trials[currentTrialIndex]
            .condition
            .ToString();
    }

    public string GetCurrentStimulus()
    {
        if (currentTrialIndex < 0 ||
            currentTrialIndex >= trials.Length)
        {
            return "None";
        }

        return trials[currentTrialIndex]
            .stimulus
            .ToString();
    }

    public string GetTrialStatus()
    {
        if (trialManager.trialActive)
        {
            return "ACTIVE";
        }

        if (trialPrepared)
        {
            return "READY";
        }

        if (currentTrialIndex >= trials.Length - 1 &&
            currentTrialIndex >= 0)
        {
            return "COMPLETE";
        }

        return "WAITING";
    }
}