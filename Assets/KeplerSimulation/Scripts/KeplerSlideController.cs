using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KeplerSlideController : SimulationSlideController
{
    [Header("Control")]
    [SerializeField] private StartButton startButton;
    [SerializeField] private TimerButton timerButton;
    [SerializeField] private bool stopTimerAutomatically = true;
    [SerializeField] private bool resetOnSlideTransition;
    private bool simHasStarted;

    [Header("COM")]
    [SerializeField] private bool showCenterOfMass;

    [Header("Star")]
    [SerializeField] private bool hideStar;
    [SerializeField] private bool showStarLabel;
    [SerializeField] private Material starMaterial;

    [Header("Planet")]
    [SerializeField] private bool showPositionVector;
    [SerializeField] private bool showPlanetLabel;

    [Header("Orbit")]
    [SerializeField] private bool showOrbit;
    [SerializeField] private int numOrbitPoints = 500;
    [SerializeField] private bool showSemiMajorAxis;
    [SerializeField] private bool showSemiMinorAxis;
    [SerializeField] private bool showEllipseCenter;

    [Header("Text Displays")]
    [SerializeField] private TextMeshProUGUI timer;
    [SerializeField] private List<TextMeshProUGUI> measuredPeriod;
    [SerializeField] private List<TextMeshProUGUI> measuredSemiMajorAxis;
    [SerializeField] private TextMeshProUGUI measuredRatio;
    [SerializeField] private TextMeshProUGUI measuredError;

    private KeplerSimulation keplerSim;
    private KeplerPrefabManager prefabs;

    private float timerTime;
    private bool timerIsRunning;
    private float theta0;

    private float currentMeasuredPeriod;
    private float currentMeasuredSemiMajorAxis;
    private LineDrawer lineDrawer;

    private Material prevMaterial;

    private float CurrentMeasuredRatio
    {
        get
        {
            float T = currentMeasuredPeriod;
            float D = currentMeasuredSemiMajorAxis;

            if (D == 0)
            {
                return float.PositiveInfinity;
            }
            else
            {
                return T * T / D / D / D;
            }
        }
    }

    private void Awake()
    {
        keplerSim = (KeplerSimulation)simulation;
        if (!keplerSim.TryGetComponent(out prefabs))
        {
            Debug.LogWarning("KeplerSlideController did not find a KeplerPrefabManager");
        }
    }

    private void Start()
    {
        SetStarVisibility();
        SetOrbit();
        SetEllipseCenter();
        UpdateErrorText();
    }

    // For drawing lines and measuring distances
    private void Update()
    {
        // While dragging the mouse
        if (lineDrawer != null && Input.GetMouseButton(0))
        {
            currentMeasuredSemiMajorAxis = lineDrawer.Distance;
            UpdateSemiMajorAxisText(currentMeasuredSemiMajorAxis);
        }
    }

    private void FixedUpdate()
    {
        if (keplerSim.paused)
        {
            return;
        }

        // No vectors, trails, etc. to update without a prefabManager
        if (prefabs == null)
        {
            return;
        }

        UpdateVectors();

        if (timerIsRunning)
        {
            timerTime += keplerSim.timeScale * Time.fixedDeltaTime;

            if (stopTimerAutomatically)
            {
                // Check if we are back where we started
                float theta1 = keplerSim.theta;
                if (theta1 < 0 || theta1 < theta0)
                {
                    theta1 += 2 * Mathf.PI;
                }
                float thetaSwept = theta1 - theta0;
                if (Mathf.Abs(2 * Mathf.PI - thetaSwept) / (2 * Mathf.PI) <= 0.005f)
                {
                    ToggleTimer();
                    UpdateTimerText(keplerSim.Period);
                    return;
                }
            }

            UpdateTimerText(timerTime);
            UpdatePeriodText(timerTime);
            currentMeasuredPeriod = timerTime;

            if (timerTime >= 10)
            {
                ToggleTimer();
            }
        }
    }

    public override void ShowAndHideUIElements()
    {
        if (prefabs == null)
        {
            return;
        }

        prefabs.SetCenterOfMassVisibility(showCenterOfMass);
        prefabs.SetStarLabelVisibility(showStarLabel);
        prefabs.SetPositionVector1Visibility(showPositionVector);
        prefabs.SetOrbit1Visibility(showOrbit);
        prefabs.SetPlanetLabelVisibility(showPlanetLabel);
        prefabs.SetSemiMajorAxisVisibility(showSemiMajorAxis);
        prefabs.SetSemiMinorAxisVisibility(showSemiMinorAxis);
        prefabs.SetEllipseCenterVisibility(showEllipseCenter);

        simHasStarted = !keplerSim.paused;
        SetStartButtonText();
        SetTimerButtonText();

        if (starMaterial)
        {
            MeshRenderer mr = prefabs.star.GetComponent<MeshRenderer>();
            prevMaterial = mr.material;
            mr.material = starMaterial;
        }

        if (resetOnSlideTransition)
        {
            keplerSim.Reset();
        }

        if (measuredPeriod != null)
        {
            if (measuredPeriod.Count > 0)
            {
                currentMeasuredPeriod = float.Parse(measuredPeriod[0].text);
            }
        }
    }

    private void OnApplicationQuit()
    {
        prevMaterial = null;
    }

    private void OnDisable()
    {
        timerTime = 0;
        timerIsRunning = false;
        UpdateTimerText(0);

        // HACK not a robust way to pass values between slides...
        if (measuredSemiMajorAxis != null)
        {
            if (measuredSemiMajorAxis.Count > 0)
            {
                currentMeasuredSemiMajorAxis = float.Parse(measuredSemiMajorAxis[0].text);
            }
        }
        if (measuredPeriod != null)
        {
            if (measuredPeriod.Count > 0)
            {
                currentMeasuredPeriod = float.Parse(measuredPeriod[0].text);
            }
        }

        if (prevMaterial)
        {
            prefabs.star.GetComponent<MeshRenderer>().material = prevMaterial;
            prevMaterial = null;
        }
    }

    private void UpdateVectors()
    {
        // Radius vectors
        if (showPositionVector && prefabs.positionVector != null)
        {
            // TODO setting line width every frame is wasteful
            //prefabs.positionVector.SetLineWidth(positionVectorLineWidth);
            prefabs.positionVector.SetPositions(keplerSim.star.Position, keplerSim.planet1.Position);
            prefabs.positionVector.Redraw();
        }
    }

    private void ClearLineDrawer()
    {
        if (lineDrawer != null)
        {
            lineDrawer.Clear();
        }
    }

    // To be called by startButton
    public void TogglePlayPause()
    {
        keplerSim.TogglePlayPause();
        SetStartButtonText();
    }

    // To be called by resetButton
    public void Reset()
    {
        keplerSim.Reset();
        keplerSim.Pause();
        simHasStarted = false;
        timerTime = 0;
        timerIsRunning = false;
        UpdateVectors();
        SetStartButtonText();
        SetTimerButtonText();
        UpdateTimerText(0);
        UpdatePeriodText(0);
        UpdateSemiMajorAxisText(0);

        currentMeasuredPeriod = 0;
        currentMeasuredSemiMajorAxis = 0;
        UpdateMeasuredRatioText();
        UpdateErrorText();
    }

    // To be called by the randomize button
    public void Randomize()
    {
        //Debug.Log("Randomising");
        //keplerSim.Reset();
        keplerSim.Randomize(true);  // Center of the ellipse at (0, 0, 0)
        SetOrbit();
        SetEllipseCenter();
        ClearLineDrawer();

        simHasStarted = true;
        timerTime = 0;
        timerIsRunning = false;
        theta0 = 0;

        SetTimerButtonText();
        UpdateTimerText(0);
        UpdatePeriodText(0);
        UpdateSemiMajorAxisText(0);

        currentMeasuredPeriod = 0;
        currentMeasuredSemiMajorAxis = 0;
        UpdateMeasuredRatioText();
        UpdateErrorText();
    }

    // To be called by the compute button
    public void ComputeMeasuredRatio()
    {
        //Debug.Log("D = " + currentMeasuredSemiMajorAxis);
        //Debug.Log("T = " + currentMeasuredPeriod);
        UpdateMeasuredRatioText();
        UpdateErrorText();
    }

    // To be called by timerButton
    public void ToggleTimer()
    {
        if (keplerSim.paused)
        {
            return;
        }

        timerIsRunning = !timerIsRunning;
        if (timerIsRunning)
        {
            timerTime = 0;
            theta0 = keplerSim.theta;
            if (theta0 < 0)
            {
                theta0 += Mathf.PI;
            }
        }

        SetTimerButtonText();
    }

    private void SetStartButtonText()
    {
        if (startButton == null)
        {
            return;
        }

        if (keplerSim.paused)
        {
            if (simHasStarted)
            {
                startButton.ShowResumeText();
            }
            else
            {
                startButton.ShowStartText();
                simHasStarted = true;
            }
        }
        else
        {
            startButton.ShowPauseText();
        }
    }

    private void SetTimerButtonText()
    {
        if (timerButton)
        {
            if (timerIsRunning)
            {
                timerButton.ShowStopText();
            }
            else
            {
                timerButton.ShowStartText();
            }
        }
    }

    private void UpdateTimerText(float newTime)
    {
        if (timer != null)
        {
            timer.text = newTime.ToString("0.00");
        }
    }

    private void UpdatePeriodText(float period)
    {
        if (measuredPeriod != null)
        {
            foreach (TextMeshProUGUI tmp in measuredPeriod)
            {
                tmp.text = period.ToString("0.00");
            }
        }
    }

    private void UpdateSemiMajorAxisText(float a)
    {
        if (measuredSemiMajorAxis != null)
        {
            foreach (TextMeshProUGUI tmp in measuredSemiMajorAxis)
            {
                tmp.text = a.ToString("0.00");
            }
        }
    }

    private void UpdateMeasuredRatioText()
    {
        if (measuredRatio)
        {
            if (currentMeasuredSemiMajorAxis == 0)
            {
                measuredRatio.text = "0.00";
            }
            else if (CurrentMeasuredRatio > 99.9f)
            {
                measuredRatio.text = ":(";
            }
            else
            {
                measuredRatio.text = CurrentMeasuredRatio.ToString("0.00");
            }
        }
    }

    private void UpdateErrorText()
    {
        if (measuredError != null)
        {
            if (currentMeasuredSemiMajorAxis == 0)
            {
                measuredError.text = "";
            }
            else if (CurrentMeasuredRatio > 99.9f)
            {
                measuredError.text = "N/A";
            }
            else
            {
                float truth = 4 * Mathf.PI * Mathf.PI / keplerSim.NewtonG;
                float error = 100 * Mathf.Abs(CurrentMeasuredRatio - truth) / truth;
                measuredError.text = error.ToString("0") + "%";
            }
        }
    }

    private void SetStarVisibility()
    {
        if (hideStar)
        {
            keplerSim.HideStar();
        }
    }

    private void SetOrbit()
    {
        if (showOrbit && prefabs.orbit != null)
        {
            Vector3[] positions = new Vector3[numOrbitPoints];
            for (int i = 0; i < numOrbitPoints; i++)
            {
                float theta = i * 2f * Mathf.PI / numOrbitPoints;
                float r = keplerSim.StarToPlanetDistance(theta);
                positions[i] = keplerSim.star.Position + new Vector3(r * Mathf.Cos(theta), r * Mathf.Sin(theta), 0);
            }

            prefabs.orbit.positionCount = numOrbitPoints;
            prefabs.orbit.SetPositions(positions);
        }

        if (showSemiMajorAxis && prefabs.semiMajorAxis)
        {
            float a = keplerSim.orbitalParameters.a;
            float e = keplerSim.orbitalParameters.e;

            Vector3 direction = (keplerSim.starAtFocus == KeplerSimulation.Focus.Left) ? Vector3.right : Vector3.left;
            Vector3 tail = keplerSim.star.Position + e * a * direction;
            Vector3 head = tail + a * direction;
            prefabs.semiMajorAxis.SetPositions(tail, head);
            prefabs.semiMajorAxis.Redraw();
        }

        if (showSemiMinorAxis && prefabs.semiMinorAxis)
        {
            float a = keplerSim.orbitalParameters.a;
            float e = keplerSim.orbitalParameters.e;

            Vector3 direction = (keplerSim.starAtFocus == KeplerSimulation.Focus.Left) ? Vector3.right : Vector3.left;
            Vector3 tail = keplerSim.star.Position + e * a * direction;
            Vector3 head = tail + Mathf.Sqrt(1 - e * e) * a * Vector3.up;
            prefabs.semiMinorAxis.SetPositions(tail, head);
            prefabs.semiMinorAxis.Redraw();
        }
    }

    private void SetEllipseCenter()
    {
        if (showEllipseCenter && prefabs.ellipseCenter)
        {
            float a = keplerSim.orbitalParameters.a;
            float e = keplerSim.orbitalParameters.e;
            Vector3 direction = (keplerSim.starAtFocus == KeplerSimulation.Focus.Left) ? Vector3.right : Vector3.left;
            prefabs.ellipseCenter.position = keplerSim.star.Position + e * a * direction;

            // Get reference to the lineDrawer so we can update text displays based on the distance drawn
            EllipseCenter center = prefabs.ellipseCenter.GetComponent<EllipseCenter>();
            lineDrawer = center.GetLineDrawer();
            center.ResetForcedStartPosition();
        }
    }
}
