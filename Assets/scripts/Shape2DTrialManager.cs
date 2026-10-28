using UnityEngine;

public class Shape2DTrialManager : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    public Shape2DDetector shapeDetector;
    public Shape2DHapticController hapticController;
    public Shape2DWorkspaceBoundary workspaceBoundary;
    public Shape2DStartMarker startMarker;


    // =========================================================
    // DEVELOPMENT VISUALIZATION
    // =========================================================

    [Header("Development Shape Visualization")]
    public GameObject squareRender;
    public GameObject circleRender;
    public GameObject triangleRender;

    [Tooltip("Development only. Turn OFF for participant trials.")]
    public bool showDevelopmentShape = true;


    [Header("Workspace Visuals (renderers only)")]
    [Tooltip("Assign renderers for the white backdrop, green region, boundary guides and start marker. Do not assign tracking or detector components.")]
    public Renderer[] workspaceVisualRenderers;

    // =========================================================
    // TRIAL STATE
    // =========================================================

    public enum TrialState
    {
        Idle,
        Prepared,
        Active,
        Complete
    }

    [Header("Trial State")]
    public TrialState currentState = TrialState.Idle;

    public const int TotalTrials = 3;
    public int currentTrialNumber = 0;
    public string participantResponse = "";
    public bool responseRecorded = false;
    public bool trialActive = false;


    // =========================================================
    // CURRENT SHAPE
    // =========================================================

    [Header("Current Shape")]
    public Shape2DDetector.ShapeType currentShape =
        Shape2DDetector.ShapeType.Square;


    // =========================================================
    // WORKSPACE STATUS
    // =========================================================

    [Header("Workspace Status")]
    public bool fingertipInsideWorkspace = false;
    public bool fingertipAtStart = false;
    public bool participantReady = false;


    // =========================================================
    // EXPERIMENTER TEST MODE
    // =========================================================

    [Header("Experimenter Test Mode")]
    public bool experimenterTestMode = false;


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        currentState = TrialState.Idle;
        currentTrialNumber = 0;
        participantResponse = "";
        responseRecorded = false;
        trialActive = false;
        experimenterTestMode = false;

        if (hapticController != null)
        {
            hapticController.DisableHaptics();
        }

        if (shapeDetector != null)
        {
            shapeDetector.activeShape = currentShape;
        }

        UpdateShapeVisualization();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        UpdateWorkspaceStatus();
    }


    // =========================================================
    // WORKSPACE STATUS
    // =========================================================

    private void UpdateWorkspaceStatus()
    {
        if (workspaceBoundary != null)
        {
            fingertipInsideWorkspace =
                workspaceBoundary.IsFingertipInsideWorkspace();
        }
        else
        {
            fingertipInsideWorkspace = false;
        }


        if (startMarker != null)
        {
            fingertipAtStart =
                startMarker.IsFingertipAtStart();
        }
        else
        {
            fingertipAtStart = false;
        }


        participantReady =
            fingertipInsideWorkspace &&
            fingertipAtStart;
    }


    // =========================================================
    // PREPARE PARTICIPANT TRIAL
    // =========================================================

    public void PrepareTrial(
        int trialNumber,
        Shape2DDetector.ShapeType shape
    )
    {
        if (experimenterTestMode)
        {
            Debug.LogWarning(
                "Cannot prepare participant trial while " +
                "Shape2D Test Mode is active."
            );

            return;
        }

        if (trialActive)
        {
            Debug.LogWarning(
                "Cannot prepare a new Shape2D trial " +
                "while another trial is active."
            );

            return;
        }

        if (trialNumber < 1 || trialNumber > TotalTrials ||
            shape != ShapeForTrial(trialNumber) ||
            (currentTrialNumber != 0 && (currentState != TrialState.Complete || trialNumber != currentTrialNumber + 1)) ||
            (currentTrialNumber == 0 && trialNumber != 1))
        {
            Debug.LogWarning("Invalid Shape2D trial progression.");
            return;
        }

        currentTrialNumber = trialNumber;
        participantResponse = "";
        responseRecorded = false;
        currentShape = shape;
        currentState = TrialState.Prepared;
        SetDevelopmentVisualization(false);

        if (shapeDetector != null)
        {
            shapeDetector.activeShape = shape;
        }

        if (hapticController != null)
        {
            hapticController.DisableHaptics();
        }

        UpdateShapeVisualization();

        Debug.Log(
            "Shape2D trial prepared: Trial " +
            currentTrialNumber +
            " | Shape: " +
            currentShape
        );
    }


    // =========================================================
    // START PARTICIPANT TRIAL
    // =========================================================

    public void StartTrial()
    {
        if (experimenterTestMode)
        {
            Debug.LogWarning(
                "Cannot start participant trial while " +
                "Shape2D Test Mode is active."
            );

            return;
        }

        if (currentState != TrialState.Prepared)
        {
            Debug.LogWarning(
                "Shape2D trial must be prepared before starting."
            );

            return;
        }

        currentState = TrialState.Active;
        trialActive = true;

        if (hapticController != null)
        {
            hapticController.EnableHaptics();
        }

        Debug.Log(
            "Shape2D trial started: Trial " +
            currentTrialNumber +
            " | Shape: " +
            currentShape
        );
    }


    // =========================================================
    // END PARTICIPANT TRIAL
    // =========================================================

    public void EndTrial()
    {
        if (!trialActive)
        {
            Debug.LogWarning(
                "No active Shape2D trial to end."
            );

            return;
        }

        if (hapticController != null)
        {
            hapticController.DisableHaptics();
        }

        trialActive = false;
        currentState = TrialState.Complete;
        // Shape identification is assessed later in the questionnaire.
        responseRecorded = true;

        Debug.Log(
            "Shape2D trial ended: Trial " +
            currentTrialNumber
        );
    }


    // =========================================================
    // FIXED THREE-TRIAL PARTICIPANT SEQUENCE
    // =========================================================
    public static Shape2DDetector.ShapeType ShapeForTrial(int number)
    {
        switch (number)
        {
            case 1: return Shape2DDetector.ShapeType.Square;
            case 2: return Shape2DDetector.ShapeType.Circle;
            default: return Shape2DDetector.ShapeType.Triangle;
        }
    }

    public bool PrepareNextTrial()
    {
        if (experimenterTestMode || trialActive || currentTrialNumber >= TotalTrials)
            return false;
        if (currentTrialNumber > 0 && (currentState != TrialState.Complete))
            return false;
        int next = currentTrialNumber + 1;
        PrepareTrial(next, ShapeForTrial(next));
        return currentState == TrialState.Prepared && currentTrialNumber == next;
    }

    public bool RecordResponse(string response)
    {
        if (currentState != TrialState.Complete || responseRecorded)
            return false;
        if (response != "Square" && response != "Circle" && response != "Triangle")
            return false;
        participantResponse = response;
        responseRecorded = true;
        return true;
    }

    public bool ExperimentComplete()
    {
        return currentTrialNumber == TotalTrials &&
               currentState == TrialState.Complete;
    }

    // =========================================================
    // ENTER EXPERIMENTER TEST MODE
    // =========================================================

    public void EnterTestMode()
    {
        if (trialActive)
        {
            Debug.LogWarning(
                "Cannot enter Shape2D Test Mode " +
                "during an active participant trial."
            );

            return;
        }

        experimenterTestMode = true;
        currentState = TrialState.Idle;
        SetDevelopmentVisualization(true);

        if (hapticController != null)
        {
            hapticController.DisableHaptics();
        }

        UpdateShapeVisualization();

        Debug.Log(
            "Shape2D Experimenter Test Mode entered."
        );
    }


    // =========================================================
    // EXIT EXPERIMENTER TEST MODE
    // =========================================================

    public void ExitTestMode()
    {
        if (hapticController != null)
        {
            hapticController.DisableHaptics();
        }

        experimenterTestMode = false;
        currentState = TrialState.Idle;
        trialActive = false;
        SetDevelopmentVisualization(false);

        UpdateShapeVisualization();

        Debug.Log(
            "Shape2D Experimenter Test Mode exited."
        );
    }


    // =========================================================
    // TEST SHAPES
    // =========================================================

    public void TestSquare()
    {
        SelectTestShape(
            Shape2DDetector.ShapeType.Square
        );
    }

    public void TestCircle()
    {
        SelectTestShape(
            Shape2DDetector.ShapeType.Circle
        );
    }

    public void TestTriangle()
    {
        SelectTestShape(
            Shape2DDetector.ShapeType.Triangle
        );
    }


    // =========================================================
    // SELECT TEST SHAPE
    // =========================================================

    private void SelectTestShape(
        Shape2DDetector.ShapeType shape
    )
    {
        if (!experimenterTestMode)
        {
            Debug.LogWarning(
                "Enter Shape2D Test Mode before " +
                "selecting a test shape."
            );

            return;
        }

        currentShape = shape;

        if (shapeDetector != null)
        {
            shapeDetector.activeShape = shape;
        }

        // Always disable haptics when switching shapes.
        if (hapticController != null)
        {
            hapticController.DisableHaptics();
        }

        UpdateShapeVisualization();

        Debug.Log(
            "Shape2D test shape selected: " +
            currentShape
        );
    }


    // =========================================================
    // ENABLE TEST HAPTICS
    // =========================================================

    public void EnableTestHaptics()
    {
        if (!experimenterTestMode)
        {
            Debug.LogWarning(
                "Cannot enable Shape2D test haptics " +
                "outside Experimenter Test Mode."
            );

            return;
        }

        if (hapticController != null)
        {
            hapticController.EnableHaptics();
        }

        Debug.Log(
            "Shape2D Test Mode haptics enabled."
        );
    }


    // =========================================================
    // DISABLE TEST HAPTICS
    // =========================================================

    public void DisableTestHaptics()
    {
        if (hapticController != null)
        {
            hapticController.DisableHaptics();
        }

        Debug.Log(
            "Shape2D Test Mode haptics disabled."
        );
    }


    // =========================================================
    // DEVELOPMENT SHAPE VISUALIZATION
    // =========================================================

    private void UpdateShapeVisualization()
    {
        // Only experimenter test mode can display the selected shape.
        // Never deactivate shape GameObjects: their other components may be needed.
        bool showShapes = experimenterTestMode && showDevelopmentShape;
        SetShapeRenderers(squareRender, showShapes && currentShape == Shape2DDetector.ShapeType.Square);
        SetShapeRenderers(circleRender, showShapes && currentShape == Shape2DDetector.ShapeType.Circle);
        SetShapeRenderers(triangleRender, showShapes && currentShape == Shape2DDetector.ShapeType.Triangle);

        // Workspace is visible for setup and test mode, hidden once trials are prepared.
        bool showWorkspace = currentTrialNumber == 0 || experimenterTestMode;
        if (workspaceVisualRenderers != null)
        {
            foreach (Renderer visual in workspaceVisualRenderers)
            {
                if (visual != null) visual.enabled = showWorkspace;
            }
        }

        // Unity Gizmos are editor-only debug drawings, separate from rendered shapes.
        if (shapeDetector != null) shapeDetector.showDebugShape = showShapes;
    }

    private static void SetShapeRenderers(GameObject root, bool visible)
    {
        if (root == null) return;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = visible;
        }
    }


    // =========================================================
    // SET DEVELOPMENT VISUALIZATION
    // =========================================================

    public void SetDevelopmentVisualization(bool visible)
    {
        showDevelopmentShape = visible;
        UpdateShapeVisualization();
    }


    // =========================================================
    // EMERGENCY STOP
    // =========================================================

    public void EmergencyStop()
    {
        if (hapticController != null)
        {
            hapticController.EmergencyStop();
        }

        trialActive = false;
        experimenterTestMode = false;
        currentState = TrialState.Idle;
        SetDevelopmentVisualization(false);

        Debug.Log(
            "Shape2D EMERGENCY STOP."
        );
    }


    // =========================================================
    // STATUS HELPERS
    // =========================================================

    public Shape2DDetector.ShapeType GetCurrentShape()
    {
        return currentShape;
    }

    public TrialState GetTrialState()
    {
        return currentState;
    }

    public bool IsTrialActive()
    {
        return trialActive;
    }

    public bool IsTestModeActive()
    {
        return experimenterTestMode;
    }

    public bool IsParticipantReady()
    {
        return participantReady;
    }


    // =========================================================
    // DISABLE
    // =========================================================

    void OnDisable()
    {
        if (hapticController != null)
        {
            hapticController.DisableHaptics();
        }

        trialActive = false;
    }
}