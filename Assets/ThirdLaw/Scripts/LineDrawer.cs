using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
public class LineDrawer : MonoBehaviour
{
    [SerializeField] private Vector3 planeNormal = Vector3.back;
    [SerializeField] private bool showTextLabel;
    [SerializeField] private TextMeshProUGUI textLabel;
    [SerializeField] private bool showImageLabel;
    [SerializeField] private Image imageLabel;
    [SerializeField] private Vector2 labelOffset;

    private Camera mainCamera;
    private Plane plane;
    private LineRenderer line;

    private bool clickedOnUIElement;

    [HideInInspector] public bool canDraw;
    [HideInInspector] public bool overrideStartPosition;
    [HideInInspector] public Vector3 forcedStartPosition;

    public float Distance
    {
        get
        {
            if (line.positionCount != 2)
            {
                return 0;
            }

            return Vector3.Distance(line.GetPosition(0), line.GetPosition(1));
        }
    }

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 0;
        line.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero });

        // Get the camera reference only once
        mainCamera = Camera.main;

        // Set up the plane to lie in the XY plane
        plane = new Plane(planeNormal, 0);

        HideTextLabel();
        HideImageLabel();
    }

    private void Update()
    {
        if (!canDraw)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            clickedOnUIElement = EventSystem.current.IsPointerOverGameObject();

            if (clickedOnUIElement)
            {
                return;
            }

            if (line.positionCount != 2)
            {
                line.positionCount = 2;
            }

            if (overrideStartPosition)
            {
                line.SetPosition(0, forcedStartPosition);
                line.SetPosition(1, forcedStartPosition);
            }
            else
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (plane.Raycast(ray, out float distance))
                {
                    line.SetPosition(0, ray.GetPoint(distance));
                    line.SetPosition(1, line.GetPosition(0));
                }
            }
        }

        if (Input.GetMouseButton(0) && !clickedOnUIElement)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float distance))
            {
                line.SetPosition(1, ray.GetPoint(distance));

                UpdateTextLabel(Distance);
                UpdateImageLabel();
                UpdateLabelPositions();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            clickedOnUIElement = false;
        }
    }

    public void HideTextLabel()
    {
        if (textLabel != null)
        {
            textLabel.gameObject.SetActive(false);
        }
    }

    public void UpdateTextLabel(float value)
    {
        if (!showTextLabel)
        {
            return;
        }
        else if (textLabel == null)
        {
            return;
        }
        else if (!textLabel.gameObject.activeInHierarchy)
        {
            textLabel.gameObject.SetActive(true);
        }

        textLabel.text = value.ToString("0.00");
    }

    public void UpdateImageLabel()
    {
        if (!showImageLabel)
        {
            return;
        }
        else if (imageLabel == null)
        {
            return;
        }
        else if (!imageLabel.gameObject.activeInHierarchy)
        {
            imageLabel.gameObject.SetActive(true);
        }
    }

    public void ShowImageLabel()
    {
        if (imageLabel != null)
        {
            imageLabel.gameObject.SetActive(true);
        }
    }

    public void HideImageLabel()
    {
        if (imageLabel != null)
        {
            imageLabel.gameObject.SetActive(false);
        }
    }

    public void UpdateLabelPositions()
    {
        // Do no calculations if not showing any labels
        if (!showTextLabel && !showImageLabel)
        {
            return;
        }

        // Also do nothing if the line doesn't have the right number of points
        if (line.positionCount != 2)
        {
            return;
        }

        Vector3 startScreenPosition = mainCamera.WorldToScreenPoint(line.GetPosition(0));
        Vector3 endScreenPosition = mainCamera.WorldToScreenPoint(line.GetPosition(1));
        Vector3 position = 0.5f * (startScreenPosition + endScreenPosition);
        Vector3 normalDirection = Vector3.Cross(position, Vector3.back).normalized;

        // TODO fix this...
        position += labelOffset.x * position.normalized + labelOffset.y * normalDirection;

        if (showTextLabel && textLabel != null)
        {
            textLabel.transform.position = position;
        }

        if (showImageLabel && imageLabel != null)
        {
            imageLabel.transform.position = position;
        }
    }

    public void Clear()
    {
        line.positionCount = 0;
        HideTextLabel();
        HideImageLabel();
    }
}
