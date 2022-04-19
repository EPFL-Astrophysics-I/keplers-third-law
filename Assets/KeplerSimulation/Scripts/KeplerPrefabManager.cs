using UnityEngine;

[RequireComponent(typeof(KeplerSimulation))]
public class KeplerPrefabManager : MonoBehaviour
{
    [Header("Star")]
    public GameObject starPrefab;

    [Header("Planet")]
    public GameObject planetPrefab;
    [SerializeField] private GameObject positionVectorPrefab;
    [SerializeField] private GameObject orbitPrefab;

    [Header("Others")]
    [SerializeField] private GameObject centerOfMassPrefab;
    [SerializeField] private GameObject angularMomentumVectorPrefab;
    [SerializeField] private GameObject semiMajorAxisPrefab;
    [SerializeField] private GameObject semiMinorAxisPrefab;
    [SerializeField] private GameObject ellipseCenterPrefab;

    [HideInInspector] public CelestialBody star;
    [HideInInspector] public CelestialBody planet;
    [HideInInspector] public Transform centerOfMass;
    [HideInInspector] public Vector positionVector;
    [HideInInspector] public LineRenderer orbit;
    [HideInInspector] public Vector semiMajorAxis;
    [HideInInspector] public Vector semiMinorAxis;
    [HideInInspector] public Transform ellipseCenter;

    public void SetCenterOfMassVisibility(bool visible)
    {
        if (centerOfMass)
        {
            centerOfMass.gameObject.SetActive(visible);
        }
    }

    public void SetPositionVector1Visibility(bool visible)
    {
        if (positionVector)
        {
            positionVector.gameObject.SetActive(visible);
        }
    }

    public void SetOrbit1Visibility(bool visible)
    {
        if (orbit)
        {
            orbit.gameObject.SetActive(visible);
        }
    }

    public void SetStarLabelVisibility(bool visible)
    {
        Transform label = star.transform.Find("Label");
        if (label)
        {
            label.gameObject.SetActive(visible);
        }
    }

    public void SetPlanetLabelVisibility(bool visible)
    {
        Transform label = planet.transform.Find("Label");
        if (label)
        {
            label.gameObject.SetActive(visible);
        }
    }

    public void SetSemiMajorAxisVisibility(bool visible)
    {
        if (semiMajorAxis)
        {
            semiMajorAxis.gameObject.SetActive(visible);
        }
    }

    public void SetSemiMinorAxisVisibility(bool visible)
    {
        if (semiMinorAxis)
        {
            semiMinorAxis.gameObject.SetActive(visible);
        }
    }

    public void SetEllipseCenterVisibility(bool visible)
    {
        if (ellipseCenter)
        {
            ellipseCenter.gameObject.SetActive(visible);
        }
    }

    public void InstantiateAllPrefabs()
    {
        if (starPrefab)
        {
            star = Instantiate(starPrefab, transform).GetComponent<CelestialBody>();
            star.gameObject.name = "Star";
        }

        if (centerOfMassPrefab)
        {
            centerOfMass = Instantiate(centerOfMassPrefab, transform).transform;
            centerOfMass.name = "Center of Mass";
        }

        if (planetPrefab)
        {
            planet = Instantiate(planetPrefab, Vector3.zero, Quaternion.identity, transform).GetComponent<CelestialBody>();
            planet.gameObject.name = "Planet 1";
        }

        if (positionVectorPrefab)
        {
            positionVector = Instantiate(positionVectorPrefab, transform).GetComponent<Vector>();
            positionVector.SetPositions(Vector3.zero, Vector3.zero);
            positionVector.name = "Position Vector 1";
        }

        if (orbitPrefab)
        {
            orbit = Instantiate(orbitPrefab, transform).GetComponent<LineRenderer>();
            orbit.positionCount = 0;
            orbit.name = "Orbit 1";
        }

        if (semiMajorAxisPrefab)
        {
            semiMajorAxis = Instantiate(semiMajorAxisPrefab, transform).GetComponent<Vector>();
            semiMajorAxis.SetPositions(Vector3.zero, Vector3.zero);
            semiMajorAxis.name = "Semi-Major Axis";
        }

        if (semiMinorAxisPrefab)
        {
            semiMinorAxis = Instantiate(semiMinorAxisPrefab, transform).GetComponent<Vector>();
            semiMinorAxis.SetPositions(Vector3.zero, Vector3.zero);
            semiMinorAxis.name = "Semi-Minor Axis";
        }

        if (ellipseCenterPrefab)
        {
            ellipseCenter = Instantiate(ellipseCenterPrefab, transform).transform;
            ellipseCenter.name = "Ellipse Center";
        }
    }
}
