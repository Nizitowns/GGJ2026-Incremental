using UnityEngine;
using VInspector.Libs;
using System;
public class GrassCutterSystem : MonoBehaviour
{
    public event Action SnipTick;
    [SerializeField] private int overlapBufferSize = 256;

    private GrassGameConfig config;
    private GameSessionController session;
    private BreakerController breaker;
    private GrassSpawner spawner;

    private Collider[] overlapBuffer;
    private bool running;
    private float nextTickTime;

    public void SetConfig(GrassGameConfig cfg) => config = cfg;

    private void Awake()
    {
        overlapBuffer = new Collider[Mathf.Max(32, overlapBufferSize)];
    }

    public void Begin(GameSessionController sessionController, BreakerController breakerController, GrassSpawner grassSpawner)
    {
        session = sessionController;
        breaker = breakerController;
        spawner = grassSpawner;

        running = true;
        nextTickTime = Time.time; // tick immediately
    }

    public void Stop()
    {
        running = false;
        session = null;
        breaker = null;
        spawner = null;
    }

    private void Update()
    {
        if (!running || config == null || session == null || breaker == null || spawner == null)
            return;
        if (session.State != GameSessionController.SessionState.Running)
            return;

        float now = Time.time;
        if (now < nextTickTime)
            return;


        nextTickTime = now + config.breakerTickInterval;
        DoSnipTick();

    }

    private void DoSnipTick()
    {
        SnipTick?.Invoke();
        Vector3 center = breaker.BreakerWorldPos;
        float radius = config.breakerRadius;

        int hitCount = Physics.OverlapSphereNonAlloc(
            center,
            radius,
            overlapBuffer,
            spawner.GrassMask,
            QueryTriggerInteraction.Ignore
        );

        if (hitCount <= 0)
            return;

        float baseDmg = config.breakerDamage;
        float critChance = config.breakerCritChance;
        float critMult = config.breakerCritBonusMultiplier;

        for (int i = 0; i < hitCount; i++)
        {
            Collider c = overlapBuffer[i];
            if (!c)
                continue;

            // GrassPatch can be on same GO or parent (depending on prefab setup)
            GrassPatch patch = c.GetComponent<GrassPatch>();
            if (!patch)
                patch = c.GetComponentInParent<GrassPatch>();
            if (!patch)
                continue;

            float dmg = baseDmg;
            if (critChance > 0f && UnityEngine.Random.value < critChance)
                dmg *= critMult;

            patch.ApplyDamage(dmg);
        }
    }
}
