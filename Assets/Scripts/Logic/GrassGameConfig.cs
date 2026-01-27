using UnityEngine;

[CreateAssetMenu(menuName = "Grass Game/Config", fileName = "GrassGameConfig")]
public class GrassGameConfig : ScriptableObject
{
    [Header("Session")]
    [Min(0.1f)] public float sessionTimeSeconds = 10f;

    [Header("Breaker")]
    [Min(0.01f)] public float breakerRadius = 1.25f;
    [Min(0f)] public float breakerDamage = 1f;
    [Min(0.01f)] public float breakerTickInterval = 0.5f;

    [Range(0f, 1f)] public float breakerCritChance = 0.1f;
    [Min(1f)] public float breakerCritBonusMultiplier = 2f;

    [Header("Grass")]
    [Min(0.01f)] public float startingGrassHP = 3f; // ~3 snips if breakerDamage=1 and no crit
    [Min(0)] public int startingGrassPatchCount = 20;
    [Min(0)] public int maxGrassPatches = 200;

    [Header("Spawning Area (World Space)")]
    public Vector3 fieldCenter = Vector3.zero;
    public Vector2 fieldSize = new Vector2(20f, 20f); // XZ size
    [Min(0f)] public float spawnY = 0f;

    [Header("Spawn Collision / Spacing")]
    [Min(0f)] public float spawnAvoidRadius = 0.5f;
    [Min(1)] public int spawnAttemptsPerPatch = 12;

    [Header("Respawn Pressure Tuning")]
    [Tooltip("Baseline spawns per second while session runs.")]
    [Min(0f)] public float baseRespawnRatePerSecond = 1.5f;

    [Tooltip("Additional spawns/sec per (cuts per second).")]
    [Min(0f)] public float respawnRatePerCutPerSecond = 0.75f;

    [Tooltip("Baseline target population the spawner tries to maintain.")]
    [Min(0)] public int baseTargetPopulation = 35;

    [Tooltip("Extra target population per (cuts per second).")]
    [Min(0f)] public float targetPopulationPerCutPerSecond = 8f;

    [Tooltip("Seconds window for measuring cuts/sec.")]
    [Min(0.1f)] public float pressureWindowSeconds = 3f;

    public Bounds GetFieldBounds()
    {
        var size3 = new Vector3(fieldSize.x, 0.01f, fieldSize.y);
        return new Bounds(new Vector3(fieldCenter.x, spawnY, fieldCenter.z), size3);
    }
}
