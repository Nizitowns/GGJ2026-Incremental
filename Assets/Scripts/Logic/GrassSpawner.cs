using System.Collections.Generic;
using UnityEngine;

public class GrassSpawner : MonoBehaviour
{
    [Header("Prefab / Layers")]
    [SerializeField] private GrassPatch grassPrefab;
    [SerializeField] private LayerMask grassLayerMask; // set to Grass layer
    [SerializeField] private Transform spawnedParent;

    private GrassGameConfig config;

    private readonly List<GrassPatch> alive = new List<GrassPatch>(512);
    private readonly Queue<GrassPatch> pool = new Queue<GrassPatch>(512);

    private bool running;
    private float spawnBudget; // accumulates spawns over time

    // Pressure measurement
    private readonly Queue<float> cutTimestamps = new Queue<float>(256);

    public void SetConfig(GrassGameConfig cfg) => config = cfg;

    public int AliveCount => alive.Count;
    public LayerMask GrassMask => grassLayerMask;

    public void ResetSpawner()
    {
        Stop();
        // Despawn all alive
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            Despawn(alive[i]);
        }
        alive.Clear();
        cutTimestamps.Clear();
        spawnBudget = 0f;
    }

    public void SpawnInitial()
    {
        if (!config || !grassPrefab)
            return;
        running = true;

        int count = Mathf.Clamp(config.startingGrassPatchCount, 0, config.maxGrassPatches);
        for (int i = 0; i < count; i++)
        {
            TrySpawnOne();
        }
    }

    public void Stop() => running = false;

    public void NotifyGrassCut()
    {
        float now = Time.time;
        cutTimestamps.Enqueue(now);

        // Trim old timestamps beyond pressure window
        TrimCuts(now);
    }

    private void Update()
    {
        if (!running || !config || !grassPrefab)
            return;

        float now = Time.time;
        TrimCuts(now);

        float cps = GetCutsPerSecond(now);

        // Compute dynamic target + respawn rate under "pressure"
        int target = Mathf.Clamp(
            Mathf.RoundToInt(config.baseTargetPopulation + config.targetPopulationPerCutPerSecond * cps),
            0,
            config.maxGrassPatches
        );

        float respawnRate = config.baseRespawnRatePerSecond + config.respawnRatePerCutPerSecond * cps;

        // Only try to spawn if we are under target (keeps it stable and tunable)
        if (alive.Count < target && alive.Count < config.maxGrassPatches)
        {
            spawnBudget += respawnRate * Time.deltaTime;

            // Convert accumulated budget into discrete spawns
            int toSpawn = Mathf.FloorToInt(spawnBudget);
            if (toSpawn > 0)
            {
                spawnBudget -= toSpawn;
                for (int i = 0; i < toSpawn; i++)
                {
                    if (alive.Count >= target || alive.Count >= config.maxGrassPatches)
                        break;
                    if (!TrySpawnOne())
                        break; // if we can't find valid positions, stop this frame
                }
            }
        }
        else
        {
            // If at/above target, slowly bleed budget to avoid huge burst later
            spawnBudget = Mathf.Max(0f, spawnBudget - Time.deltaTime);
        }
    }

    private void TrimCuts(float now)
    {
        float cutoff = now - config.pressureWindowSeconds;
        while (cutTimestamps.Count > 0 && cutTimestamps.Peek() < cutoff)
            cutTimestamps.Dequeue();
    }

    private float GetCutsPerSecond(float now)
    {
        // count cuts in last window / window length
        if (config.pressureWindowSeconds <= 0.0001f)
            return 0f;
        return cutTimestamps.Count / config.pressureWindowSeconds;
    }

    private bool TrySpawnOne()
    {
        if (!TryGetSpawnPosition(out Vector3 pos))
            return false;

        GrassPatch patch = (pool.Count > 0) ? pool.Dequeue() : Instantiate(grassPrefab, spawnedParent ? spawnedParent : transform);
        patch.transform.position = pos;

        patch.Cut -= OnGrassPatchCut; // safety (in case it was pooled with a leftover)
        patch.Cut += OnGrassPatchCut;

        patch.Initialize(config.startingGrassHP);

        alive.Add(patch);
        return true;
    }

    private void OnGrassPatchCut(GrassPatch patch)
    {
        NotifyGrassCut();
        Despawn(patch);
        alive.Remove(patch);
    }

    private void Despawn(GrassPatch patch)
    {
        if (!patch)
            return;
        patch.Cut -= OnGrassPatchCut;
        patch.gameObject.SetActive(false);
        pool.Enqueue(patch);
    }

    private bool TryGetSpawnPosition(out Vector3 pos)
    {
        Bounds b = config.GetFieldBounds();

        // We'll do a simple overlap check with a sphere radius
        float r = Mathf.Max(0f, config.spawnAvoidRadius);

        for (int attempt = 0; attempt < config.spawnAttemptsPerPatch; attempt++)
        {
            float x = Random.Range(b.min.x, b.max.x);
            float z = Random.Range(b.min.z, b.max.z);
            pos = new Vector3(x, config.spawnY, z);

            if (r <= 0f)
                return true;

            // Check if something already occupies the spawn area (grass layer only)
            Collider[] hits = Physics.OverlapSphere(pos, r, grassLayerMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
                return true;
        }

        pos = default;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!config)
            return;
        Bounds b = config.GetFieldBounds();
        Gizmos.DrawWireCube(b.center, new Vector3(b.size.x, 0.01f, b.size.z));
    }
}
