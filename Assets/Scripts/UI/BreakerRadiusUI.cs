using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class BreakerRadiusUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform circleRect;   // UI Image rect
    [SerializeField] private Canvas canvas;              // Screen Space Overlay
    [SerializeField] private Camera cam;                 // Camera.main
    [SerializeField] private BreakerController breaker;
    [SerializeField] private GrassCutterSystem cutter;   // add this

    [Header("Pulse")]
    [SerializeField, Min(1f)] private float pulseScale = 1.15f;
    [SerializeField, Min(0.01f)] private float pulseInSeconds = 0.06f;
    [SerializeField, Min(0.01f)] private float pulseOutSeconds = 0.10f;

    [Header("Look")]
    [SerializeField] private float extraPixelsPadding = 0f;
    [SerializeField] private bool hideIfNoMouse = true;

    RectTransform canvasRect;
    Vector3 baseScale = Vector3.one;
    Coroutine pulseCo;
    void Awake()
    {
        if (!cam)
            cam = Camera.main;
        if (!canvas)
            canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas.transform as RectTransform;
    }

    void Update()
    {
        if (!circleRect || !canvas || !cam || breaker == null)
            return;

        if (hideIfNoMouse && Mouse.current == null)
        {
            circleRect.gameObject.SetActive(false);
            return;
        }
        circleRect.gameObject.SetActive(true);

        Vector2 mouseScreen = Mouse.current.position.ReadValue();

        // 1) Put circle at mouse position (correct for Canvas scaling)
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, mouseScreen, null, out Vector2 localPoint
        );
        circleRect.anchoredPosition = localPoint;

        // 2) World radius -> pixel radius (at breaker world position)
        Vector3 centerWorld = breaker.BreakerWorldPos;

        Vector3 centerScreen = cam.WorldToScreenPoint(centerWorld);
        Vector3 edgeWorld = centerWorld + cam.transform.right * breaker.Radius;
        Vector3 edgeScreen = cam.WorldToScreenPoint(edgeWorld);

        if (centerScreen.z <= 0f || edgeScreen.z <= 0f)
        {
            circleRect.gameObject.SetActive(false);
            return;
        }

        float pixelRadius = Vector2.Distance(
            new Vector2(centerScreen.x, centerScreen.y),
            new Vector2(edgeScreen.x, edgeScreen.y)
        );

        // Convert pixels -> canvas units
        float scaleFactor = canvas.scaleFactor <= 0f ? 1f : canvas.scaleFactor;
        float diameterCanvasUnits = ((pixelRadius * 2f) + extraPixelsPadding) / scaleFactor;

        circleRect.sizeDelta = new Vector2(diameterCanvasUnits, diameterCanvasUnits);
    }
}
