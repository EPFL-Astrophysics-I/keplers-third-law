using UnityEngine;

// This class is very similar to TwoBodySimulation, except we assume here that the
// mass of one body (the star) is very large compared to the mass of the other (the planet)
//
// Default units used here are
// 1 L = 1 AU
// 1 M = 1 solar mass
// 1 T = 1 year
//
[RequireComponent(typeof(KeplerPrefabManager))]
public class KeplerSimulation : Simulation
{
    private KeplerPrefabManager prefabs;

    [Header("Simulation Properties")]
    public int numSubsteps = 10;
    public bool resetAfterOnePeriod = true;
    public enum UnitTime { Year, Month, Day }
    public UnitTime unitTime = UnitTime.Year;
    public enum UnitLength { AU, SolarRadius }
    public UnitLength unitLength = UnitLength.AU;
    public enum UnitMass { SolarMass }
    public UnitMass unitMass = UnitMass.SolarMass;
    public float timeScale = 1;

    [Header("Star")]
    public float starMass = 1f;
    public float starRadius = 10f;
    public Vector2 starPosition = Vector2.zero;
    public enum Focus { Left, Right }
    public Focus starAtFocus = Focus.Left;

    [Header("Planet & Orbit")]
    public float planet1Radius = 1f;
    public float perihelionDistance = 21.48f;
    public bool startAtPerihelion = true;
    public float eccentricity = 0.016f;
    public enum OrbitDirection { Clockwise, Counterclockwise }
    public OrbitDirection orbitDirection = OrbitDirection.Counterclockwise;

    // Celestial bodies
    [HideInInspector] public CelestialBody star;
    [HideInInspector] public CelestialBody planet1;

    private Light planetLight;

    // Orbital parameters
    [HideInInspector] public OrbitalParameters orbitalParameters;
    private float orbitSign;

    // Timer for reinitializing the simulation after one full period
    [HideInInspector] public float theta;
    [HideInInspector] public float resetTimer;
    private Vector3 initPlanetPosition;

    // Other quantities used by the UI Manager
    [HideInInspector] public Vector3 currentForce;

    // Constants
    private const double newtonG_SI = 6.6743e-11;   // m^3 / kg / s^2
    private const float au_SI = 149597870700f;      // m
    private const double r_sun_SI = 6.9634e8;       // m
    private const double m_sun_SI = 1.98847e30;     // kg
    private const float year_SI = 31556952f;        // s
    private const float month_SI = year_SI / 12f;   // s
    private const float day_SI = 86400f;            // s

    // Gravitational constant
    public float NewtonG
    {
        get
        {
            float t = year_SI;
            if (unitTime == UnitTime.Month)
            {
                t = month_SI;
            }
            else if (unitTime == UnitTime.Day)
            {
                t = day_SI;
            }

            double l = au_SI;
            if (unitLength == UnitLength.SolarRadius)
            {
                l = r_sun_SI;
            }

            return (float)(newtonG_SI * m_sun_SI * t * t / l / l / l);
        }
    }
    // Semi-major axis
    public float SemiMajorAxis
    {
        get { return perihelionDistance / (1 - Eccentricity); }
    }
    // Orbital period
    public float Period
    {
        get
        {
            // Unbound orbit
            if (Eccentricity >= 1)
            {
                return float.PositiveInfinity;
            }

            // Bound orbit
            float a = SemiMajorAxis;
            return 2 * Mathf.PI * Mathf.Sqrt(a * a * a / NewtonG / star.Mass);
        }
    }
    // Energy (specific)
    public float Energy
    {
        get { return -0.5f * NewtonG * star.Mass / SemiMajorAxis; }
    }
    // Angular momentum magnitude (specific)
    public float AngularMomentum
    {
        get
        {
            float sign = (orbitDirection == OrbitDirection.Clockwise) ? -1f : 1f;
            return sign * Mathf.Sqrt(NewtonG * star.Mass * SemiLatusRectum);
        }
    }
    // Orbit eccentricity
    public float Eccentricity
    {
        get { return eccentricity; }
    }
    // Semi-latus Rectum
    public float SemiLatusRectum
    {
        get { return perihelionDistance * (1 + Eccentricity); }
    }
    // Ratio T^2 / a^3
    public float PeriodToSemiMajorAxisRatio
    {
        get { return Period * Period / SemiMajorAxis / SemiMajorAxis / SemiMajorAxis; }
    }

    private void Awake()
    {
        if (!TryGetComponent(out prefabs))
        {
            Debug.LogWarning("No KeplerPrefabs component found.");
            Pause();
            return;
        }

        // Create all objects assigned in the inspector
        prefabs.InstantiateAllPrefabs();

        star = prefabs.star;
        planet1 = prefabs.planet;

        Reset();

        if (planet1)
        {
            planetLight = planet1.GetComponentInChildren<Light>();
        }

        if (prefabs.centerOfMass)
        {
            prefabs.centerOfMass.localPosition += 0.02f * Vector3.down;
        }
    }

    private void FixedUpdate()
    {
        if (paused)
        {
            return;
        }

        if (!star || !planet1)
        {
            return;
        }

        if (resetAfterOnePeriod)
        {
            // Re-establish the system to exact initial positions after one period to avoid numerical errors
            if (resetTimer >= Period)
            {
                resetTimer = 0;
                planet1.Position = initPlanetPosition;
                Vector3 position1 = initPlanetPosition - star.Position;
                theta = Mathf.Atan2(position1.y, position1.x);
                //Debug.Log("Resetting sim...");
            }

            resetTimer += timeScale * Time.fixedDeltaTime;
        }

        // Update planet positions
        float substep = timeScale * Time.fixedDeltaTime / numSubsteps;
        for (int i = 1; i <= numSubsteps; i++)
        {
            StepForward(planet1, ref theta, substep);
        }

        if (planetLight)
        {
            planetLight.transform.forward = (planet1.Position - star.Position).normalized;
        }

        // Update the current force between the two bodies (for UIManager)
        currentForce = -(NewtonG * star.Mass / planet1.Position.sqrMagnitude) * planet1.Position.normalized;
    }

    private void StepForward(CelestialBody planet, ref float theta, float deltaTime)
    {
        // Solve the equation of motion in polar coordinates
        Vector3 vectorR = planet.Position - star.Position;

        // Update position (direction of rotation is given by the signed L value)
        theta += orbitalParameters.L * deltaTime / vectorR.sqrMagnitude;
        float r = StarToPlanetDistance(theta);
        Vector3 position = new Vector3(r * Mathf.Cos(theta), r * Mathf.Sin(theta));
        planet.Position = star.Position + position;
    }

    public override void Reset()
    {
        resetTimer = 0;

        if (!star || !planet1)
        {
            return;
        }

        // Star
        star.Position = starPosition;
        star.Mass = starMass;
        star.Radius = starRadius;  //Mathf.Pow(3f * star.Mass / 4f / Mathf.PI, 0.333f);
        star.CanRotate = false;

        // Planet
        if (startAtPerihelion)
        {
            Vector3 direction = (starAtFocus == Focus.Left) ? Vector3.left : Vector3.right;
            initPlanetPosition = star.Position + perihelionDistance * direction;
        }
        else
        {
            float aphelionDistance = (1 + Eccentricity) * SemiMajorAxis;
            Vector3 direction = (starAtFocus == Focus.Left) ? Vector3.right : Vector3.left;
            initPlanetPosition = star.Position + aphelionDistance * direction;
        }
        planet1.Position = initPlanetPosition;
        planet1.Radius = planet1Radius;
        planet1.CanRotate = false;

        orbitSign = (starAtFocus == Focus.Left) ? -1f : 1f;
        orbitalParameters = new OrbitalParameters(SemiMajorAxis, Eccentricity, SemiLatusRectum, AngularMomentum, Energy, Period);

        Vector3 position1 = initPlanetPosition - star.Position;
        theta = Mathf.Atan2(position1.y, position1.x);

        //Debug.Log(transform.name + " semi-major axis is " + SemiMajorAxis);
        //Debug.Log(transform.name + " period is " + Period);
        //Debug.Log(transform.name + " G = " + NewtonG);
        //Debug.Log(transform.name + " P^2/a^3 = " + Period * Period / SemiMajorAxis / SemiMajorAxis / SemiMajorAxis);
    }

    public void Randomize(bool centerTheOrbit = false)
    {
        resetTimer = 0;

        int leftRightFocus = Random.Range(0, 2);  // Left = 0, Right = 1
        perihelionDistance = Random.Range(0.5f, 0.75f);
        eccentricity = Random.Range(0.3f, 0.7f);

        Vector3 direction = (leftRightFocus == 0) ? Vector3.left : Vector3.right;

        // Shift the system so that the center of the orbital ellipse is at (0, 0, 0)
        if (centerTheOrbit)
        {
            star.Position = Eccentricity * SemiMajorAxis * direction;
        }

        // Planet position
        initPlanetPosition = star.Position + perihelionDistance * direction;
        planet1.Position = initPlanetPosition;
        Vector3 position1 = initPlanetPosition - star.Position;
        theta = Mathf.Atan2(position1.y, position1.x);

        orbitSign = (leftRightFocus == 0) ? -1f : 1f;
        starAtFocus = (leftRightFocus == 0) ? Focus.Left : Focus.Right;
        orbitalParameters = new OrbitalParameters(SemiMajorAxis, Eccentricity, SemiLatusRectum, AngularMomentum, Energy, Period);
    }

    public void ChangeStarMass(float mass)
    {
        star.Mass = mass;
    }

    public void HideStar()
    {
        star.transform.GetComponent<MeshRenderer>().enabled = false;
    }

    public float StarToPlanetDistance(float theta)
    {
        // Theta is always the angle wrt the positive x-axis (positive is CCW)
        return orbitalParameters.p / (1f + orbitSign * orbitalParameters.e * Mathf.Cos(theta));
    }

    public struct OrbitalParameters
    {
        public float a;  // Semimajor axis
        public float e;  // Eccentricity
        public float p;  // Semi-latus rectum
        public float L;  // Angular momentum (specific)
        public float E;  // Energy (specific)
        public float T;  // Orbital period

        public OrbitalParameters(float a, float e, float p, float L, float E, float T)
        {
            this.a = a;
            this.e = e;
            this.p = p;
            this.L = L;
            this.E = E;
            this.T = T;
        }
    }
}
