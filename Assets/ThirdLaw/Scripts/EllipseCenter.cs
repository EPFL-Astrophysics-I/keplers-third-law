using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CircleCollider2D))]
public class EllipseCenter : MonoBehaviour
{
    [SerializeField] private LineDrawer lineDrawer;

    private Vector3 defaultScale;
    private bool mouseIsOver;

    private void Awake()
    {
        defaultScale = transform.localScale;
    }

    private void Start()
    {
        if (lineDrawer != null)
        {
            lineDrawer.overrideStartPosition = true;
            lineDrawer.forcedStartPosition = transform.localPosition;
            lineDrawer.canDraw = false;
        }
    }

    private void Update()
    {
        if (lineDrawer == null)
        {
            return;
        }

        //if (lineDrawer.canDraw && Input.GetMouseButton(0))
        //{
        //    Debug.Log("Currently drawing a line");
        //}

        if (Input.GetMouseButtonDown(0))
        {
            transform.localScale = defaultScale;
        }

        if (Input.GetMouseButtonUp(0))
        {
            lineDrawer.canDraw = false;
            transform.localScale = defaultScale;
        }
    }

    public void OnMouseEnter()
    {
        if (mouseIsOver || Input.GetMouseButton(0))
        {
            return;
        }

        mouseIsOver = true;
        transform.localScale *= 1.5f;

        if (lineDrawer != null)
        {
            lineDrawer.canDraw = true;
        }
    }

    public void OnMouseExit()
    {
        mouseIsOver = false;

        if (!Input.GetMouseButton(0))
        {
            lineDrawer.canDraw = false;
            transform.localScale = defaultScale;
        }
    }

    public LineDrawer GetLineDrawer()
    {
        return lineDrawer;
    }

    public void ResetForcedStartPosition()
    {
        lineDrawer.forcedStartPosition = transform.localPosition;
    }
}
