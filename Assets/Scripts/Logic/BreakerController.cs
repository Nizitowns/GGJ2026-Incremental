using UnityEngine;

public class BreakerController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask playfieldMask = ~0; // set to Playfield layer in inspector

    private GrassGameConfig config;

    public Vector3 BreakerWorldPos
    {
        get; private set;
    }
    public float Radius => config ? config.breakerRadius : 1f;

    public void SetConfig(GrassGameConfig cfg) => config = cfg;

    private void Awake()
    {
        if (!mainCamera)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        UpdateBreakerPosition();
    }

    private void UpdateBreakerPosition()
    {
        if (!mainCamera)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Prefer a collider-based playfield for accuracy.
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, playfieldMask, QueryTriggerInteraction.Ignore))
        {
            BreakerWorldPos = hit.point;
            return;
        }

        // Fallback: plane at config.spawnY or y=0
        float y = config ? config.spawnY : 0f;
        Plane plane = new Plane(Vector3.up, new Vector3(0f, y, 0f));
        if (plane.Raycast(ray, out float enter))
        {
            BreakerWorldPos = ray.GetPoint(enter);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!config)
            return;
        Gizmos.DrawWireSphere(BreakerWorldPos, config.breakerRadius);
    }
}
