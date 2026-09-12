using UnityEngine;
using UnityEngine.UI;

public class ExperimenterUI : MonoBehaviour
{
    [Header("References")]
    public ExperimentController experimentController;
    public ESP32Serial esp32Serial;

    [Header("Status")]
    public Text statusText;
    public Text esp32StatusText;
    public Text motorStatusText;

    [Header("Participant")]
    public InputField participantIDInput;

    [Header("Buttons")]
    public Button prepareButton;
    public Button startButton;
    public Button endButton;
    public Button repeatButton;
    public Button emergencyStopButton;


    // ============================================================
    // START
    // ============================================================

    void Start()
    {
        // --------------------------------------------------------
        // FIND REFERENCES
        // --------------------------------------------------------

        if (experimentController == null)
            experimentController = FindObjectOfType<ExperimentController>();

        if (esp32Serial == null)
            esp32Serial = FindObjectOfType<ESP32Serial>();


        // --------------------------------------------------------
        // VERIFY EXPERIMENT CONTROLLER
        // --------------------------------------------------------

        if (experimentController == null)
        {
            Debug.LogError(
                "ExperimenterUI: ExperimentController not found."
            );

            enabled = false;
            return;
        }


        // --------------------------------------------------------
        // BUTTON LISTENERS
        // --------------------------------------------------------

        if (prepareButton != null)
            prepareButton.onClick.AddListener(
                experimentController.PrepareNextTrial
            );

        if (startButton != null)
            startButton.onClick.AddListener(
                experimentController.StartPreparedTrial
            );

        if (endButton != null)
            endButton.onClick.AddListener(
                experimentController.EndCurrentTrial
            );

        if (repeatButton != null)
            repeatButton.onClick.AddListener(
                experimentController.RepeatCurrentTrial
            );

        if (emergencyStopButton != null)
            emergencyStopButton.onClick.AddListener(
                experimentController.EmergencyStop
            );


        // --------------------------------------------------------
        // PARTICIPANT ID LISTENER
        // --------------------------------------------------------

        if (participantIDInput != null)
        {
            participantIDInput.onEndEdit.AddListener(
                SetParticipantID
            );
        }


        // --------------------------------------------------------
        // INITIAL UI UPDATE
        // --------------------------------------------------------

        UpdateUI();
    }


    // ============================================================
    // UPDATE
    // ============================================================

    void Update()
    {
        UpdateUI();
    }


    // ============================================================
    // PARTICIPANT ID
    // ============================================================

    private void SetParticipantID(string id)
    {
        if (TrialLogger.Instance != null)
        {
            TrialLogger.Instance.SetParticipantID(id);
        }
        else
        {
            Debug.LogWarning(
                "ExperimenterUI: TrialLogger not found."
            );
        }
    }


    // ============================================================
    // UPDATE UI
    // ============================================================

    private void UpdateUI()
    {
        if (experimentController == null)
            return;


        // --------------------------------------------------------
        // EXPERIMENT STATUS
        // --------------------------------------------------------

        if (statusText != null)
        {
            statusText.text =
                "INTERNAL TEST CONTROL\n\n" +
                "Trial: " +
                experimentController.GetCurrentTrialNumber() +
                " / " +
                experimentController.GetTotalTrials() +
                "\n" +

                "Name: " +
                experimentController.GetCurrentTrialName() +
                "\n" +

                "Condition: " +
                experimentController.GetCurrentCondition() +
                "\n" +

                "Stimulus: " +
                experimentController.GetCurrentStimulus() +
                "\n\n" +

                "Status: " +
                experimentController.GetTrialStatus();
        }


        // --------------------------------------------------------
        // ESP32 STATUS
        // --------------------------------------------------------

        if (esp32StatusText != null)
        {
            if (esp32Serial != null &&
                esp32Serial.IsConnected)
            {
                esp32StatusText.text =
                    "ESP32: CONNECTED";
            }
            else
            {
                esp32StatusText.text =
                    "ESP32: DISCONNECTED";
            }
        }


        // --------------------------------------------------------
        // MOTOR STATUS
        // --------------------------------------------------------

        if (motorStatusText != null)
        {
            if (esp32Serial == null ||
                !esp32Serial.IsConnected)
            {
                motorStatusText.text =
                    "Motor: OFFLINE";
            }
            else
            {
                int intensity =
                    esp32Serial.GetCurrentIntensity();

                if (intensity <= 0)
                {
                    motorStatusText.text =
                        "Motor: OFF";
                }
                else
                {
                    motorStatusText.text =
                        "Motor: " +
                        intensity +
                        "%";
                }
            }
        }


        // --------------------------------------------------------
        // BUTTON STATES
        // --------------------------------------------------------

        if (experimentController.trialManager == null)
            return;

        bool active =
            experimentController.trialManager.trialActive;

        bool prepared =
            experimentController.trialPrepared;


        // PREPARE
        bool hasParticipantID =
            participantIDInput != null &&
            !string.IsNullOrWhiteSpace(participantIDInput.text);

        if (prepareButton != null)
        {
            prepareButton.interactable =
                !active &&
                !prepared &&
                hasParticipantID;
        }


        // START
        if (startButton != null)
        {
            startButton.interactable =
                prepared &&
                !active;
        }


        // END
        if (endButton != null)
        {
            endButton.interactable =
                active;
        }


        // REPEAT
        if (repeatButton != null)
        {
            repeatButton.interactable =
                experimentController.currentTrialIndex >= 0;
        }


        // EMERGENCY STOP
        if (emergencyStopButton != null)
        {
            emergencyStopButton.interactable =
                true;
        }
    }
}